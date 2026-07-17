using System.Text.Json.Serialization;

namespace Farm.Web.Core.Irrigation;

public record DesignVersionInfo(
    Guid Id,
    [property: JsonPropertyName("zone_id")] Guid ZoneId,
    string? Name,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

/// <summary>
/// Versioned, backend-persisted zone designs. Every Save creates an immutable
/// version; the newest version is the zone's "current design". The designer
/// edits a local working copy and only persists on explicit Save — unsaved
/// edits are the playground mode.
/// </summary>
public interface IZoneDesignStore
{
    /// <summary>The newest saved version's design, or null when none exists.</summary>
    Task<ZoneDesign?> GetLatestAsync(Guid zoneId);

    /// <summary>Saves a new version; returns its info, or null on failure.</summary>
    Task<DesignVersionInfo?> SaveVersionAsync(Guid zoneId, ZoneDesign design, string? name);

    Task<List<DesignVersionInfo>> ListVersionsAsync(Guid zoneId);

    Task<ZoneDesign?> GetVersionAsync(Guid designId);

    /// <summary>Whether the zone has any saved version (drives status labels).</summary>
    Task<bool> HasDesignAsync(Guid zoneId);
}
