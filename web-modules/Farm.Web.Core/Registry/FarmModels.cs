using System.Text.Json.Serialization;

namespace Farm.Web.Core.Registry;

public record FarmDto(Guid Id, string Name);

public record FieldDto(
    Guid Id,
    [property: JsonPropertyName("farm_id")] Guid FarmId,
    string Name,
    [property: JsonPropertyName("area_ha")] double AreaHa,
    string? Notes,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

public record FieldCreateRequest(
    string Name,
    [property: JsonPropertyName("area_ha")] double AreaHa,
    string? Notes);

public record FieldUpdateRequest(
    string? Name,
    [property: JsonPropertyName("area_ha")] double? AreaHa,
    string? Notes);

public record ZoneDto(
    Guid Id,
    [property: JsonPropertyName("field_id")] Guid FieldId,
    [property: JsonPropertyName("field_name")] string FieldName,
    string Name,
    [property: JsonPropertyName("area_ha")] double AreaHa,
    string? Crop,
    [property: JsonPropertyName("irrigation_system_type")] string IrrigationSystemType,
    [property: JsonPropertyName("irrigation_interval_days")] int? IrrigationIntervalDays,
    [property: JsonPropertyName("is_active")] bool IsActive,
    string Status,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

public record ZoneCreateRequest(
    [property: JsonPropertyName("field_id")] Guid FieldId,
    string Name,
    [property: JsonPropertyName("area_ha")] double AreaHa,
    string? Crop,
    [property: JsonPropertyName("irrigation_system_type")] string IrrigationSystemType,
    [property: JsonPropertyName("irrigation_interval_days")] int? IrrigationIntervalDays);

public record ZoneUpdateRequest(
    [property: JsonPropertyName("field_id")] Guid? FieldId,
    string? Name,
    [property: JsonPropertyName("area_ha")] double? AreaHa,
    string? Crop,
    [property: JsonPropertyName("irrigation_system_type")] string? IrrigationSystemType,
    [property: JsonPropertyName("irrigation_interval_days")] int? IrrigationIntervalDays);

public record SettingsDto(
    Guid Id,
    string Timezone,
    [property: JsonPropertyName("kc_establishment")] double KcEstablishment,
    [property: JsonPropertyName("kc_midseason")] double KcMidseason,
    [property: JsonPropertyName("kc_lateseason")] double KcLateseason);

public record SettingsUpdateRequest(
    string? Timezone,
    [property: JsonPropertyName("kc_establishment")] double? KcEstablishment,
    [property: JsonPropertyName("kc_midseason")] double? KcMidseason,
    [property: JsonPropertyName("kc_lateseason")] double? KcLateseason);
