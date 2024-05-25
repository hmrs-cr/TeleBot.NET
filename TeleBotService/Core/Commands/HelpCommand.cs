using System.Text;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class HelpCommand : TelegramCommand
{
    private readonly ITelegramService telegramService;

    public HelpCommand(ITelegramService telegramService)
    {
        this.telegramService = telegramService;
    }

    public override bool CanExecuteCommand(Message message) => ContainsText(message, "help");

    public override async Task Execute(Message message, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        foreach (var command in this.telegramService.GetCommands().Where(c => c.IsEnabled && !string.IsNullOrEmpty(c.Usage)))
        {
            sb.Append("<b>").Append(Localize(message, command.Description)).Append(':').Append("</b>")
              .AppendLine();

            foreach (var cmd in command.Usage.Split('\n'))
            {
                var cmdLocalized = Localize(message, cmd);
                sb.Append("<i>").Append(cmdLocalized).Append(" --> </i>/").Append(cmdLocalized.Replace(" ", string.Empty));
                sb.AppendLine();
            }
            sb.AppendLine();
        }

        await this.ReplyFormated(message, sb.ToString(), cancellationToken);
    }
}