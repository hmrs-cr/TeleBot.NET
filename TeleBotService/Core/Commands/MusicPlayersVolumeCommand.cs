using Microsoft.Extensions.Options;
using TeleBotService.Config;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayersVolumeCommand : MusicPlayerCommandBase
{
    public MusicPlayersVolumeCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig, ILogger<MusicPlayersVolumeCommand> logger) : base(config, tapoConfig, logger) { }

    public override string Description => "Control music volume";

    public override string Usage => "{music} {volume} {up}\n{music} {volume} {down}\n{music} {volume} {low}\n{music} {volume} {half}\n{music} {volume} {high}";

    public override bool CanExecuteCommand(Message message) =>
        ContainsText(message, "volume") && ContainsText(message, "music") &&
        (ContainsText(message, "up") || ContainsText(message, "down") || ContainsText(message, "high") || ContainsText(message, "half") || ContainsText(message, "low"));

    protected override async Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default)
    {
        if (ContainsText(message, "up"))
        {
            await this.ExecutePlayerClientCommand(message, playerConfig, (pc) => pc.Client.PlayerVolumeUp());
        }
        else if (ContainsText(message, "down"))
        {
            await this.ExecutePlayerClientCommand(message, playerConfig, (pc) => pc.Client.PlayerVolumeDown());
        }
        else if (ContainsText(message, "low"))
        {
            await this.ExecutePlayerClientCommand(message, playerConfig, (pc) => pc.Client.PlayerSetVolume(33));
        }
        else if (ContainsText(message, "half"))
        {
            await this.ExecutePlayerClientCommand(message, playerConfig, (pc) => pc.Client.PlayerSetVolume(50));
        }
        else if (ContainsText(message, "high"))
        {
            await this.ExecutePlayerClientCommand(message, playerConfig, (pc) => pc.Client.PlayerSetVolume(88));
        }

        return ReplyPlayerStatusDelayShort;
    }
}
