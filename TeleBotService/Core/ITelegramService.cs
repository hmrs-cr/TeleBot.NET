using Telegram.Bot.Types;

namespace TeleBotService.Core;

public interface ITelegramService : IHostedService
{
    IEnumerable<ITelegramCommand> GetCommands();
    Task<User> GetInfo();
    Task<string?> ExecuteCommand(string command, string userName, bool sentReply = false, CancellationToken cancellationToken = default);
}