using Linkplay.HttpApi;

namespace TeleBotService.Config;

public class PlayersConfig
{
    private LinkplayHttpApiClient? client = null;

    public string? Name { get; init; }
    public string? Host { get; init; }

    public int Timeout { get; init; } = 5;

    public IReadOnlyList<MusicPlayersPresetConfig>? Presets { get; init; }

    public LinkplayHttpApiClient Client => this.client ??= this.SetTimeOut(new LinkplayHttpApiClient(this.Host!, logger: TelebotServiceApp.Logger));

    public int TapoDevice { get; init; }

    private LinkplayHttpApiClient SetTimeOut(LinkplayHttpApiClient linkplayHttpApiClient)
    {
        if (this.Timeout > 0)
        {
            linkplayHttpApiClient.RequestTimeout = TimeSpan.FromSeconds(this.Timeout);
        }

        return linkplayHttpApiClient;
    }

    internal MusicPlayersPresetConfig? GetPreset(string? text) => this.Presets?.FirstOrDefault(p => text?.Contains(p.Name!, StringComparison.InvariantCultureIgnoreCase) == true);
}
