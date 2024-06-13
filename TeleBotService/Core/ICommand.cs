namespace TeleBotService.Core;

public interface ICommand
{
    bool IsEnabled { get; }
    bool IsAdmin { get; }

    string Name { get; }
    string Description { get; }
    string Usage { get; }
}
