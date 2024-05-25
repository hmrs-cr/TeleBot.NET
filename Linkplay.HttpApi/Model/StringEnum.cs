namespace Linkplay.HttpApi.Model;

public class StringEnum
{
    private readonly string value;

    protected StringEnum(string value)
    {
        this.value = value;
    }

    public override string ToString() => this.value;

    public static bool operator !=(StringEnum left, StringEnum right) => !(left == right);

    public static bool operator ==(StringEnum left, StringEnum right) => left.Equals(right);

    public override int GetHashCode() => EqualityComparer<string>.Default.GetHashCode(this.value);

    public override bool Equals(object obj) => obj is StringEnum @enum && this.Equals(@enum);

    public bool Equals(StringEnum other) => EqualityComparer<string>.Default.Equals(value, other.value);

    public static IEnumerable<StringEnum> GetValues<T>() where T : StringEnum =>
        typeof(T).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                 .Where(f => f.FieldType.IsAssignableTo(typeof(StringEnum)))
                 .Select(f => f.GetValue(null))
                 .Cast<StringEnum>();
}