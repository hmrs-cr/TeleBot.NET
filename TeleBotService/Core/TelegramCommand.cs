﻿using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using TeleBotService.Extensions;
using TeleBotService.Localization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core;

public abstract class TelegramCommand : ITelegramCommand
{
    [JsonIgnore]
    public TelegramBotClient BotClient { get; protected set; }

    public virtual bool IsEnabled => true;

    public virtual string Name => this.GetType().Name;

    public virtual string Description => string.Empty;

    public virtual string Usage => string.Empty;

[   JsonIgnore]
    public ILocalizationResolver? LocalizationResolver { get; protected set; }

    public abstract Task Execute(Message message, CancellationToken cancellationToken = default);
    public abstract bool CanExecuteCommand(Message message);

    protected Task Reply(Message message, string replyMessage, CancellationToken cancellationToken = default) => this.BotClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: Localize(message, replyMessage),
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken);

    protected Task ReplyFormated(Message message, string replyMessage, CancellationToken cancellationToken = default) => this.BotClient.SendTextMessageAsync(
               chatId: message.Chat.Id,
               text: replyMessage,
               replyToMessageId: message.MessageId,
               parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
               cancellationToken: cancellationToken);

    protected string Localize(Message message, string text) => this.LocalizationResolver?.GetLocalizedString(message.GetContext().LanguageCode, text) ?? text;

    protected bool ContainsText(Message message, string text, bool ignoreSpaces = false)
    {
        var result = (this.LocalizationResolver?.GetLocalizedStrings(message.GetContext().LanguageCode, text) ?? []).Append(text).Any(t =>
            message.Text?.Contains(t, StringComparison.InvariantCultureIgnoreCase) == true) == true;

        if (!result && ignoreSpaces)
        {
            result = (this.LocalizationResolver?.GetLocalizedStrings(message.GetContext().LanguageCode, text) ?? []).Append(text).Any(t =>
                message.Text?.Contains(t.Replace(" ", null), StringComparison.InvariantCultureIgnoreCase) == true) == true;
        }

        return result;
    }
}

public static class TelegramCommandRegistrationExtensions
{
    internal static IEnumerable<Type> CommandTypes => AppDomain.CurrentDomain
                                                               .GetAssemblies()
                                                               .SelectMany(a => a.GetTypes())
                                                               .Where(t => !t.IsAbstract && t.IsClass && t.IsAssignableTo(typeof(ITelegramCommand)));

    public static IServiceCollection RegisterTelegramCommands(this IServiceCollection serviceDescriptors)
    {
        foreach (var commandType in CommandTypes)
        {
            serviceDescriptors.AddSingleton(commandType);
        }

        return serviceDescriptors;
    }
}
