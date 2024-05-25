using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot.Requests;

namespace TeleBotService;

public partial class SimpleLocalizationResolver
{
    private Dictionary<string, Dictionary<string, string[]>>? localizedTextMappings = null;

    public static SimpleLocalizationResolver Default { get; private set; }

    public string? CultureName { get; set; }

    public CultureInfo? CultureInfo => this.GetCultureInfo(this.CultureName);


    public string this[string text] => this.GetLocalizedString(this.CultureName, text);

    public CultureInfo? GetCultureInfo(string cultureName) => CultureInfo.GetCultureInfo(cultureName);

    public string GetLocalizedString(string cultureName, string textValue, int idx = 0) 
    {
       var localizedText = this.localizedTextMappings?.GetValueOrDefault(textValue)?.GetValueOrDefault(cultureName);       
       var value = localizedText?.Skip(idx).FirstOrDefault() ?? textValue;
       if (this.localizedTextMappings != null && value.Contains('{') && value.Contains('}')) 
       {
            value = ResolveTokens(value, cultureName, idx, this.localizedTextMappings);
       }

       return value;
    }

    public IEnumerable<string> GetLocalizedStrings(string cultureName, string textValue)
    {
       var localizedStrings = this.localizedTextMappings?.GetValueOrDefault(textValue)?.GetValueOrDefault(cultureName);
       return localizedStrings ?? Enumerable.Repeat(textValue, 1);
    }

    internal static string ResolveTokens(string template, string cultureName, int idx, Dictionary<string, Dictionary<string, string[]>> replacements) =>
        ResolveCurlyTokensRegex().Replace(template, match => replacements.GetValueOrDefault(match.Groups[1].Value)?.GetValueOrDefault(cultureName)?.Skip(idx)?.FirstOrDefault() ??  match.Value.TrimStart('{').TrimEnd('}'));

    internal static string ResolveSquareTokens(string template, Dictionary<string, string> tokenValues) => 
        ResolveSquareTokensRegex().Replace(template, match => tokenValues.GetValueOrDefault(match.Groups[1].Value) ??  match.Value.TrimStart('[').TrimEnd(']'));


    internal static void InitDefaultInstance(string fileName) 
    {
        Default = new SimpleLocalizationResolver();
         try
        {
            using var file = File.OpenRead(fileName);
            Default.localizedTextMappings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string[]>>>(file);
            Default.localizedTextMappings = new(Default.localizedTextMappings, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading localized strings: {e.Message}");
        }
    }

    [GeneratedRegex(@"[(.*?)]")]
    private static partial Regex ResolveSquareTokensRegex();

    [GeneratedRegex(@"{(.*?)}")]
    private static partial Regex ResolveCurlyTokensRegex();
}
