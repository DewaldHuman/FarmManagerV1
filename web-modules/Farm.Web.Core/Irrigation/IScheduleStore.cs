using System.Text.Json.Serialization;

namespace Farm.Web.Core.Irrigation;

/// <summary>
/// A zone's agronomic irrigation schedule. The four inputs are entered by the
/// user; interval_days + readily_available_water_mm are computed client-side by
/// SchedulingCalculators and stored verbatim (the backend never recomputes).
/// </summary>
public record ScheduleDto(
    [property: JsonPropertyName("available_water_mm_per_metre")] double AvailableWaterMmPerMetre,
    [property: JsonPropertyName("root_depth_metres")] double RootDepthMetres,
    [property: JsonPropertyName("allowable_depletion_percent")] double AllowableDepletionPercent,
    [property: JsonPropertyName("peak_water_use_mm_per_day")] double PeakWaterUseMmPerDay,
    [property: JsonPropertyName("interval_days")] double IntervalDays,
    [property: JsonPropertyName("readily_available_water_mm")] double ReadilyAvailableWaterMm,
    [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt = null);

/// <summary>
/// Per-zone schedule store. Lives in Farm.Web.Core (eager) because
/// ZoneOverview.razor reads it — same DI rule as IZoneDesignStore.
/// </summary>
public interface IScheduleStore
{
    Task<ScheduleDto?> GetAsync(Guid zoneId);

    /// <summary>Creates or replaces the zone's schedule; null on failure.</summary>
    Task<ScheduleDto?> UpsertAsync(Guid zoneId, ScheduleDto schedule);
}
