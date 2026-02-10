using System.Net.Http.Json;
using ReportingAgent.Models;

namespace ReportingAgent.Clients;

public sealed class CollectionSchemaClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<CollectionDefinition>> GetAllCollectionsAsync(CancellationToken cancellationToken)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<CollectionDefinition>>("/CollectionSchema/GetAllCollections", cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<CollectionParameter>> GetAllParametersAsync(CancellationToken cancellationToken)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<CollectionParameter>>("/CollectionSchema/GetAllParameters", cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<ValueSetDefinition>> GetAllValueSetsAsync(CancellationToken cancellationToken)
        => await httpClient.GetFromJsonAsync<IReadOnlyList<ValueSetDefinition>>("/CollectionSchema/GetAllValueSets", cancellationToken)
           ?? [];
}
