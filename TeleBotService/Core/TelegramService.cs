using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Commands;
using TeleBotService.Core.Model;
using TeleBotService.Data;
using TeleBotService.Extensions;
using TeleBotService.Localization;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace TeleBotService.Core;

public class TelegramService : ITelegramService
{
    private readonly ITelegramBotClient botClient;
    private readonly TelegramConfig config;
    private readonly CancellationTokenSource cts = new();
    private readonly ReceiverOptions receiverOptions = new()
    {
        AllowedUpdates = [] // receive all update types except ChatMember related updates
    };

    private IReadOnlyCollection<ITelegramCommand>? commandInstances;
    private readonly ILocalizationResolver localizationResolver;
    private readonly IServiceProvider serviceProvider;
    private readonly IUsersRepository userSettingsRepository;
    private readonly UsersConfig users;
    private readonly ILogger<TelegramService> logger;

    private readonly Lazy<Task<User>> myInfo;

    public TelegramService(
        IOptions<TelegramConfig> confif,
        ILocalizationResolver localizationResolver,
        IServiceProvider serviceProvider,
        IOptions<UsersConfig> users,
        IUsersRepository userSettingsRepository,
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
        this.userSettingsRepository = userSettingsRepository;
        this.users = users.Value;
        this.logger = logger;
        this.myInfo = new(() => this.botClient.GetMeAsync());
    }

    public Task<User> GetInfo() => this.myInfo.Value;

    public IEnumerable<ITelegramCommand> GetCommands() => this.commandInstances ??= this.GetCommandInstances();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.StartNetClientMonitor();

        this.botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        if (!TelebotServiceApp.IsDev)
        {
            _ = this.SentAdminMessage($"Service started: {InternalInfoCommand.GetInternalInfoString(await this.myInfo.Value)}", cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.serviceProvider.GetService<INetClientMonitor>() is { } netClientMonitor)
        {
            netClientMonitor.StopNetClientMonitor();
        }

        if (!cts.IsCancellationRequested)
        {
            _ = this.SentAdminMessage($"Service stopped: {InternalInfoCommand.GetInternalInfoString(await this.myInfo.Value)}", default);
            cts.Cancel();
            cts.Dispose();
        }
    }

    private async Task StartNetClientMonitor()
    {
        try
        {
            if (this.serviceProvider.GetService<INetClientMonitor>() is { } netClientMonitor)
            {
                var started = false;
                var ids = this.userSettingsRepository.GetNetClientMonitorChatIds();
                await foreach (var id in ids)
                {
                    started |= netClientMonitor.StartNetClientMonitor(this.botClient, id);
                }

                if (!started)
                {
                    netClientMonitor.StartNetClientMonitor(this.botClient, -1);
                }
            }
        }
        catch (Exception e)
        {
            this.logger.LogSimpleException("Error StartingNetClientMonitor", e);
        }
    }

    public async Task<string?> ExecuteCommand(string command, string userName, bool sentReply = false, CancellationToken cancellationToken = default)
    {
        var user = this.users.GetUser(userName);
        if (user is null || !user.Enabled)
        {
            return "Unauthorized";
        }

        var chatId = sentReply ? user.GetLongSetting(UserData.ChatIdKeyName) : 0;
        var update = new FakeUpdateTelegramClient.FakeUpdate(command, userName, chatId);
        var tc = new FakeUpdateTelegramClient(this.botClient);
        await this.HandleUpdateAsync(tc, update, cancellationToken);
        return await tc.GetTextResponse();
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return Task.CompletedTask;

        // Only process text messages
        if (message.Text is not { } messageText)
            return Task.CompletedTask;

        var user = this.users.GetUser(message.Chat.Username);
        if (user is null || !user.Enabled)
        {
            this.HandleUnknownUser(botClient, message, cancellationToken);
            return Task.CompletedTask;
        }

        this.logger.LogDebug("Received '{messageText}' message in chat {messageChatId}.", messageText, message.Chat.Id);

        var messageContext = new MessageContext(botClient, message, user);
        _ = HandleCommands(messageContext, cancellationToken);

        return Task.CompletedTask;
    }

