using System.Text.Json.Serialization;
using TeleBotService.Config;
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

    public virtual bool IsEnabled => true;

    public virtual bool IsAdmin => false;

    public virtual string Name => this.GetType().Name;

    public virtual string Description => string.Empty;

    public virtual string Usage => this.CommandString;

    public virtual string CommandString => string.Empty;

[   JsonIgnore]
    public ILocalizationResolver? LocalizationResolver { get; private set; }

    public virtual bool CanExecuteCommand(Message message) => !string.IsNullOrEmpty(this.CommandString) && this.ContainsText(message, this.CommandString);

    protected ILogger? Logger { get; init; }

    public async Task<bool> HandleCommand(MessageContext messageContext, CancellationToken cancellationToken)
    {
        var context = messageContext.Context;
        var canExecute = await this.StartExecuting(messageContext, cancellationToken);
        try
        {
            if (canExecute)
            {
                if (messageContext.Message.Chat.Id > 0)
                {
                    messageContext.User.SetSetting(UserData.ChatIdKeyName, messageContext.Message.Chat.Id);
                }

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

    protected Task Reply(MessageContext context, string replyMessage, CancellationToken cancellationToken = default) => context.BotClient?.SendTextMessageAsync(
                chatId: context.Message.Chat.Id,
                text: Localize(context.Message, replyMessage),
                replyToMessageId: context.Message.MessageId,
                cancellationToken: cancellationToken) ?? Task.CompletedTask;

    protected Task ReplyPrompt(MessageContext context, string prompt, IEnumerable<string> choices, CancellationToken cancellationToken = default)
    {
        var message = context.Message;
        message.GetContext().LastPromptMessage = message;
        prompt = Localize(message, prompt);
        return this.Reply(context, $"{prompt} : /{string.Join(" /", choices)}", cancellationToken);
    }

    protected Task ReplyFormatedPrompt(MessageContext context, string prompt, IEnumerable<string>? choices = null, CancellationToken cancellationToken = default)
    {
        var message = context.Message;
        message.GetContext().LastPromptMessage = message;
        if (choices != null)
        {
            prompt = Localize(message, prompt);
            return this.ReplyFormated(context, $"{prompt} : /{string.Join(" /", choices)}", cancellationToken);
        }

        return this.ReplyFormated(context, prompt, cancellationToken);
    }

    protected Task ReplyFormated(MessageContext context, string replyMessage, CancellationToken cancellationToken = default) => context.BotClient?.SendTextMessageAsync(
               chatId: context.Message.Chat.Id,
               text: replyMessage,
               replyToMessageId: context.Message.MessageId,
               parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
               cancellationToken: cancellationToken) ?? Task.CompletedTask;
    protected Task ReplyFormated(ITelegramBotClient botClient, long chatId, string replyMessage, CancellationToken cancellationToken = default) => botClient?.SendTextMessageAsync(
               chatId: chatId,
               text: replyMessage,
               replyToMessageId: null,
               parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
               cancellationToken: cancellationToken) ?? Task.CompletedTask;

    protected async Task AudioReply(MessageContext context, string audioFileName, string? title = null, int? duration = null, bool deleteFile = false)
    {
        using var file = System.IO.File.OpenRead(audioFileName);
        await this.AudioReply(context, file, title, duration);
        if (deleteFile)
        {
            System.IO.File.Delete(audioFileName);
        }
    }

    protected Task AudioReply(MessageContext context, Stream audioStream, string? title = null, int? duration = null)
    {
        if (title == "voice-memo")
        {
            return context.BotClient!.SendVoiceAsync(chatId: context.Message.Chat.Id, replyToMessageId: context.Message.MessageId, duration: duration, voice: InputFile.FromStream(audioStream));
        }

        return context.BotClient!.SendAudioAsync(chatId: context.Message.Chat.Id, replyToMessageId: context.Message.MessageId, title: title, duration: duration, audio: InputFile.FromStream(audioStream));

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
    public void LogSimpleException(Exception e, string message) => this.Logger?.LogSimpleException(message, e);
    public void LogWarning(string message) => this.Logger?.LogWarning(message);
    public void LogError(string message, params object?[] args) => this.Logger?.LogError(message, args);
    public void LogError(Exception e, string message, params object?[] args) => this.Logger?.LogError(e, message, args);
    public void LogError(Exception e, string message) => this.Logger?.LogError(e, message);
    public void LogError(string message) => this.Logger?.LogError(message);

    internal TelegramCommand Init(ILocalizationResolver localizationResolver)
    {
        this.LocalizationResolver = localizationResolver;
        return this;
    }
}

public static class TelegramCommandRegistrationExtensions
{
    internal static IEnumerable<Type> CommandTypes => AppDomain.CurrentDomain
                                                               .GetAssemblies()
                                                               .SelectMany(a => a.GetTypes())
                                                               .Where(t => t is { IsAbstract: false, IsClass: true } && t.IsAssignableTo(typeof(ITelegramCommand)));

    public static IServiceCollection RegisterTelegramCommands(this IServiceCollection serviceDescriptors)
    {
        foreach (var commandType in CommandTypes)
        {
            serviceDescriptors.AddSingleton(commandType);
        }

        return serviceDescriptors;
    }
}
