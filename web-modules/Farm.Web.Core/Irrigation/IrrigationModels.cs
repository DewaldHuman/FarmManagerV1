using System.Text.Json.Serialization;

namespace Farm.Web.Core.Irrigation;

public record CalculationRunCreateRequest(
    [property: JsonPropertyName("zone_id")] Guid ZoneId,
    [property: JsonPropertyName("calculator_type")] string CalculatorType,
    Dictionary<string, double> Inputs,
    Dictionary<string, double> Outputs);

public record CalculationRunDto(
    Guid Id,
    [property: JsonPropertyName("zone_id")] Guid ZoneId,
    [property: JsonPropertyName("zone_name")] string ZoneName,
    [property: JsonPropertyName("calculator_type")] string CalculatorType,
    Dictionary<string, double> Inputs,
    Dictionary<string, double> Outputs,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);
