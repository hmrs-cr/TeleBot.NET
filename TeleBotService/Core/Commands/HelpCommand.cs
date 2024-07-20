using System.Runtime.CompilerServices;
using System.Text;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class HelpCommand : TelegramCommand
{
    private readonly ITelegramService telegramService;

    public HelpCommand(ITelegramService telegramService)
    {
        this.telegramService = telegramService;
    }

    public override string CommandString => "help";

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        var sb = new StringBuilder();
        foreach (var commandGroup in this.telegramService
                                    .GetCommands()
                                    .Where(c => c.IsEnabled && !string.IsNullOrEmpty(c.Usage) && !string.IsNullOrEmpty(c.Description) && !c.IsAdmin)
                                    .GroupBy(c =>  c.Description))
        {
            sb.Append("<b>").Append(Localize(message, commandGroup.Key)).Append(':').Append("</b>")
              .AppendLine();

            foreach(var command in commandGroup)
            {
                this.AddCommandUssage(command, message, sb);
            }

            sb.AppendLine();
        }

        var otherCommands = this.telegramService.GetCommands().Where(c => !string.IsNullOrEmpty(c.Usage) && string.IsNullOrEmpty(c.Description) && !c.IsAdmin).ToList();
        if (otherCommands.Count > 0)
        {
            sb.Append("<b>").Append(Localize(message, "Other Commands")).Append(':').Append("</b>")
              .AppendLine();

            foreach (var command in otherCommands)
            {
                this.AddCommandUssage(command, message, sb);
            }
        }

        if (messageContext.User.IsAdmin)
        {
            sb.AppendLine();
            var adminCommands = this.telegramService.GetCommands().Where(c => c.IsAdmin).ToList();
            if (adminCommands.Count > 0)
            {
                sb.Append("<b>").Append(Localize(message, "Admin Commands")).Append(':').Append("</b>")
                .AppendLine();

                foreach (var command in adminCommands)
                {
                    this.AddCommandUssage(command, message, sb);
                }
            }
        }

        await this.ReplyFormated(messageContext, sb.RemoveAccents().ToString(), cancellationToken);
    }

    private void AddCommandUssage(ITelegramCommand command, Message message, StringBuilder sb)
    {
        foreach (var cmd in command.Usage.Split('\n'))
        {
            var cmdLocalized = Localize(message, cmd);
            sb.Append("<i>").Append(cmdLocalized).Append("</i>");
            if (!cmdLocalized.StartsWith('/'))
            {
                sb.Append("\t==>\t/").Append(cmdLocalized.Replace(" ", string.Empty));
            }
            sb.AppendLine();
        }
    }
}