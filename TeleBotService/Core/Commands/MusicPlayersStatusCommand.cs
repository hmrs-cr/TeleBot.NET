using Microsoft.Extensions.Options;
using TeleBotService.Config;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayersStatusCommand : MusicPlayerCommandBase
{
    public MusicPlayersStatusCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig) : base(config, tapoConfig) { }

    public override string Description => "Tell what's playing";

    public override string Usage => "Playing now";

    public override bool CanExecuteCommand(Message message) => 
        ContainsText(message, "Playing now");

    protected override Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default) => ReplyPlayerStatusDelayShortTask;
}
