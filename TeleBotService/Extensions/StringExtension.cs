using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TeleBotService.Extensions;

public static partial class StringExtension
{
    private static Dictionary<char, char> accentTable = new()
    {
        { 'á', 'a' },
        { 'é', 'e' },
        { 'í', 'i' },
        { 'ó', 'o' },
        { 'ú', 'u' },
        { 'ñ', 'n' },
    };

    public static string RemoveAccents(this string text)
    {
        StringBuilder? sb = null;
        foreach (var accent in accentTable)
        {
            if (text.Contains(accent.Key))
            {
                sb ??= new StringBuilder(text);
                sb.Replace(accent.Key, accent.Value);
            }
        }

        var result = sb?.ToString() ?? text;
        return result;
    }

    public static StringBuilder RemoveAccents(this StringBuilder sb)
    {
        foreach (var accent in accentTable)
        {
            sb.Replace(accent.Key, accent.Value);
        }

        return sb;
    }

    public static string Format(this string template, object tokenValues) =>
        ResolveSquareTokensRegex().Replace(template, match => ResolveTokenValue(match.Groups[1].Value, tokenValues) ?? match.Value);


    private static string? ResolveTokenValue(string token, object tokenValues)
    {
        if (tokenValues is IDictionary<string, string> tokenValuesDict && tokenValuesDict.TryGetValue(token, out var value))
        {
            return value;
        }

        var result = tokenValues.GetType()
                                .GetProperty(token)?
                                .GetValue(tokenValues)?
                                .ToString();

        return result;
    }

    [GeneratedRegex(@"\[(.*?)\]")]
    private static partial Regex ResolveSquareTokensRegex();
}
