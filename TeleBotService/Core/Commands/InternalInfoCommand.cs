using System.Reflection;
using System.Text;
using TeleBotService.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService;

public class InternalInfoCommand : TelegramCommand
{
    public override bool CanExecuteCommand(Message message) => ContainsText(message, "/info");

    public override async Task Execute(Message message, CancellationToken cancellationToken = default)
    {
        var myInfo = await this.BotClient.GetMeAsync();
        var internalInfo = GetInternalInfoString(myInfo);
        await this.Reply(message, internalInfo);
    }

    public static string GetInternalInfoString(User user) => new StringBuilder()
          .Append(user.FirstName)
          .Append(" (")
          .Append(user.Username)
          .Append(") V")
          .Append(TelebotServiceApp.Version)
          .ToString();
}
