using System.Text.Json.Serialization;
using TeleBotService.Extensions;
using TeleBotService.Localization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core;

public abstract class TelegramCommand : ITelegramCommand
{
    private readonly Task<bool> TaskTrueResult = Task.FromResult(true);
    private readonly Task<bool> TaskFalseResult = Task.FromResult(false);

    [JsonIgnore]
    public TelegramBotClient BotClient { get; protected set; }

    public virtual bool IsEnabled => true;

    public virtual string Name => this.GetType().Name;

    public virtual string Description => string.Empty;

    public virtual string Usage => string.Empty;

    public virtual bool CanBeExecuteConcurrently => false;

[   JsonIgnore]
    public ILocalizationResolver? LocalizationResolver { get; protected set; }

    public abstract bool CanExecuteCommand(Message message);

    public async Task<bool> HandleCommand(Message message, CancellationToken cancellationToken)
    {
        var canExecute = await this.StartExecuting(message, cancellationToken);
        try
        {
            if (canExecute)
            {
                var task = this.Execute(message, cancellationToken);
                message.GetContext().AddExecutingTask(task);
                await task;
                return true;
            }
        }
        finally
        {
            if (canExecute)
            {
                await this.EndExecuting(message, cancellationToken);
                message.GetContext().RemoveFinishedTasks();
            }
        }

        return canExecute;
    }

    protected virtual Task<bool> StartExecuting(Message message, CancellationToken token)
    {
        if (!this.CanBeExecuteConcurrently && message.GetContext().GetExecutingTaskCount() > 0)
        {
            return TaskFalseResult;
        }

        return TaskTrueResult;
    }

    protected abstract Task Execute(Message message, CancellationToken cancellationToken = default);

    protected virtual Task EndExecuting(Message message, CancellationToken token) => Task.CompletedTask;

    protected Task Reply(Message message, string replyMessage, CancellationToken cancellationToken = default) => this.BotClient?.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: Localize(message, replyMessage),
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken) ?? Task.CompletedTask;

    protected Task ReplyFormated(Message message, string replyMessage, CancellationToken cancellationToken = default) => this.BotClient?.SendTextMessageAsync(
               chatId: message.Chat.Id,
               text: replyMessage,
               replyToMessageId: message.MessageId,
               parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
               cancellationToken: cancellationToken) ?? Task.CompletedTask;

    protected async Task AudioReply(Message message, string audioFileName, string? title = null, int? duration = null, bool deleteFile = false)
    {
        using var file = System.IO.File.OpenRead(audioFileName);
        await this.AudioReply(message, file, title, duration);
        if (deleteFile)
        {
            System.IO.File.Delete(audioFileName);
        }
    }

     protected Task AudioReply(Message message, Stream audioStream, string? title = null, int? duration = null) =>
        this.BotClient.SendAudioAsync(chatId: message.Chat.Id, replyToMessageId: message.MessageId, title: title, duration: duration, audio: InputFile.FromStream(audioStream));

    protected string Localize(Message message, string text) => this.LocalizationResolver?.GetLocalizedString(message.GetContext().LanguageCode, text) ?? text;

    protected bool ContainsText(Message message, string text, bool ignoreSpaces = false)
    {
        var result = (this.LocalizationResolver?.GetLocalizedStrings(message.GetContext().LanguageCode, text) ?? []).Append(text).Any(t =>
            message.Text?.Contains(t, StringComparison.InvariantCultureIgnoreCase) == true) == true;

        if (!result && ignoreSpaces)
        {
            // TODO: find a more efficient way to implement this
            result = (this.LocalizationResolver?.GetLocalizedStrings(message.GetContext().LanguageCode, text) ?? []).Append(text).Any(t =>
                message.Text?.Contains(t.Replace(" ", null), StringComparison.InvariantCultureIgnoreCase) == true) == true;
        }

        return result;
    }
}

public static class TelegramCommandRegistrationExtensions
{
    internal static IEnumerable<Type> CommandTypes => AppDomain.CurrentDomain
                                                               .GetAssemblies()
                                                               .SelectMany(a => a.GetTypes())
                                                               .Where(t => !t.IsAbstract && t.IsClass && t.IsAssignableTo(typeof(ITelegramCommand)));

    public static IServiceCollection RegisterTelegramCommands(this IServiceCollection serviceDescriptors)
    {
        foreach (var commandType in CommandTypes)
        {
            serviceDescriptors.AddSingleton(commandType);
        }

        return serviceDescriptors;
    }
}
