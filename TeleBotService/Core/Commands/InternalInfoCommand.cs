using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class InternalInfoCommand : TelegramCommand
{
    public override string CommandString => "info";

    protected override async Task Execute(Message message, CancellationToken cancellationToken = default)
    {
        var myInfo = await this.BotClient!.GetMeAsync();
        var internalInfo = GetInternalInfoString(myInfo);
        await this.Reply(message, internalInfo);
    }

    public static string GetInternalInfoString(User user) => new StringBuilder()
          .Append(user.FirstName)
          .Append(" (")
          .Append(user.Username)
          .Append(") V")
          .Append(TelebotServiceApp.Version)
          .Append('-')
          .Append(TelebotServiceApp.VersionLabel)
          .Append('-')
          .Append(TelebotServiceApp.VersionHash)
          .ToString();
}
