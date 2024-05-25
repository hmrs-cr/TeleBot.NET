namespace TeleBotService.Localization;

public interface ILocalizationResolver
{
    IEnumerable<string>? DefinedLanguages { get; }

    string GetLocalizedString(string? cultureName, string textValue, int idx = 0);
    IEnumerable<string> GetLocalizedStrings(string cultureName, string textValue);
}
