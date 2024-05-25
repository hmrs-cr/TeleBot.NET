using System.Reflection;
using System.Text.RegularExpressions;

namespace TeleBotService;

public static partial class StringExtension
{
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
