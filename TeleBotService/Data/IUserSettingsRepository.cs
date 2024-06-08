using TeleBotService.Config;

namespace TeleBotService.Data;

public interface IUserSettingsRepository
{
    bool SaveUserSettings(UserData user, UserSettings settings);
    UserSettings GetUserSettings(UserData user);
}
