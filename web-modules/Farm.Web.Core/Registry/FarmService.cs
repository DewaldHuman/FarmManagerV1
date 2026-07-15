using System.Net.Http.Json;

namespace Farm.Web.Core.Registry;

public class FarmService : IFarmService
{
    private readonly HttpClient _httpClient;

    public FarmService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FarmDto?> GetFarmAsync() =>
        await _httpClient.GetFromJsonAsync<FarmDto>("api/v1/core/farm");

    public async Task<List<FieldDto>> ListFieldsAsync() =>
        await _httpClient.GetFromJsonAsync<List<FieldDto>>("api/v1/core/fields") ?? new();

    public async Task<FieldDto?> GetFieldAsync(Guid id) =>
        await _httpClient.GetFromJsonAsync<FieldDto>($"api/v1/core/fields/{id}");

    public async Task<FieldDto?> CreateFieldAsync(FieldCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/core/fields", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FieldDto>();
    }

    public async Task<FieldDto?> UpdateFieldAsync(Guid id, FieldUpdateRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync($"api/v1/core/fields/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FieldDto>();
    }

    public async Task<bool> ArchiveFieldAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/core/fields/{id}/archive", content: null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ZoneDto>> ListZonesAsync(Guid? fieldId = null)
    {
        var url = fieldId is null ? "api/v1/core/zones" : $"api/v1/core/zones?field_id={fieldId}";
        return await _httpClient.GetFromJsonAsync<List<ZoneDto>>(url) ?? new();
    }

    public async Task<ZoneDto?> GetZoneAsync(Guid id) =>
        await _httpClient.GetFromJsonAsync<ZoneDto>($"api/v1/core/zones/{id}");

    public async Task<ZoneDto?> CreateZoneAsync(ZoneCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/core/zones", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ZoneDto>();
    }

    public async Task<ZoneDto?> UpdateZoneAsync(Guid id, ZoneUpdateRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync($"api/v1/core/zones/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ZoneDto>();
    }

    public async Task<bool> ArchiveZoneAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/core/zones/{id}/archive", content: null);
        return response.IsSuccessStatusCode;
    }

    public async Task<SettingsDto?> GetSettingsAsync() =>
        await _httpClient.GetFromJsonAsync<SettingsDto>("api/v1/core/settings");

    public async Task<SettingsDto?> UpdateSettingsAsync(SettingsUpdateRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync("api/v1/core/settings", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SettingsDto>();
    }
}
