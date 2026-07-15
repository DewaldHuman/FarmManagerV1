using System.Text.Json.Serialization;

namespace Farm.Web.Core.Auth;

public record UserDto(
    Guid Id,
    string Username,
    [property: JsonPropertyName("display_name")] string DisplayName,
    string Role,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("preferred_language")] string PreferredLanguage);

public record LanguagePreferenceUpdateRequest(string Language);
