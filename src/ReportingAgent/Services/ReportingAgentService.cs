using ReportingAgent.Clients;
using ReportingAgent.Models;

namespace ReportingAgent.Services;

public sealed class ReportingAgentService(
    CollectionSchemaClient collectionSchemaClient,
    DataQueryClient dataQueryClient,
    ILogger<ReportingAgentService> logger)
{
    public const string SystemPrompt = """
你是一个专业的数据上报助手，专门帮助用户通过已有数据集定义选取产生可以用作上报项目的业务数据集。

## 可用工具
### 标准数据集定义查询工具（CollectionSchema插件）
用于查询标准数据集定义：
1. GetAllCollections - 获取所有的标准数据集
2. GetAllParameters - 获取所有的标准数据集的输入参数和输出参数
3. GetAllValueSets - 获取所有的标准数据集参数值域

### 业务数据查询工具（DataQuery插件）
用于调用Restful API服务查询数据集：
1. SampleCollectionData - 对标准数据集进行数据采样
2. ExecuteBusinessQuery - 根据业务数据集定义执行查询验证

## 工作流程
1. 理解需求
2. 获取元数据
3. 数据采样
4. 选择数据集
5. 组装数据集
6. 验证执行
7. 返回结果

## 重要规则
- 组装的业务数据集必须要有标准数据集定义作为来源，不能自行新增数据来源
- 标准数据集之间的关系通过数据集的参数名称匹配，名称相同的可作为关联关系
- 具有值域的参数，根据标准数据集值域名称涵义与业务数据集要求的值域做对照处理
- 使用中文回答，提供清晰的分析和建议
""";

    public async Task<BusinessDatasetDefinition> BuildBusinessDatasetAsync(UserRequirementRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("开始处理业务数据集组装请求: {Requirement}", request.RequirementText);

        var collections = await collectionSchemaClient.GetAllCollectionsAsync(cancellationToken);
        var parameters = await collectionSchemaClient.GetAllParametersAsync(cancellationToken);
        var valueSets = await collectionSchemaClient.GetAllValueSetsAsync(cancellationToken);

        var requirementTokens = request.RequirementText.Split([' ', ',', '，', '。', ';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var candidateCollections = collections
            .Where(c => requirementTokens.Any(t => c.Name.Contains(t, StringComparison.OrdinalIgnoreCase) || c.Description.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (candidateCollections.Count == 0)
        {
            candidateCollections = collections.Take(2).ToList();
        }

        var selectedParameters = parameters.Where(p => candidateCollections.Any(c => c.Name == p.CollectionName)).ToList();

        var inputs = selectedParameters
            .Where(p => p.Direction.Equals("Input", StringComparison.OrdinalIgnoreCase))
            .Select(p => new DatasetField(p.Name, p.DataType, "Input", p.CollectionName, $"输入参数（默认值: {p.DefaultValue ?? "无"}）"))
            .DistinctBy(p => (p.Name, p.SourceCollection, p.Direction))
            .ToList();

        var outputs = selectedParameters
            .Where(p => p.Direction.Equals("Output", StringComparison.OrdinalIgnoreCase))
            .Select(p => new DatasetField(p.Name, p.DataType, "Output", p.CollectionName, "输出参数"))
            .DistinctBy(p => (p.Name, p.SourceCollection, p.Direction))
            .ToList();

        var joinHints = BuildJoinHints(selectedParameters);

        var sources = candidateCollections
            .Select(c => new DatasetSource(
                c.Name,
                c.Description,
                joinHints.Where(j => j.LeftCollection == c.Name || j.RightCollection == c.Name).ToList()))
            .ToList();

        var valueSetMappings = selectedParameters
            .Where(p => !string.IsNullOrWhiteSpace(p.ValueSetName))
            .Select(p =>
            {
                var vs = valueSets.FirstOrDefault(v => v.Name.Equals(p.ValueSetName, StringComparison.OrdinalIgnoreCase));
                return new ValueSetMapping(
                    p.Name,
                    p.ValueSetName!,
                    vs?.Description ?? "值域定义未找到",
                    vs?.Items ?? []);
            })
            .DistinctBy(x => (x.ParameterName, x.ValueSetName))
            .ToList();

        await TrySampleDataAsync(inputs, request.PreferredInputs, cancellationToken);

        var draft = new BusinessDatasetDefinition(
            Name: $"Business_{DateTime.UtcNow:yyyyMMddHHmmss}",
            Requirement: request.RequirementText,
            Sources: sources,
            Inputs: inputs,
            Outputs: outputs,
            ValueSetMappings: valueSetMappings,
            Validation: null);

        var validationResponse = await dataQueryClient.ExecuteBusinessQueryAsync(
            new BusinessQueryValidationRequest(draft, request.PreferredInputs),
            cancellationToken);

        var validation = validationResponse is null
            ? new ValidationResult(false, "执行验证未返回结果", null)
            : new ValidationResult(validationResponse.Success, validationResponse.Message, validationResponse.PreviewRows);

        return draft with { Validation = validation };
    }

    private static List<JoinHint> BuildJoinHints(IReadOnlyList<CollectionParameter> selectedParameters)
    {
        var joinHints = new List<JoinHint>();
        var groupedByName = selectedParameters.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedByName)
        {
            var items = group.Select(x => x.CollectionName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (items.Count < 2)
            {
                continue;
            }

            for (var i = 0; i < items.Count - 1; i++)
            {
                joinHints.Add(new JoinHint(items[i], group.Key, items[i + 1], group.Key));
            }
        }

        return joinHints;
    }

    private async Task TrySampleDataAsync(
        IReadOnlyList<DatasetField> inputs,
        IReadOnlyDictionary<string, string?> preferredInputs,
        CancellationToken cancellationToken)
    {
        var grouped = inputs
            .GroupBy(i => i.SourceCollection, StringComparer.OrdinalIgnoreCase)
            .Take(2);

        foreach (var inputGroup in grouped)
        {
            var parameterPayload = inputGroup.ToDictionary(
                x => x.Name,
                x => preferredInputs.TryGetValue(x.Name, out var value) ? value : null,
                StringComparer.OrdinalIgnoreCase);

            try
            {
                await dataQueryClient.SampleCollectionDataAsync(
                    new CollectionSampleRequest(inputGroup.Key, parameterPayload),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "数据采样失败，集合: {CollectionName}", inputGroup.Key);
            }
        }
    }
}
