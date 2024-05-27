using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicNotificationCommand : MusicPlayerCommandBase
{
    public MusicNotificationCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig) : base(config, tapoConfig)
    {
    }

    public override bool CanExecuteCommand(Message message) => ContainsText(message, "/musicnotify") && (ContainsText(message, "begin") || ContainsText(message, "end"));
    protected override Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig playersConfig, MusicPlayersPresetConfig? musicPlayersPresetConfig, CancellationToken cancellationToken = default)
    {
        if (ContainsText(message, "begin"))
        {
            _ = message.GetContext().NotifyPlayerStatusChanges(playersConfig.Client, async (client, prevStatus, currentStatus) =>
            {
                var same = prevStatus?.Title == currentStatus?.Title;
                // TODO: Do not sent status if stopped
                await ReplyPlayerStatus(message, playersConfig);
                Console.WriteLine($"Player status New: {currentStatus?.Title}, Prev: {prevStatus?.Title}");
            });
        }
        else if (ContainsText(message, "end"))
        {
            _ = message.GetContext().NotifyPlayerStatusChanges(playersConfig.Client, null);
        }

        return DoNotReplyPlayerStatusTask;
    }
}
