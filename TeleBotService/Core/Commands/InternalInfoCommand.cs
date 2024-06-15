using System.Text;
using TeleBotService.Core.Model;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class InternalInfoCommand : TelegramCommand
{
    public override string CommandString => "info";

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var myInfo = await this.BotClient!.GetMeAsync();
        var internalInfo = GetInternalInfoString(myInfo);
        await this.Reply(messageContext.Message, internalInfo);
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
          .AppendLine()
          .Append("Running at ")
          .Append(TelebotServiceApp.HostName)
          .ToString();
}
