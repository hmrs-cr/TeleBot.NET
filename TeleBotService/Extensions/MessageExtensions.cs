using TeleBotService.Core;
using TeleBotService.Core.Model;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Extensions;

public static class MessageExtensions
{
    public static TResult GetCommandContextData<TResult>(this Message message, TelegramCommand command) where TResult : class, new() =>
        message.GetContext().GetCommandContextData<TResult>(command);

    public static TelegramChatContext GetContext(this Message message) => TelegramChatContext.GetContext(message.Chat);

    public static Task Reply(this TelegramBotClient botClient, Message message, string replyMessage, CancellationToken cancellationToken = default) => botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: replyMessage,
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken);

    public static Task ReplyFormated(this TelegramBotClient botClient, Message message, string replyMessage, CancellationToken cancellationToken = default) => botClient.SendTextMessageAsync(
               chatId: message.Chat.Id,
               text: replyMessage,
               replyToMessageId: message.MessageId,
               parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
               cancellationToken: cancellationToken);

    public static int? ParseLastInt(this Message message)
    {
        var i = message.Text?.LastIndexOf(' ');
        if (i > 0 && int.TryParse(message.Text.AsSpan(i.Value), out var result))
        {
            return result;
        }

        return null;
    }

    public static Uri? ParseLastUrl(this Message message) => Uri.TryCreate(message.GetLastString(), default, out var result) ? result : null;

     public static string? GetLastString(this Message message)
    {
        var i = message.Text?.LastIndexOf(' ');
        return i > 0 ? message.Text?[(i.Value+1)..] : null;
    }
}
