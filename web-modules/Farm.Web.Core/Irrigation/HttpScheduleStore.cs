using System.Net.Http.Json;

namespace Farm.Web.Core.Irrigation;

/// <summary>
/// Backend-persisted schedule store over /api/v1/irrigation/zones/{id}/schedule.
/// GET returns 200 + null body when unset; the read is best-effort (null on
/// failure — it feeds a status card), while UpsertAsync surfaces failure as null.
/// </summary>
public class HttpScheduleStore : IScheduleStore
{
    private readonly HttpClient _httpClient;

    public HttpScheduleStore(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ScheduleDto?> GetAsync(Guid zoneId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ScheduleDto?>(
                $"api/v1/irrigation/zones/{zoneId}/schedule");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ScheduleDto?> UpsertAsync(Guid zoneId, ScheduleDto schedule)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/v1/irrigation/zones/{zoneId}/schedule", schedule);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ScheduleDto>();
        }
        catch
        {
            return null;
        }
    }
}
