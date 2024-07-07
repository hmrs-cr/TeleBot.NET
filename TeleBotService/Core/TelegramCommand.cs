using System.Text.Json.Serialization;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using TeleBotService.Localization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core;

public abstract class TelegramCommand : ITelegramCommand
{
    private readonly Task<bool> TaskTrueResult = Task.FromResult(true);
    private readonly Task<bool> TaskFalseResult = Task.FromResult(false);

    private bool isExecuting;

    [JsonIgnore]
    public ITelegramBotClient? BotClient { get; private set; }

    public virtual bool IsEnabled => true;

    public virtual bool IsAdmin => false;

    public virtual string Name => this.GetType().Name;

    public virtual string Description => string.Empty;

    public virtual string Usage => this.CommandString;

    public virtual string CommandString => string.Empty;

[   JsonIgnore]
    public ILocalizationResolver? LocalizationResolver { get; private set; }

    public virtual bool CanExecuteCommand(Message message) => !string.IsNullOrEmpty(this.CommandString) && this.ContainsText(message, this.CommandString);

    public virtual void Configure(IConfiguration config) {  }

    protected ILogger? Logger { get; init; }

    public async Task<bool> HandleCommand(MessageContext messageContext, CancellationToken cancellationToken)
    {
        var context = messageContext.Context;
        var canExecute = await this.StartExecuting(messageContext, cancellationToken);
        try
        {
            if (canExecute)
            {
                this.isExecuting = true;
                var task = this.Execute(messageContext, cancellationToken);
                context.AddExecutingTask(task);
                await task;
                return true;
            }
        }
        finally
        {
            if (canExecute)
            {
                this.isExecuting = false;
                await this.EndExecuting(messageContext, cancellationToken);
                context.IsPromptReplyMessage = false;
                context.RemoveFinishedTasks();
            }
        }

        return canExecute;
    }

    protected virtual Task<bool> StartExecuting(MessageContext messageContext, CancellationToken token)
    {
        if (this.isExecuting)
        {
            return TaskFalseResult;
        }

        if (this.IsAdmin && !messageContext.User.IsAdmin)
        {
            return TaskFalseResult;
        }

        return TaskTrueResult;
    }

    protected abstract Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default);

    protected virtual Task EndExecuting(MessageContext messageContext, CancellationToken token) => Task.CompletedTask;

    protected Task Reply(Message message, string replyMessage, CancellationToken cancellationToken = default) => this.BotClient?.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: Localize(message, replyMessage),
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken) ?? Task.CompletedTask;

    protected Task ReplyPrompt(Message message, string prompt, IEnumerable<string> choices, CancellationToken cancellationToken = default)
    {
        message.GetContext().LastPromptMessage = message;
        prompt = Localize(message, prompt);
        return this.Reply(message, $"{prompt} : /{string.Join(" /", choices)}", cancellationToken);
    }

    protected Task ReplyFormatedPrompt(Message message, string prompt, IEnumerable<string>? choices = null, CancellationToken cancellationToken = default)
    {
        message.GetContext().LastPromptMessage = message;
        if (choices != null)
        {
            prompt = Localize(message, prompt);
            return this.ReplyFormated(message, $"{prompt} : /{string.Join(" /", choices)}", cancellationToken);
        }

        return this.ReplyFormated(message, prompt, cancellationToken);
    }

    protected Task ReplyFormated(Message message, string replyMessage, CancellationToken cancellationToken = default) => this.BotClient?.SendTextMessageAsync(
               chatId: message.Chat.Id,
               text: replyMessage,
               replyToMessageId: message.MessageId,
               parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
               cancellationToken: cancellationToken) ?? Task.CompletedTask;
    protected Task ReplyFormated(long chatId, string replyMessage, CancellationToken cancellationToken = default) => this.BotClient?.SendTextMessageAsync(
               chatId: chatId,
               text: replyMessage,
               replyToMessageId: null,
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

    protected Task AudioReply(Message message, Stream audioStream, string? title = null, int? duration = null)
    {
        if (title == "voice-memo")
        {
            return this.BotClient!.SendVoiceAsync(chatId: message.Chat.Id, replyToMessageId: message.MessageId, duration: duration, voice: InputFile.FromStream(audioStream));
        }

        return this.BotClient!.SendAudioAsync(chatId: message.Chat.Id, replyToMessageId: message.MessageId, title: title, duration: duration, audio: InputFile.FromStream(audioStream));

    }

    protected string Localize(Message message, string text) => this.LocalizationResolver?.GetLocalizedString(message.GetContext().LanguageCode, text) ?? text;

    protected bool ContainsText(Message message, string text, bool ignoreSpaces = false)
    {
        var result = (this.LocalizationResolver?.GetLocalizedStrings(message.GetContext().LanguageCode!, text) ?? []).Append(text).Any(t =>
            message.Text?.Contains(t, StringComparison.InvariantCultureIgnoreCase) == true) == true;

        if (!result && ignoreSpaces)
        {
            // TODO: find a more efficient way to implement this
            result = (this.LocalizationResolver?.GetLocalizedStrings(message.GetContext().LanguageCode!, text) ?? []).Append(text).Any(t =>
                message.Text?.Contains(t.Replace(" ", null), StringComparison.InvariantCultureIgnoreCase) == true) == true;
        }

        return result;
    }

    public void LogDebug(string message, params object?[] args) => this.Logger?.LogDebug(message, args);
    public void LogDebug(string message) => this.Logger?.LogDebug(message);
    public void LogInformation(string message, params object?[] args) => this.Logger?.LogInformation(message, args);
    public void LogInformation(string message) => this.Logger?.LogInformation(message);
    public void LogWarning(string message, params object?[] args) => this.Logger?.LogWarning(message, args);
    public void LogWarning(Exception e, string message, params object?[] args) => this.Logger?.LogWarning(e, message, args);
    public void LogWarning(Exception e, string message) => this.Logger?.LogWarning(e, message);
    public void LogWarning(string message) => this.Logger?.LogWarning(message);
    public void LogError(string message, params object?[] args) => this.Logger?.LogError(message, args);
    public void LogError(Exception e, string message, params object?[] args) => this.Logger?.LogError(e, message, args);
    public void LogError(Exception e, string message) => this.Logger?.LogError(e, message);
    public void LogError(string message) => this.Logger?.LogError(message);

    internal TelegramCommand Init(ITelegramBotClient botClient, ILocalizationResolver localizationResolver, IConfiguration configuration)
    {
        this.BotClient = botClient;
        this.LocalizationResolver = localizationResolver;
        this.Configure(configuration);
        return this;
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
