using TeleBotService.Config;

namespace TeleBotService.Data;

public interface IUsersRepository
{
    Task<bool> AreSettingsAvailable();
    ValueTask<bool> SaveUserSettings(UserData user, UserSettings settings);
    ValueTask<UserSettings> GetUserSettings(UserData user);
    IAsyncEnumerable<long> GetNetClientMonitorChatIds();
}
