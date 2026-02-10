using ReportingAgent.Clients;
using ReportingAgent.Models;
using ReportingAgent.Options;
using ReportingAgent.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PluginApiOptions>(builder.Configuration.GetSection(PluginApiOptions.SectionName));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<CollectionSchemaClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IConfiguration>().GetSection(PluginApiOptions.SectionName).Get<PluginApiOptions>() ?? new PluginApiOptions();
    client.BaseAddress = new Uri(options.CollectionSchemaBaseUrl);
});

builder.Services.AddHttpClient<DataQueryClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IConfiguration>().GetSection(PluginApiOptions.SectionName).Get<PluginApiOptions>() ?? new PluginApiOptions();
    client.BaseAddress = new Uri(options.DataQueryBaseUrl);
});

builder.Services.AddHttpClient<OpenAiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IConfiguration>().GetSection(OpenAiOptions.SectionName).Get<OpenAiOptions>() ?? new OpenAiOptions();
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddScoped<ReportingAgentService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/agent/system-prompt", () => Results.Ok(new { prompt = ReportingAgentService.SystemPrompt }));

app.MapPost("/agent/build-business-dataset", async (UserRequirementRequest request, ReportingAgentService service, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.RequirementText))
    {
        return Results.BadRequest("RequirementText 不能为空。");
    }

    var result = await service.BuildBusinessDatasetAsync(request, cancellationToken);
    return Results.Ok(result);
});

app.Run();
