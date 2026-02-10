using System.Net.Http.Json;
using ReportingAgent.Models;

namespace ReportingAgent.Clients;

public sealed class DataQueryClient(HttpClient httpClient)
{
    public async Task<CollectionSampleResponse?> SampleCollectionDataAsync(CollectionSampleRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/DataQuery/SampleCollectionData", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CollectionSampleResponse>(cancellationToken);
    }

    public async Task<BusinessQueryValidationResponse?> ExecuteBusinessQueryAsync(BusinessQueryValidationRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/DataQuery/ExecuteBusinessQuery", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BusinessQueryValidationResponse>(cancellationToken);
    }
}
