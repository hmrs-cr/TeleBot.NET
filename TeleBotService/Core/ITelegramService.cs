using Telegram.Bot.Types;

namespace TeleBotService.Core;

public interface ITelegramService : IHostedService
{
    IEnumerable<ITelegramCommand> GetCommands();
    Task<User> GetInfo();
}