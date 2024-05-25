using TeleBotService.Model;
using Telegram.Bot.Types;

namespace TeleBotService.Extensions;

public static class MessageExtensions
{
    public static TelegramChatContext GetContext(this Message message) => TelegramChatContext.GetContext(message.Chat);
}
