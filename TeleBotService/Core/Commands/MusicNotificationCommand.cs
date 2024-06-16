using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicNotificationCommand : MusicPlayerCommandBase
{
    public MusicNotificationCommand(
        IOptions<MusicPlayersConfig> config,
        IOptions<TapoConfig> tapoConfig,
        ILogger<MusicNotificationCommand> logger) : base(config, tapoConfig, logger)
    {
    }

    public override bool CanExecuteCommand(Message message) => ContainsText(message, "musicnotify") && (ContainsText(message, "begin") || ContainsText(message, "end"));

    public override string Usage => "musicnotify_begin\nmusicnotify_end";

    public override string Description => "Enable/disable music notifications";

    public override bool IsAdmin => true;

    protected override Task<int> ExecuteMusicPlayerCommand(MessageContext messageContext, PlayersConfig playersConfig, MusicPlayersPresetConfig? musicPlayersPresetConfig, CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        if (ContainsText(message, "begin"))
        {
            _ = messageContext.Context.NotifyPlayerStatusChanges(playersConfig.Client, async (client, prevStatus, currentStatus) =>
            {
                var same = prevStatus?.Title == currentStatus?.Title;
                // TODO: Do not sent status if stopped
                await ReplyPlayerStatus(message, playersConfig);
                this.LogInformation("Player status New: {currentStatusTitle}, Prev: {prevStatusTitle}", currentStatus?.Title, prevStatus?.Title);
            });
        }
        else if (ContainsText(message, "end"))
        {
            _ = messageContext.Context.NotifyPlayerStatusChanges(playersConfig.Client, null);
        }

        return DoNotReplyPlayerStatusTask;
    }
}
