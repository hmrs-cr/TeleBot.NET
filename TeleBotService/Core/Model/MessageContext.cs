using TeleBotService.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Model;

public class MessageContext
{
    private readonly ITelegramService mainService;

    internal MessageContext(ITelegramService mainService, ITelegramBotClient botClient, Message message, UserData user)
    {
        this.mainService = mainService;
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

    public Task<string?> ExecuteCommand(string commandLine, bool sentReply = true, CancellationToken ct = default) =>
        this.mainService.ExecuteCommand(commandLine, this.User.UserName ?? string.Empty, sentReply, 
            chatId: Context.ChatId, messageId: Message.MessageId, cancellationToken: ct);
}
