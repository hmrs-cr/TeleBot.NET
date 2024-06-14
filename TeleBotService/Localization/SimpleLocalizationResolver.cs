using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TeleBotService.Config;

namespace TeleBotService.Localization;

public partial class SimpleLocalizationResolver : ILocalizationResolver
{
    public const string DefaulLanguage = "en";

    private readonly LocalizedStringsConfig localizedTextMappings;
    private readonly TelegramConfig serviceConfig;

    public SimpleLocalizationResolver(
        IOptions<LocalizedStringsConfig> localizedTextMappings,
        IOptions<TelegramConfig> serviceConfig)
    {
        this.localizedTextMappings = localizedTextMappings.Value;
        this.serviceConfig = serviceConfig.Value;
    }

    public IEnumerable<string>? DefinedLanguages => this.localizedTextMappings?.Values.SelectMany(v => v.Keys).Append(DefaulLanguage).Distinct();

    public CultureInfo? GetCultureInfo(string cultureName) => CultureInfo.GetCultureInfo(cultureName);

    public string GetLocalizedString(string? cultureName, string textValue, int idx = 0)
    {
        cultureName ??= this.serviceConfig.DefaultLanguageCode;
        if (cultureName is null)
        {
            return textValue;
        }

        var localizedText = this.localizedTextMappings?.GetValueOrDefault(textValue)?.GetValueOrDefault(cultureName);
        var value = localizedText?.Skip(idx).FirstOrDefault() ?? textValue;
        return ResolveTokens(value, cultureName, idx, this.localizedTextMappings);
    }

    public IEnumerable<string> GetLocalizedStrings(string cultureName, string textValue)
    {
        var localizedStrings = this.localizedTextMappings?.GetValueOrDefault(textValue)?.GetValueOrDefault(cultureName);
        return localizedStrings ?? Enumerable.Repeat(textValue, 1);
    }

    internal static string ResolveTokens(string template, string cultureName, int idx, Dictionary<string, Dictionary<string, string[]>>? replacements)
    {
        if (template.Contains('{') && template.Contains('}'))
        {
            return ResolveCurlyTokensRegex().Replace(template, match => replacements?.GetValueOrDefault(match.Groups[1].Value)?
                                                                                     .GetValueOrDefault(cultureName)?
                                                                                     .Skip(idx)?.FirstOrDefault() ?? match.Value.TrimStart('{').TrimEnd('}'));
        }

        return template;
    }

    [GeneratedRegex(@"{(.*?)}")]
    private static partial Regex ResolveCurlyTokensRegex();
}
