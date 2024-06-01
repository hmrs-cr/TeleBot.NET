namespace TeleBotService.Config;

public record MusicPlayersConfig
{
    public const string MusicPlayersConfigName = "MusicPlayers";

    private IReadOnlyDictionary<string, PlayersConfig>? playersDict;

    public IReadOnlyList<PlayersConfig>? Players { get; init; }

    public IReadOnlyDictionary<string, PlayersConfig>? PlayersDict => this.playersDict ??= this.Players?.ToDictionary(p => p.Name!);
}
