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

        if (!string.IsNullOrEmpty(this.User.Language))
        {
            this.Context.LanguageCode = this.User.Language;
        }
    }

    public Message Message { get; }
    public TelegramChatContext Context { get; }
    public UserData User { get; }
}
