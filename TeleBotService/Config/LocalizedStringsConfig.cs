namespace TeleBotService;

public class LocalizedStringsConfig : Dictionary<string, Dictionary<string, string[]>>
{
    public const string LocalizedStringsConfigName = "LocalizedStrings";

    public LocalizedStringsConfig() : base(StringComparer.OrdinalIgnoreCase) { }
}