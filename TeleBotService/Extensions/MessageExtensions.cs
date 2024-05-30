using System.Buffers;
using System.Collections.ObjectModel;
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

    public static IReadOnlyList<int> ParseIntArray(this Message message, char separator = ' ')
    {
        var messageText = message.Text ?? string.Empty;

        var results = new List<int>();
        var separatorLength = 1;

        var i = messageText.Length;
        int prevIndex = message.Text?.Length ?? 0;

        while ((i = messageText.LastIndexOf(separator, i - 1)) > 0)
        {
            var len = prevIndex - separatorLength - i;
            var span = message.Text.AsSpan(i + separatorLength, len);

            if (int.TryParse(span, out var result))
            {
                results.Insert(0, result);
            }

            prevIndex = i;
        }

        return results;
    }

    public static Uri? ParseLastUrl(this Message message) => Uri.TryCreate(message.GetLastString(), default, out var result) ? result : null;

    public static string? GetLastString(this Message message)
    {
        var i = message.Text?.LastIndexOf(' ');
        return i > 0 ? message.Text?[(i.Value + 1)..] : null;
    }
}
