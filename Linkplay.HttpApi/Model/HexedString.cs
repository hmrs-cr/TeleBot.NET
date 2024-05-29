using System.Globalization;
using System.Text;

namespace Linkplay.HttpApi.Model;

public struct HexedString
{
    private readonly string value;

    public HexedString(string? hexedValue)
    {
        hexedValue ??= string.Empty;
        try
        {
            this.value = HexedToString(hexedValue);
        }
        catch
        {
            this.value = hexedValue;
        }
    }

    public static string HexedToString(string hexString)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < hexString.Length; i += 2)
        {
            var hs = hexString.AsSpan(i, 2);
            var decval = uint.Parse(hs, NumberStyles.HexNumber);
            var character = Convert.ToChar(decval);
            sb.Append(character);
        }

        return sb.ToString();
    }

    public static bool operator !=(HexedString left, HexedString right) => !(left == right);

    public static bool operator ==(HexedString left, HexedString right) => left.Equals(right);

    public override int GetHashCode() => EqualityComparer<string>.Default.GetHashCode(this.value);

    public override bool Equals(object? obj) => obj is HexedString other && this.Equals(other);

    public override string ToString() => this.value;

    public bool Equals(HexedString other) => EqualityComparer<string>.Default.Equals(value, other.value);
}
