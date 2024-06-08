using TeleBotService.Config;

namespace TeleBotService.Data;

public class UserSettingsRepository : IUserSettingsRepository
{
    public bool SaveUserSettings(UserData user, UserSettings settings)
    {
        return true;
    }

    public UserSettings GetUserSettings(UserData user)
    {
        return UserSettings.Empty;;
    }
}
