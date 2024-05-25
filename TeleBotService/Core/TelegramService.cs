using Microsoft.Extensions.Options;
using TeleBotService.Config;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBotService.Core;

public class TelegramService : ITelegramService
{
    private readonly TelegramBotClient botClient;
    private readonly TelegramConfig config;
    private readonly CancellationTokenSource cts = new ();
    private readonly ReceiverOptions receiverOptions = new ()
    {
        AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
    };
    
    private IReadOnlyCollection<ITelegramCommand> commandInstances;

    private readonly IServiceProvider serviceProvider;

    public TelegramService(IOptions<TelegramConfig> confif, IServiceProvider serviceProvider) 
    {
        if (string.IsNullOrEmpty(confif.Value.BotToken) || confif.Value.BotToken.Length < 10)
        {
            throw new ApplicationException("No Telegram BotToken configured.");
        }

        this.botClient = new TelegramBotClient(confif.Value.BotToken);
        this.config = confif.Value;
        this.serviceProvider = serviceProvider;      
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
            _ = this.SendAdminMessage($"Service started: {InternalInfoCommand.GetInternalInfoString(await this.botClient.GetMeAsync())}", cancellationToken);   
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
       if (!TelebotServiceApp.IsDev)
       {
            await this.SendAdminMessage($"Service stopped: {InternalInfoCommand.GetInternalInfoString(await this.botClient.GetMeAsync())}", cancellationToken);
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

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        if (!this.config.AllowedUsers.Contains(message.Chat.Username))
        {
            Console.WriteLine($"Forbidden {message.Chat.Username}:{message.Chat.Id}");
            _ = botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Y usted quien es?",
                            replyToMessageId: message.MessageId,
                            cancellationToken: cancellationToken);
            return;
        }

        var chatId = message.Chat.Id;
        Console.WriteLine($"Received '{messageText}' message in chat {chatId}.");

        var executedCommandCount = 0;
        var commands = this.GetCommands(message);
        foreach (var command in commands) 
        {
            await command.Execute(message, cts.Token);
            Console.WriteLine($"Executed '{command.GetType().Name}' commnad in chat {chatId}.");
            executedCommandCount++;
        }

        if (executedCommandCount > 0)
        {
            Console.WriteLine($"Executed {executedCommandCount} commands: '{messageText}', chat {chatId}.");
        }
        else 
        {
            Console.WriteLine($"No commands found for message '{messageText}', chat {chatId}.");
            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "No entendí",
                                replyToMessageId: message.MessageId,
                                cancellationToken: cancellationToken);
        }
    }

    private async Task SendAdminMessage(string message, CancellationToken cancellationToken)
    {
        if (this.config.AdminChatId > 0) 
        {
            await botClient.SendTextMessageAsync(
                    chatId: this.config.AdminChatId,
                    text: message,
                    cancellationToken: cancellationToken);
        }
    }

    private IEnumerable<ITelegramCommand> GetCommands(Message message) => this.GetCommands().Where(c => c?.IsEnabled == true && c.CanExecuteCommand(message))!;

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    internal IReadOnlyCollection<ITelegramCommand> GetCommandInstances() =>
        TelegramCommandRegistrationExtensions.CommandTypes.Select(t => 
        {
            ;
            var instance = this.serviceProvider.GetService(t);
            typeof(TelegramCommand).GetProperty(nameof(TelegramCommand.BotClient))?.SetValue(instance, this.botClient);
            return instance as TelegramCommand;

        })
        .Where(i => i != null)
        .ToList()!;
}
