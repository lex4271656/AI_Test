using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ReportingAgent.Options;

namespace ReportingAgent.Clients;

public sealed class OpenAiClient(HttpClient httpClient, IOptions<OpenAiOptions> options, ILogger<OpenAiClient> logger)
{
    private readonly OpenAiOptions _options = options.Value;

    public async Task<LlmDraftResponse?> BuildDatasetDraftAsync(
        string systemPrompt,
        string requirement,
        IReadOnlyDictionary<string, string?> preferredInputs,
        object metadataContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogWarning("OpenAI ApiKey 未配置，跳过 LLM 调用。");
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var userPrompt = $$"""
请根据用户需求与可用元数据，产出可执行的业务数据集草案。

用户需求：{{requirement}}
用户偏好输入（可能为空）：{{JsonSerializer.Serialize(preferredInputs)}}

可用元数据（JSON）：
{{JsonSerializer.Serialize(metadataContext)}}

输出要求：
1) 只能使用元数据中存在的集合、字段和值域；
2) 尽量补全输入/输出字段，Description 要说明用途；
3) 当需求信息不足时，给出保守可执行方案；
4) 只输出 JSON，不要输出 Markdown。
""";

        var payload = new ChatCompletionsRequest(
            _options.Model,
            _options.Temperature,
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt)
            ],
            new JsonObjectResponseFormat("json_object"));

        request.Content = JsonContent.Create(payload);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("OpenAI 调用失败: {StatusCode}, {Detail}", response.StatusCode, detail);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionsResponse>(cancellationToken: cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("OpenAI 返回内容为空。");
            return null;
        }

        try
        {
            var draft = JsonSerializer.Deserialize<LlmDraftResponse>(content, JsonOptions);
            return draft;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "OpenAI 返回 JSON 解析失败: {Content}", content);
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record ChatCompletionsRequest(
        string Model,
        double Temperature,
        IReadOnlyList<ChatMessage> Messages,
        JsonObjectResponseFormat Response_Format);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record JsonObjectResponseFormat(string Type);

    private sealed record ChatCompletionsResponse(IReadOnlyList<Choice>? Choices);

    private sealed record Choice(Message? Message);

    private sealed record Message(string? Content);
}

public sealed record LlmDraftResponse(
    string? Name,
    IReadOnlyList<string>? Sources,
    IReadOnlyList<LlmField>? Inputs,
    IReadOnlyList<LlmField>? Outputs,
    IReadOnlyList<LlmValueSetMapping>? ValueSetMappings,
    string? Notes);

public sealed record LlmField(
    string? Name,
    string? DataType,
    string? SourceCollection,
    string? Description);

public sealed record LlmValueSetMapping(
    string? ParameterName,
    string? ValueSetName,
    string? Description);
