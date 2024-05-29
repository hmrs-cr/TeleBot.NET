using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class SetLanguageCommand : TelegramCommand
{
    public override bool CanExecuteCommand(Message message) => message.Text?.StartsWith("/setlang") == true;

    protected override Task Execute(Message message, CancellationToken cancellationToken = default)
    {
        var lang = new { lang = message.GetLastString() };
        if (lang.lang is { } && this.LocalizationResolver?.DefinedLanguages?.Contains(lang.lang) == true)
        {
            message.GetContext().LanguageCode = lang.lang;
            var localizedText = this.Localize(message, "Hello, the new language is '[lang]'");
            this.Reply(message, localizedText.Format(lang), cancellationToken);
        }
        else
        {
            var localizedText = this.Localize(message, "Language '[lang]' not found");
            this.Reply(message, localizedText.Format(lang), cancellationToken);
        }

        return Task.CompletedTask;
    }
}
