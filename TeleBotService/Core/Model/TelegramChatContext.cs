using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace TeleBotService.Model;

public class TelegramChatContext
{
    private static ConcurrentDictionary<ChatContextKey, TelegramChatContext> currentChats = [];

    private readonly ChatContextKey key;

    private TelegramChatContext(ChatContextKey key)
    {
        this.key = key;
    }

    public long? ChatId => this.key.Chat?.Id;

    public string? Username => this.key.Chat?.Username;

    public string LanguageCode { get; set; } = "es";

    public override int GetHashCode() => this.key.GetHashCode();

    public override bool Equals(object? obj) => this.key.Equals(obj);

    public static TelegramChatContext GetContext(Chat chat) => currentChats.GetOrAdd(new (chat), (key) => new TelegramChatContext(key));

    private class ChatContextKey
    {
        public Chat Chat { get; }

        public ChatContextKey(Chat chat)
        {
            this.Chat = chat;
        }

        public override bool Equals(object? obj) => obj is ChatContextKey other && this.Equals(other);

        public bool Equals(ChatContextKey? other) => other?.Chat?.Id == this.Chat?.Id && other?.Chat?.Username == this.Chat?.Username;

        public override int GetHashCode() => HashCode.Combine(this.Chat?.Id, this.Chat?.Username);
    }
}
