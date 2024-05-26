using System.Collections.Concurrent;
using Linkplay.HttpApi;
using Linkplay.HttpApi.Model;
using Telegram.Bot.Types;

namespace TeleBotService.Model;

public class TelegramChatContext
{
    private static ConcurrentDictionary<ChatContextKey, TelegramChatContext> currentChats = [];

    private readonly ChatContextKey key;

    private CancellationTokenSource? playerStatusNotificationCts;

    private TelegramChatContext(ChatContextKey key)
    {
        this.key = key;
    }

    public long? ChatId => this.key.Chat?.Id;

    public string? Username => this.key.Chat?.Username;

    public string LanguageCode { get; set; } = "en";
    public bool IsNotifingPlayerStatusChanges => this.playerStatusNotificationCts != null && !this.playerStatusNotificationCts.IsCancellationRequested;
    public override int GetHashCode() => this.key.GetHashCode();

    public override bool Equals(object? obj) => this.key.Equals(obj);

    public static TelegramChatContext GetContext(Chat chat) => currentChats.GetOrAdd(new (chat), (key) => new TelegramChatContext(key));

    public async Task NotifyPlayerStatusChanges(LinkplayHttpApiClient client, Action<LinkplayHttpApiClient, PlayerStatus?, PlayerStatus?>? notify)
    {
        if (notify == null)
        {
            if (this.IsNotifingPlayerStatusChanges)
            {
                this.playerStatusNotificationCts.Cancel();
                Console.WriteLine("Unregistered player status change notification");
            }
        }
        else if (this.playerStatusNotificationCts == null || this.playerStatusNotificationCts.IsCancellationRequested)
        {
            Console.WriteLine("Starting player status change notification");
            this.playerStatusNotificationCts = new();
            PlayerStatus? prevPlayerStatus = null;
            while (!this.playerStatusNotificationCts.IsCancellationRequested)
            {
                try
                {
                    var status = await client.GetPlayerStatus();
                    if (status == null)
                    {
                        if (prevPlayerStatus != null)
                        {
                            prevPlayerStatus = null;
                            notify(client, prevPlayerStatus, null);
                        }
                        await Task.Delay(10000, this.playerStatusNotificationCts.Token);
                    }
                    else
                    {
                        var delay = ((status.Totlen - status.Curpos) / 2) + 1500;

                        if (prevPlayerStatus?.Title != status.Title ||
                            prevPlayerStatus?.Artist != status.Artist ||
                              prevPlayerStatus?.Album != status.Album ||
                                prevPlayerStatus?.Status != status.Status)
                        {
                            notify(client, prevPlayerStatus, status);
                            prevPlayerStatus = status;
                        }


                        Console.WriteLine($"Status notification delay: {delay / 1000}s");
                        await Task.Delay(delay, this.playerStatusNotificationCts.Token);
                    }
                }
                catch (Exception e)
                {
                }
            }

            Console.WriteLine("Ending player status change notification");
        }
    }

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
