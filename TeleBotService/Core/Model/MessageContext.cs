using TeleBotService.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Model;

public class MessageContext
{
    internal MessageContext(ITelegramBotClient botClient, Message message, UserData user)
    {
        this.BotClient = botClient;
        this.Message = message;
        this.Context = TelegramChatContext.GetContext(message.Chat);
        this.User = user;

        if (this.User.GeStringSetting(nameof(UserData.Language), this.User.Language) is { } lang)
        {
            this.Context.LanguageCode = lang;
        }
    }

    public ITelegramBotClient BotClient { get; }
    public Message Message { get; }
    public TelegramChatContext Context { get; }
    public UserData User { get; }
}