    private void HandleUnknownUser(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var messageText = message.Text ?? string.Empty;
        if (messageText.EndsWith("start") && this.config.JoinBotServicesWatchword != null)
            {
                _ = botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: this.config.JoinBotServicesWatchword, cancellationToken: cancellationToken);
                this.logger.LogInformation("Unknown user {messageChatUsername}:{messageChatId} is starting bot", message.Chat.Username, message.Chat.Id);
                return;
            }
            else
            {
                if (!string.IsNullOrEmpty(message.Chat.Username) && messageText == this.config.JoinBotServicesPassword)
                {
                    this.AcceptNewUser(botClient, message, cancellationToken);
                    return;
                }

                _ = this.RejectNewUser(botClient, message, cancellationToken);
                this.logger.LogInformation("Forbidden {messageChatUsername}:{messageChatId}", message.Chat.Username, message.Chat.Id);
                return;
            }
    }

    private async Task RejectNewUser(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await Task.Delay(2500);
        await this.ReplyPhotoUrl(botClient, message, "👎", this.config.FailedJoinPasswordImageUrl, cancellationToken);
    }

    private void AcceptNewUser(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        this.users.AddNewUser(message.Chat.Username!, message.GetContext().LanguageCode ?? "en");
        this.logger.LogInformation("New user '{userName}' added", message.Chat.Username);

        _ = this.SentAdminMessage($"'{message.Chat.Username}' joined the bot services.", cancellationToken);
        _ = this.ReplyPhotoUrl(botClient, message, "Welcome! Write /Help to see what you can do.", this.config.WelcomeImageUrl, cancellationToken);
    }

    private Task ReplyPhotoUrl(ITelegramBotClient botClient, Message message, string messageText, Uri? imageUrl, CancellationToken cancellationToken)
    {
        if (imageUrl != null)
        {
            var image = InputFile.FromUri(imageUrl);
            messageText = this.localizationResolver.GetLocalizedString(message.GetContext().LanguageCode, messageText);
            return botClient.SendPhotoAsync(message.Chat.Id, image, replyToMessageId: message.MessageId, caption: messageText, cancellationToken: cancellationToken);
        }

        return this.Reply(botClient, message, messageText, cancellationToken);
    }

    private async Task HandleCommands(MessageContext messageContext, CancellationToken cancellationToken)
    {
        var executedCommandCount = 0;
        var failedCommandCount = 0;
        var refusedCommandCount = 0;
        var executed = false;
        var commands = this.GetCommands(messageContext);

        await this.LoadUserSettingsIfNeeded(messageContext);
        foreach (var command in commands)
        {
            try
            {
                executed = await command.HandleCommand(messageContext, cancellationToken);
                if (executed)
                {
                    this.logger.LogInformation("Executed '{commandName}' command in chat {messageChatId}.", command.Name, messageContext.Message.Chat.Id);
                    executedCommandCount++;
                }
                else
                {
                    this.logger.LogInformation("Refused execution of '{commandName}' command in chat {messageChatId}.", command.Name, messageContext.Message.Chat.Id);
                    refusedCommandCount++;
                }
            }
            catch (Exception e)
            {
                failedCommandCount++;
                this.logger.LogWarning("Unhandled error while executing command {command.Name}: [{ExceptionType}] {ExceptionMessage}", command.Name, e.GetType().FullName, e.Message);
            }
        }

        if (executedCommandCount > 0)
        {
            this.SaveUserSettingsIfNeeded(messageContext);
            this.logger.LogDebug("Executed {executedCommandCount} commands: '{messageText}', chat {messageChatId}.", executedCommandCount, messageContext.Message.Text, messageContext.Message.Chat.Id);
        }
        else if (failedCommandCount == 0 && refusedCommandCount == 0)
        {
            this.logger.LogInformation("No commands found for message '{messageText}', chat {messageChatId}.", messageContext.Message.Text, messageContext.Message.Chat.Id);
            await this.Reply(messageContext, "I didn't understand you", cancellationToken);
        }
    }

    private void SaveUserSettingsIfNeeded(MessageContext messageContext) =>
        _ = messageContext.User.SaveSettings(this.userSettingsRepository.SaveUserSettings);

    private async ValueTask LoadUserSettingsIfNeeded(MessageContext messageContext)
    {
        await messageContext.User.LoadSettings(this.userSettingsRepository.GetUserSettings);
        messageContext.Context.LanguageCode = messageContext.User.GeStringSetting(nameof(UserData.Language), messageContext.User.Language) ?? this.config.DefaultLanguageCode ?? messageContext.Context.LanguageCode;
    }

    private Task Reply(MessageContext context, string text, CancellationToken cancellationToken) =>
        context.BotClient.Reply(context.Message, this.localizationResolver.GetLocalizedString(context.Message.GetContext().LanguageCode, text), cancellationToken);

    private Task Reply(ITelegramBotClient botClient, Message message, string text, CancellationToken cancellationToken) =>
        botClient.Reply(message, this.localizationResolver.GetLocalizedString(message.GetContext().LanguageCode, text), cancellationToken);

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

    private IEnumerable<ITelegramCommand> GetCommands(MessageContext messageContext)
    {
        var message = messageContext.Message;
        var context = messageContext.Context;
        var lastPromptMessage = context.LastPromptMessage;
        if (!string.IsNullOrEmpty(lastPromptMessage?.Text) && !string.IsNullOrEmpty(message.Text))
        {
            message.Text = $"{lastPromptMessage.Text} {message.Text}";
            context.IsPromptReplyMessage = true;
            context.LastPromptMessage = null;
        }

        return this.GetCommands().Where(c => c?.IsEnabled == true && c.CanExecuteCommand(message));
    }

    private async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        this.logger.LogSimpleException("Telegram API Error", exception);
        await Task.Delay(5000);
    }

    private List<TelegramCommand> GetCommandInstances() =>
        TelegramCommandRegistrationExtensions.CommandTypes.Select(t =>
        {
            var command = this.serviceProvider.GetService(t) as TelegramCommand;
            return command?.Init(this.localizationResolver);

        })
        .Where(i => i != null)
        .ToList()!;

    private class FakeUpdateTelegramClient : ITelegramBotClient
    {
        private TaskCompletionSource<string> responseTaskCompletion = new();

        private readonly ITelegramBotClient botClient;

        public bool LocalBotServer => this.botClient.LocalBotServer;

        public long? BotId => this.botClient.BotId;

        public TimeSpan Timeout { get; set; }

        public IExceptionParser ExceptionsParser { get; set; }

        public FakeUpdateTelegramClient(ITelegramBotClient botClient)
        {
            this.botClient = botClient;
            this.ExceptionsParser = botClient.ExceptionsParser;
            this.Timeout = botClient.Timeout;
        }

        public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;

        public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;

        private void SetTextResponse(string textResponse)
        {
            if (!this.responseTaskCompletion.Task.IsCompleted)
            {
                this.responseTaskCompletion.SetResult(textResponse);
            }
        }

        public Task<string> GetTextResponse() => this.GetTextResponse(TimeSpan.FromMinutes(2.5));

        public async Task<string> GetTextResponse(TimeSpan  timeout)
        {
            var resultTask = this.responseTaskCompletion.Task;
            await Task.WhenAny(resultTask, Task.Delay(timeout));
            return resultTask.IsCompleted ? resultTask.Result : "Timeout";
        }

        public Task DownloadFileAsync(string filePath, Stream destination, CancellationToken cancellationToken = default) =>
            this.botClient.DownloadFileAsync(filePath, destination, cancellationToken);


        public async Task<TResponse> MakeRequestAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var textResponse = string.Empty;
            if (request is SendMessageRequest messageRequest && messageRequest.Text is { })
            {
                textResponse = messageRequest.Text;
            }
            else if (request is SendPhotoRequest photoRequest && photoRequest.Caption is { })
            {
                textResponse = photoRequest.Caption;
            }
            else if (request is SendAudioRequest audioRequest && audioRequest.Caption is { })
            {
                textResponse = audioRequest.Caption;
            }

            this.SetTextResponse(textResponse);

            var result = default(TResponse);
            if (request is IChatTargetable { ChatId.Identifier: > 0 })
            {
                result = await this.botClient.MakeRequestAsync(request, cancellationToken);
            }

            return result;
        }

        public Task<bool> TestApiAsync(CancellationToken cancellationToken = default) => this.botClient.TestApiAsync(cancellationToken);

        public class FakeUpdate : Update
        {
            public FakeUpdate(string command, string userName, long chatId)
            {
                this.Message = new()
                {
                    Text = command,
                    Chat = new()
                    {
                        Username = userName,
                        Id = chatId,
                    }
                };
            }
        }
    }
}