using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
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

    protected static TelegramChatContext GetContext(Message message) => TelegramChatContext.GetContext(message.Chat);

    protected static string Localize(Message message, string text) => GetContext(message)?.Localize(text) ?? text;

    protected static bool ContainsText(Message message, string text) =>
        GetContext(message)?.GetLocalizedStrings(text).Append(text).Any(t =>
            message.Text?.Contains(t, StringComparison.InvariantCultureIgnoreCase) == true) == true;

    protected static int? ParseLastInt(Message message)
    {
        var i = message.Text?.LastIndexOf(' ');
        if (i > 0 && int.TryParse(message.Text.AsSpan(i.Value), out var result))
        {
            return result;
        }

        return null;
    }

    protected static Uri? ParseLastUrl(Message message)
    {
        var i = message.Text?.LastIndexOf(' ');
        if (i > 0 && Uri.TryCreate(message.Text.Substring(i.Value), default(UriCreationOptions), out var result))
        {
            return result;
        }

        return null;
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
