using System.Collections.Concurrent;
using Linkplay.HttpApi;
using Linkplay.HttpApi.Model;
using TeleBotService.Config;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Model;

public class TelegramChatContext
{
    private object executingTasksLock = new();
    private List<Task> executingTasks = new();

    private static ConcurrentDictionary<ChatContextKey, TelegramChatContext> currentChats = [];

    private ConcurrentDictionary<Type, ICollection<object>> commandContextData = [];

    private readonly ChatContextKey key;

    private CancellationTokenSource? playerStatusNotificationCts;

    private TelegramChatContext(ChatContextKey key)
    {
        this.key = key;
    }

    public long? ChatId => this.key.Chat?.Id;

    public string? Username => this.key.Chat?.Username;

    public string LanguageCode { get; set; } = "es";

    public ILogger? Logger { get; set; } = TelebotServiceApp.Logger;

    public TResult GetCommandContextData<TResult>(TelegramCommand command) where TResult : class, new()
    {
        var contextDataList = this.commandContextData.GetOrAdd(command.GetType(), k => []);
        var commandContextData = contextDataList.FirstOrDefault(c => c is TResult);
        if (commandContextData == null)
        {
            commandContextData = new TResult();
            contextDataList.Add(commandContextData);
        }

        return (TResult)commandContextData;
    }


    public bool IsNotifingPlayerStatusChanges => this.playerStatusNotificationCts != null && !this.playerStatusNotificationCts.IsCancellationRequested;

    public PlayersConfig? LastPlayerConfig { get; internal set; }
    public Message? LastPromptMessage { get; internal set; }
    public bool IsPromptReplyMessage { get; internal set; }

    public override int GetHashCode() => this.key.GetHashCode();

    public override bool Equals(object? obj) => this.key.Equals(obj);

    public static TelegramChatContext GetContext(Chat chat) => currentChats.GetOrAdd(new(chat), (key) => new TelegramChatContext(key));

    public async Task NotifyPlayerStatusChanges(ILinkplayHttpApiClient client, Action<ILinkplayHttpApiClient, PlayerStatus?, PlayerStatus?>? notify)
    {
        if (notify == null)
        {
            if (this.IsNotifingPlayerStatusChanges)
            {
                this.playerStatusNotificationCts?.Cancel();
                this.Logger?.LogInformation("Unregistered player status change notification");
            }
        }
        else if (this.playerStatusNotificationCts == null || this.playerStatusNotificationCts.IsCancellationRequested)
        {
            this.Logger?.LogInformation("Starting player status change notification");
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


                        this.Logger?.LogInformation($"Status notification delay: {delay / 1000}s");
                        await Task.Delay(delay, this.playerStatusNotificationCts.Token);
                    }
                }
                catch (Exception)
                {
                    // Ignored
                }
            }

            this.Logger?.LogInformation("Ending player status change notification");
        }
    }

    internal void AddExecutingTask(Task task)
    {
        if (!task.IsCompleted && !task.IsCanceled && !task.IsFaulted && task.Status != TaskStatus.RanToCompletion)
        {
            lock (this.executingTasksLock)
            {
                this.executingTasks.Add(task);
            }

            this.Logger?.LogInformation("Added Task. Total tasks tracked for {key}: {executingTaskCount}", this.key, this.executingTasks.Count);
        }
    }

    internal int GetExecutingTaskCount()
    {
        int result;
        lock (this.executingTasksLock)
        {
            result = this.executingTasks.Count;
        }
        return result;
    }

    internal void RemoveFinishedTasks()
    {
        lock (this.executingTasksLock)
        {
            this.executingTasks.RemoveAll(t => t.IsCanceled || t.IsCompleted || t.IsFaulted || t.Status == TaskStatus.RanToCompletion);
            this.Logger?.LogInformation("Removed Tasks. Total tasks tracked for {key}: {executingTaskCount}", this.key, this.executingTasks.Count);
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

        public override string ToString() => $"{this.Chat.Username}.{this.Chat.Id}";
    }
}
