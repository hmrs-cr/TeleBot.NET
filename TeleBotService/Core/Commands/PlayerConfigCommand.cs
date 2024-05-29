using System.Text;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class PlayerConfigCommand : MusicPlayerCommandBase
{
    public PlayerConfigCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig) : base(config, tapoConfig)
    {
    }

    public override bool CanExecuteCommand(Message message) =>
        this.ContainsText(message, "music") && this.ContainsText(message, "config");

    protected override async Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig playersConfig, MusicPlayersPresetConfig? musicPlayersPresetConfig, CancellationToken cancellationToken = default)
    {
        if (this.ContainsText(message, "set default"))
        {
            message.GetContext().LastPlayerConfig = playersConfig;
            await this.Reply(message, $"New default player config: {playersConfig.Name}");
        }
        else
        {
            var deafultConfig = message.GetContext().LastPlayerConfig;
            var sb = new StringBuilder();
            if (this.playersConfig != null)
            {
                foreach (var confifName in this.playersConfig.Values.Select(v => v.Name))
                {
                    var isDefault = deafultConfig?.Name?.Equals(confifName, StringComparison.InvariantCultureIgnoreCase) == true;
                    if (isDefault)
                    {
                        sb.Append("<b>");
                    }
                    sb.AppendLine(confifName);
                    if (isDefault)
                    {
                        sb.Append("</b>");
                    }
                }
            }

            await this.ReplyFormated(message, sb.ToString());
        }

        return DoNotReplyPlayerStatus;
    }
}
