using TeleBotService.Core.Model;
using Telegram.Bot.Types;

namespace TeleBotService.Core;

public interface ITelegramCommand : ICommand
{
    Task<bool> HandleCommand(MessageContext message, CancellationToken cancellationToken = default);
    bool CanExecuteCommand(Message message);
}
