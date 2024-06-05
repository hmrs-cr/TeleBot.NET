using System.Text;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class PlayerConfigCommand : MusicPlayerCommandBase
{
    public PlayerConfigCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig, ILogger<PlayerConfigCommand> logger) : base(config, tapoConfig, logger)
    {
    }

    public override bool CanExecuteCommand(Message message) =>
        this.ContainsText(message, "music") && this.ContainsText(message, "config");

    public override string Usage => "{music} {config}";

    protected override async Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig currentPlayerConfig, MusicPlayersPresetConfig? musicPlayersPresetConfig, CancellationToken cancellationToken = default)
    {
        var context = message.GetContext();
        var lastString = message.GetLastString()?.Replace('_', ' ').Trim('/') ?? string.Empty;
        if (this.playersConfig?.GetValueOrDefault(lastString) is { } newDefPlayer)
        {
            context.LastPlayerConfig = newDefPlayer;
            await this.Reply(message, $"New default player config: {newDefPlayer.Name}");
        }
        else if (!context.IsPromptReplyMessage)
        {
            var sb = new StringBuilder();
            if (this.playersConfig != null)
            {
                foreach (var confifName in this.playersConfig.Values.Select(v => v.Name).Where(cn => cn is { }))
                {
                    var isDefault = currentPlayerConfig.Name?.Equals(confifName, StringComparison.InvariantCultureIgnoreCase) == true;
                    if (isDefault)
                    {
                        sb.Append("<b>");
                    }
                    sb.Append(confifName);
                    if (isDefault)
                    {
                        sb.Append("</b>");
                    }

                    sb.Append(" ==> /");

                    var l = sb.Length;
                    sb.AppendLine(confifName).Replace(' ', '_', l, confifName!.Length);

                }
            }

            await this.ReplyFormatedPrompt(message, sb.ToString());
        }
        else
        {
            await this.Reply(message, "Not a valid music player name");
        }

        return DoNotReplyPlayerStatus;
    }
}
