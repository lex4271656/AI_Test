namespace ReportingAgent.Options;

public sealed class PluginApiOptions
{
    public const string SectionName = "PluginApis";

    public string CollectionSchemaBaseUrl { get; set; } = "http://localhost:5001";

    public string DataQueryBaseUrl { get; set; } = "http://localhost:5002";
}
