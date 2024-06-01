using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TeleBotService.Localization;

public partial class SimpleLocalizationResolver : ILocalizationResolver
{
    public const string DefaulLanguage = "en";

    private CaseInsensitiveTextMappingDictionary? localizedTextMappings = null;

    public SimpleLocalizationResolver(ILogger? logger)
    {
        this.Logger = logger;
    }

    public SimpleLocalizationResolver() { }

    public ILogger? Logger { get; set; }

    public IEnumerable<string>? DefinedLanguages => localizedTextMappings?.Values.SelectMany(v => v.Keys).Append(DefaulLanguage).Distinct();

    public CultureInfo? GetCultureInfo(string cultureName) => CultureInfo.GetCultureInfo(cultureName);

    public string GetLocalizedString(string? cultureName, string textValue, int idx = 0)
    {
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

    internal void LoadStringMappings(string? fileName)
    {
        try
        {
            using var file = File.OpenRead(fileName ?? "localizedStrings.json");
            this.localizedTextMappings = JsonSerializer.Deserialize<CaseInsensitiveTextMappingDictionary>(file);
        }
        catch (Exception e)
        {
            this.Logger?.LogWarning(e, "Error loading localized strings");
        }
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

    private class CaseInsensitiveTextMappingDictionary : Dictionary<string, Dictionary<string, string[]>>
    {
        public CaseInsensitiveTextMappingDictionary() : base(StringComparer.OrdinalIgnoreCase) {}
    }
}

public static class ResgistrationExtensions
{
    public static IServiceCollection AddCommandTextMappings(this IServiceCollection services, IConfiguration config)
    {
        var fileName = config.GetValue<string>("LocalizedStringMappingFile");
        var instance = new SimpleLocalizationResolver(TelebotServiceApp.Logger);
        instance.LoadStringMappings(fileName);
        services.AddSingleton<ILocalizationResolver>(instance);
        return services;
    }
}
