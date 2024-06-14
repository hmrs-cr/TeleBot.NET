namespace TeleBotService;

public class LocalizedStringsConfig : Dictionary<string, Dictionary<string, string[]>>
{
    public string? DefaultLang { get; set; }
    public const string LocalizedStringsConfigName = "LocalizedStrings";

    public LocalizedStringsConfig() : base(StringComparer.OrdinalIgnoreCase) { }
}