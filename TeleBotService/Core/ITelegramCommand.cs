using TeleBotService.Localization;
using Telegram.Bot.Types;

namespace TeleBotService.Core;

public interface ITelegramCommand : ICommand
{
    Task Execute(Message message, CancellationToken cancellationToken = default);
    bool CanExecuteCommand(Message message);
}
