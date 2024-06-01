namespace TeleBotService.Config;

public record MusicPlayersPresetConfig
{
    public string? Name { get; init; }
    public uint Index { get; init; }
    public Uri? Url { get; init; }
}
