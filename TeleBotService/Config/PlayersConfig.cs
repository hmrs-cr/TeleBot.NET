using Linkplay.HttpApi;

namespace TeleBotService.Config;

public class PlayersConfig
{
    private ILinkplayHttpApiClient? client = null;

    public string? Name { get; init; }
    public string? Host { get; init; }

    public int Timeout { get; init; } = 5;

    public IReadOnlyList<MusicPlayersPresetConfig>? Presets { get; init; }

    public ILinkplayHttpApiClient Client =>
        this.client ??= new LinkplayHttpApiClient(this.Host!).SetLogger(TelebotServiceApp.Logger).SetTimeout(TimeSpan.FromSeconds(this.Timeout));

    public int TapoDevice { get; init; }

    internal MusicPlayersPresetConfig? GetPreset(string? text) => this.Presets?.FirstOrDefault(p => text?.Contains(p.Name!, StringComparison.InvariantCultureIgnoreCase) == true);
}
