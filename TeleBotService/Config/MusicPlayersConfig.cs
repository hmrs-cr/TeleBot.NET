using Linkplay.HttpApi;

namespace TeleBotService.Config;

public class PlayersConfig
{
    private LinkplayHttpApiClient client = null;

    public string Name { get; set; }
    public string Host { get; set; }

    public int Timeout { get; set; } = 5;

    public IReadOnlyList<MusicPlayersPresetConfig> Presets { get; set; }

    public LinkplayHttpApiClient Client => this.client ??= this.SetTimeOut(new LinkplayHttpApiClient(this.Host));

    public int TapoDevice { get; set; }

    private LinkplayHttpApiClient SetTimeOut(LinkplayHttpApiClient linkplayHttpApiClient)
    {
        if (this.Timeout > 0)
        {
            linkplayHttpApiClient.RequestTimeout = TimeSpan.FromSeconds(this.Timeout);
        }

        return linkplayHttpApiClient;
    }

    internal MusicPlayersPresetConfig? GetPreset(string? text) => this.Presets?.FirstOrDefault(p => text?.Contains(p.Name, StringComparison.InvariantCultureIgnoreCase) == true);
}

public record MusicPlayersPresetConfig
{
    public string Name { get; set; }
    public uint Index { get; set; }

    public Uri Url { get; set; }
}

public record MusicPlayersConfig
{
    public const string MusicPlayersConfigName = "MusicPlayers";
    public IReadOnlyList<PlayersConfig> Players { get; set; }
}
