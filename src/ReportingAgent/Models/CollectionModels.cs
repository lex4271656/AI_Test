namespace ReportingAgent.Models;

public sealed record CollectionDefinition(string Name, string Description);

public sealed record CollectionParameter(
    string CollectionName,
    string Name,
    string Direction,
    string DataType,
    string? ValueSetName,
    string? DefaultValue);

public sealed record ValueSetDefinition(string Name, string Description, IReadOnlyList<ValueSetItem> Items);

public sealed record ValueSetItem(string Code, string Label);

public sealed record CollectionSampleRequest(string CollectionName, IReadOnlyDictionary<string, string?> InputParameters);

public sealed record CollectionSampleResponse(string CollectionName, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

public sealed record BusinessQueryValidationRequest(BusinessDatasetDefinition Definition, IReadOnlyDictionary<string, string?> RuntimeInputs);

public sealed record BusinessQueryValidationResponse(bool Success, string Message, IReadOnlyList<IReadOnlyDictionary<string, object?>>? PreviewRows);

public sealed record UserRequirementRequest(string RequirementText, IReadOnlyDictionary<string, string?> PreferredInputs);

public sealed record BusinessDatasetDefinition(
    string Name,
    string Requirement,
    IReadOnlyList<DatasetSource> Sources,
    IReadOnlyList<DatasetField> Inputs,
    IReadOnlyList<DatasetField> Outputs,
    IReadOnlyList<ValueSetMapping> ValueSetMappings,
    ValidationResult? Validation);

public sealed record DatasetSource(string CollectionName, string Purpose, IReadOnlyList<JoinHint> JoinHints);

public sealed record JoinHint(string LeftCollection, string LeftParameter, string RightCollection, string RightParameter);

public sealed record DatasetField(string Name, string DataType, string Direction, string SourceCollection, string Description);

public sealed record ValueSetMapping(string ParameterName, string ValueSetName, string Description, IReadOnlyList<ValueSetItem> Items);

public sealed record ValidationResult(bool Success, string Message, IReadOnlyList<IReadOnlyDictionary<string, object?>>? PreviewRows);
