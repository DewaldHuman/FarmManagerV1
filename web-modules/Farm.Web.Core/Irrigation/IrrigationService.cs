using System.Net.Http.Json;

namespace Farm.Web.Core.Irrigation;

public class IrrigationService : IIrrigationService
{
    private readonly HttpClient _httpClient;

    public IrrigationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CalculationRunDto?> LogCalculationRunAsync(
        Guid zoneId, string calculatorType, Dictionary<string, double> inputs, Dictionary<string, double> outputs)
    {
        var request = new CalculationRunCreateRequest(zoneId, calculatorType, inputs, outputs);
        var response = await _httpClient.PostAsJsonAsync("api/v1/irrigation/calculation-runs", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CalculationRunDto>();
    }

    public async Task<List<CalculationRunDto>> ListCalculationRunsAsync(Guid zoneId) =>
        await _httpClient.GetFromJsonAsync<List<CalculationRunDto>>($"api/v1/irrigation/calculation-runs?zone_id={zoneId}") ?? new();
}
