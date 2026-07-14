using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Farm.Web.Core.Localization;

public class LanguageService : ILanguageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _nav;

    public IReadOnlyList<(string Code, string DisplayName)> AvailableLanguages { get; } = new List<(string, string)>
    {
        ("en", "English"),
        ("af", "Afrikaans"),
    };

    public string CurrentLanguage => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public LanguageService(IJSRuntime jsRuntime, NavigationManager nav)
    {
        _jsRuntime = jsRuntime;
        _nav = nav;
    }

    public async Task SetLanguageAsync(string code)
    {
        // Client-side only for now — no Users/Auth backend exists yet.
        // Once Core: Users + Auth lands, extend this to also persist
        // server-side (e.g. PATCH /api/v1/core/users/me/language);
        // ILanguageService's public shape shouldn't need to change.
        await _jsRuntime.InvokeVoidAsync("farmCulture.set", code);
        _nav.NavigateTo(_nav.Uri, forceLoad: true);
    }
}
