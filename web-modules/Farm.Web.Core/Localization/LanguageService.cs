using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Farm.Web.Core.Localization;

public class LanguageService : ILanguageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _nav;
    private readonly HttpClient _httpClient;

    public IReadOnlyList<(string Code, string DisplayName)> AvailableLanguages { get; } = new List<(string, string)>
    {
        ("en", "English"),
        ("af", "Afrikaans"),
    };

    public string CurrentLanguage => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public LanguageService(IJSRuntime jsRuntime, NavigationManager nav, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _nav = nav;
        _httpClient = httpClient;
    }

    public async Task SetLanguageAsync(string code)
    {
        await _jsRuntime.InvokeVoidAsync("farmCulture.set", code);

        // Best-effort server-side persistence — localStorage (above) remains the
        // fast local cache that drives the synchronous pre-render culture bootstrap
        // in Program.cs, so a failed/offline PATCH here must not block switching.
        try
        {
            await _httpClient.PatchAsJsonAsync("api/v1/core/users/me/language", new { language = code });
        }
        catch (HttpRequestException)
        {
            // Ignored — the client-side switch above already succeeded.
        }

        _nav.NavigateTo(_nav.Uri, forceLoad: true);
    }
}
