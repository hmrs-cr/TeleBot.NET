using System.Text;
using TeleBotService.Core.Model;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class InternalInfoCommand : TelegramCommand
{
    private readonly ITelegramService telegramService;

    public override string CommandString => "info";

    public InternalInfoCommand(ITelegramService telegramService)
    {
        this.telegramService = telegramService;
    }

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var myInfo = await this.telegramService.GetInfo();
        var internalInfo = GetInternalInfoString(myInfo);
        await this.Reply(messageContext, internalInfo);
    }

    public static string GetInternalInfoString(User user) => new StringBuilder()
          .Append(user.FirstName)
          .Append(" (")
          .Append(user.Username)
          .Append('@')
          .Append(TelebotServiceApp.HostName)
          .Append(") V")
          .Append(TelebotServiceApp.Version)
          .Append('-')
          .Append(TelebotServiceApp.VersionLabel)
          .Append('-')
          .Append(TelebotServiceApp.VersionHash)
          .ToString();
}
