namespace Farm.Web.Core.Irrigation;

/// <summary>
/// Storage seam for zone irrigation designs. The current implementation is
/// in-memory only (designs survive in-app navigation, not a refresh) — the
/// async signatures exist so a future HTTP/backend-persisted store is a
/// drop-in replacement.
/// </summary>
public interface IZoneDesignStore
{
    Task<ZoneDesign?> GetAsync(Guid zoneId);

    Task SaveAsync(Guid zoneId, ZoneDesign design);

    Task<bool> HasDesignAsync(Guid zoneId);
}
