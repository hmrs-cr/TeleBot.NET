namespace Linkplay.HttpApi.Model;
public record InfolistEntry
{
    public HexedString Filename { get; init; }
    public string? Totlen { get; init; }
    public HexedString Title { get; init; }
    public HexedString Artist { get; init; }
    public HexedString Album { get; init; }
}