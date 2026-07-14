namespace Farm.Web.Core.Localization;

public interface ILanguageService
{
    string CurrentLanguage { get; }

    IReadOnlyList<(string Code, string DisplayName)> AvailableLanguages { get; }

    Task SetLanguageAsync(string code);
}
