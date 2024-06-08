using TeleBotService.Config;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Model;

public class MessageContext
{
    internal MessageContext(Message message, UserData user)
    {
        this.Message = message;
        this.Context = TelegramChatContext.GetContext(message.Chat);
        this.User = user;
    }

    public Message Message { get; }
    public TelegramChatContext Context { get; }
    public UserData User { get; }
}
