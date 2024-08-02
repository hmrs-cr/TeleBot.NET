using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Humanizer;

namespace TeleBotService.Extensions;

public static partial class StringExtension
{
    private static readonly Dictionary<char, char> accentTable = new()
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

    public static string? Capitalize(this string? value)
    {
        if (string.IsNullOrEmpty(value) || char.IsUpper(value[0]))
        {
            return value;
        }

        var sb = new StringBuilder(value.Length);
        sb.Append(char.ToUpper(value[0]));
        sb.Append(value.AsSpan(1));
        return sb.ToString();
    }

    public static StringBuilder AppendSpaces(this StringBuilder sb, int count) => sb.Append(' ', count);

    public static ReadOnlySpan<char> GetPartAt(this string? value, char separator, int at)
    {
        var i = 1;
        var enumerator = value.SplitEnumerated(separator);
        while (enumerator.MoveNext() && ++i <= at) { }
        if (i - 1 == at)
        {
            return enumerator.Current;
        }

        return [];
    }

    public static SpanSeparatorEnumerator SplitEnumerated(this ReadOnlySpan<char> value, char separator, int count = -1, bool removeEmpty = false) =>
        new (value, separator, count, removeEmpty);

    public static SpanSeparatorEnumerator SplitEnumerated(this string? value, char separator, int count = -1, bool removeEmpty = false) =>
        new (value, separator, count, removeEmpty);

    public ref struct SpanSeparatorEnumerator
    {
        private readonly char separator;
        private readonly int count;
        private readonly bool removeEmpty;


        private int separatorPosition = -1;
        private ReadOnlySpan<char> remaining;
        private ReadOnlySpan<char> current;

        internal SpanSeparatorEnumerator(ReadOnlySpan<char> value, char separator, int count, bool removeEmpty)
        {
            this.remaining = value;
            this.separator = separator;
            this.count = count;
            this.removeEmpty = removeEmpty;
        }

        public int Count { get; private set; }

        public readonly ReadOnlySpan<char> Current => this.current;

        public readonly SpanSeparatorEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (this.remaining.IsEmpty)
            {
                return false;
            }

            this.separatorPosition = remaining.IndexOf(separator);
            if (this.separatorPosition < 0 || this.Count + 1 == this.count)
            {
                separatorPosition = remaining.Length;
            }

            if (this.removeEmpty && this.separatorPosition == 0)
            {
                this.UpdateRemaining();
                return this.MoveNext();
            }

            this.current = this.remaining[..this.separatorPosition];
            this.UpdateRemaining();

            this.Count++;
            return true;
        }

        private void UpdateRemaining() =>
            this.remaining = this.separatorPosition == remaining.Length ? [] : this.remaining.Slice(this.separatorPosition + 1);
    }

    [GeneratedRegex(@"\[(.*?)\]")]
    private static partial Regex ResolveSquareTokensRegex();
}
