using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Farm.Web.Core.Irrigation;

/// <summary>
/// Backend-persisted design store over /api/v1/irrigation/zones/{id}/designs.
/// The ZoneDesign payload is serialized client-side (default camelCase JSON)
/// and stored verbatim by the backend — only this client ever interprets it.
/// Reads are best-effort (null/empty on failure — they feed status labels);
/// SaveVersionAsync surfaces failure as null so the designer can say so.
/// </summary>
public class HttpZoneDesignStore : IZoneDesignStore
{
    private readonly HttpClient _httpClient;

    public HttpZoneDesignStore(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private sealed record SaveRequest(string? Name, ZoneDesign Design);

    private sealed record VersionRead(
        Guid Id,
        [property: JsonPropertyName("zone_id")] Guid ZoneId,
        string? Name,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
        ZoneDesign? Design);

    public async Task<ZoneDesign?> GetLatestAsync(Guid zoneId)
    {
        try
        {
            var latest = await _httpClient.GetFromJsonAsync<VersionRead?>(
                $"api/v1/irrigation/zones/{zoneId}/designs/latest");
            return latest?.Design;
        }
        catch
        {
            return null;
        }
    }

    public async Task<DesignVersionInfo?> SaveVersionAsync(Guid zoneId, ZoneDesign design, string? name)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/v1/irrigation/zones/{zoneId}/designs",
                new SaveRequest(string.IsNullOrWhiteSpace(name) ? null : name.Trim(), design));
            response.EnsureSuccessStatusCode();
            var saved = await response.Content.ReadFromJsonAsync<VersionRead>();
            return saved is null ? null : new DesignVersionInfo(saved.Id, saved.ZoneId, saved.Name, saved.CreatedAt);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<DesignVersionInfo>> ListVersionsAsync(Guid zoneId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<DesignVersionInfo>>(
                $"api/v1/irrigation/zones/{zoneId}/designs") ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<ZoneDesign?> GetVersionAsync(Guid designId)
    {
        try
        {
            var version = await _httpClient.GetFromJsonAsync<VersionRead>($"api/v1/irrigation/designs/{designId}");
            return version?.Design;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> HasDesignAsync(Guid zoneId) => await GetLatestAsync(zoneId) is not null;
}
