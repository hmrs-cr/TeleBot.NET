using TeleBotService.Config;

namespace TeleBotService.Data;

public interface IUserSettingsRepository
{
    ValueTask<bool> SaveUserSettings(UserData user, UserSettings settings);
    ValueTask<UserSettings> GetUserSettings(UserData user);
}
