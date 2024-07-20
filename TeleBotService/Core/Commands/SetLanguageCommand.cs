using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class SetLanguageCommand : TelegramCommand
{
    public override string CommandString => "setlang";

    protected override Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        var lang = new { lang = message.GetLastString()?.Trim('/') };
        var definedLanguages = this.LocalizationResolver?.DefinedLanguages ?? [];
        if (lang.lang is { } && definedLanguages.Contains(lang.lang) == true)
        {
            messageContext.Context.LanguageCode = lang.lang;
            messageContext.User.SetSetting(nameof(UserData.Language), messageContext.Context.LanguageCode);
            var localizedText = this.Localize(message, "Hello, the new language is '[lang]'");
            this.Reply(messageContext, localizedText.Format(lang), cancellationToken);
        }
        else if (lang.lang is null)
        {
            this.ReplyPrompt(messageContext, "Choose your language", definedLanguages, cancellationToken);
        }
        else
        {
            var localizedText = this.Localize(message, "Language '[lang]' not found");
            this.Reply(messageContext, localizedText.Format(lang), cancellationToken);
        }

        return Task.CompletedTask;
    }
}
