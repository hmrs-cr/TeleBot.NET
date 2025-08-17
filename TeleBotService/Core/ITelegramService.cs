using Telegram.Bot.Types;

namespace TeleBotService.Core;

public interface ITelegramService : IHostedService
{
    IEnumerable<ITelegramCommand> GetCommands();
    Task<User> GetInfo();
    Task<string?> ExecuteCommand(string command, string userName, bool sentReply = false, long? chatId = null, 
        int messageId = 0, CancellationToken cancellationToken = default);
}