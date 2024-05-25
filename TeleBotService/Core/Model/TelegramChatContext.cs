using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace TeleBotService;

public class TelegramChatContext
{
    private static ConcurrentDictionary<long, TelegramChatContext> currentChats = [];

    public TelegramChatContext(long id)
    {
        this.ChatId = id;
    }

    public long ChatId { get; }
    public string CultureName { get; set; } = "es";

    public string Localize(string text) => SimpleLocalizationResolver.Default?.GetLocalizedString(this.CultureName, text) ?? text;

    public IEnumerable<string> GetLocalizedStrings(string text) => SimpleLocalizationResolver.Default?.GetLocalizedStrings(this.CultureName, text) ?? Enumerable.Repeat(text, 1);

    public static TelegramChatContext GetContext(Chat chat) => currentChats.GetOrAdd(chat.Id, (id) => new TelegramChatContext(id));
}
