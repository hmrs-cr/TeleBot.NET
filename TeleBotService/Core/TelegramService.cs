using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Commands;
using TeleBotService.Extensions;
using TeleBotService.Localization;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBotService.Core;

public class TelegramService : ITelegramService
{
    private readonly ITelegramBotClient botClient;
    private readonly TelegramConfig config;
    private readonly CancellationTokenSource cts = new();
    private readonly ReceiverOptions receiverOptions = new()
    {
        AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
    };

    private IReadOnlyCollection<ITelegramCommand>? commandInstances;
    private readonly ILocalizationResolver localizationResolver;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<TelegramService> logger;

    public TelegramService(
        IOptions<TelegramConfig> confif,
        ILocalizationResolver localizationResolver,
        IServiceProvider serviceProvider,
        ILogger<TelegramService> logger)
    {
        if (string.IsNullOrEmpty(confif.Value.BotToken) || confif.Value.BotToken.Length < 10)
        {
            throw new ApplicationException("No Telegram BotToken configured.");
        }

        this.botClient = new TelegramBotClient(confif.Value.BotToken);
        this.config = confif.Value;
        this.localizationResolver = localizationResolver;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public Task<User> GetInfo() => this.botClient.GetMeAsync();

    public IEnumerable<ITelegramCommand> GetCommands() => this.commandInstances ??= this.GetCommandInstances();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        _ = this.SetUpCommandsIfNeeded();

        if (!TelebotServiceApp.IsDev)
        {
            _ = this.SentAdminMessage($"Service started: {InternalInfoCommand.GetInternalInfoString(await this.botClient.GetMeAsync())}", cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!TelebotServiceApp.IsDev)
        {
            await this.SentAdminMessage($"Service stopped: {InternalInfoCommand.GetInternalInfoString(await this.botClient.GetMeAsync())}", cancellationToken);
        }
        cts.Cancel();
        cts.Dispose();
    }

    private Task SetUpCommandsIfNeeded()
    {
        /*var commands = await this.botClient.GetMyCommandsAsync();
        if (commands.Length == 0)
        {
        }*/
        return Task.CompletedTask;
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return Task.CompletedTask;
        // Only process text messages
        if (message.Text is not { } messageText)
            return Task.CompletedTask;

        if (!this.config.AllowedUsers.Contains(message.Chat.Username))
        {
            this.logger.LogInformation("Forbidden {messageChatUsername}:{messageChatId}", message.Chat.Username, message.Chat.Id);
            _ = this.botClient.Reply(message, "Who are you?", cancellationToken);
            return Task.CompletedTask;
        }

        this.logger.LogInformation("Received '{messageText}' message in chat {messageChatId}.", messageText, message.Chat.Id);
        _ = HandleCommands(message, cancellationToken);

        return Task.CompletedTask;
    }

    private async Task HandleCommands(Message message, CancellationToken cancellationToken)
    {
        var executedCommandCount = 0;
        var failedCommandCount = 0;
        var refusedCommandCount = 0;
        var executed = false;
        var commands = this.GetCommands(message);
        foreach (var command in commands)
        {
            try
            {
                executed = await command.HandleCommand(message, cancellationToken);
                if (executed)
                {
                    this.logger.LogInformation("Executed '{commandName}' command in chat {messageChatId}.", command.Name, message.Chat.Id);
                    executedCommandCount++;
                }
                else
                {
                    this.logger.LogInformation("Refused execution of '{commandName}' command in chat {messageChatId}.", command.Name, message.Chat.Id);
                    refusedCommandCount++;
                }
            }
            catch (Exception e)
            {
                failedCommandCount++;
                this.logger.LogWarning(e, "Unhandled error while executing command {command.Name}", command.Name);
            }
        }

        if (executedCommandCount > 0)
        {
            this.logger.LogInformation("Executed {executedCommandCount} commands: '{messageText}', chat {messageChatId}.", executedCommandCount, message.Text, message.Chat.Id);
        }
        else if (failedCommandCount == 0 && refusedCommandCount == 0)
        {
            this.logger.LogInformation("No commands found for message '{messageText}', chat {messageChatId}.", message.Text, message.Chat.Id);
            await this.Reply(message, "I didn't understand you", cancellationToken);
        }
    }

    private Task Reply(Message message, string text, CancellationToken cancellationToken) => this.botClient.Reply(message, this.localizationResolver.GetLocalizedString(message.GetContext().LanguageCode, text), cancellationToken);

    private async Task SentAdminMessage(string message, CancellationToken cancellationToken)
    {
        if (this.config.AdminChatId > 0)
        {
            await botClient.SendTextMessageAsync(
                    chatId: this.config.AdminChatId,
                    text: message,
                    cancellationToken: cancellationToken);
        }
    }

    private IEnumerable<ITelegramCommand> GetCommands(Message message)
    {
        var context = message.GetContext();
        var lastPromptMessage = context.LastPromptMessage;
        if (!string.IsNullOrEmpty(lastPromptMessage?.Text) && !string.IsNullOrEmpty(message.Text))
        {
            message.Text = $"{lastPromptMessage.Text} {message.Text}";
            context.IsPromptReplyMessage = true;
            context.LastPromptMessage = null;
        }

        return this.GetCommands().Where(c => c?.IsEnabled == true && c.CanExecuteCommand(message));
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        this.logger.LogWarning(exception, "Telegram API Error");
        return Task.CompletedTask;
    }

    private IReadOnlyCollection<ITelegramCommand> GetCommandInstances() =>
        TelegramCommandRegistrationExtensions.CommandTypes.Select(t =>
        {
            ;
            var instance = this.serviceProvider.GetService(t);
            typeof(TelegramCommand).GetProperty(nameof(TelegramCommand.BotClient))?.SetValue(instance, this.botClient);
            typeof(TelegramCommand).GetProperty(nameof(TelegramCommand.LocalizationResolver))?.SetValue(instance, this.localizationResolver);
            return instance as TelegramCommand;

        })
        .Where(i => i != null)
        .ToList()!;
}
