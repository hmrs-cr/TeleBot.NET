namespace TeleBotService.Config;

public class UsersConfig : Dictionary<string, UserData>
{
    public const string UsersConfigName = "Users";

    public UserData? GetUser(string? userName)
    {
        var user = this.GetValueOrDefault(userName ?? string.Empty);
        if (user != null)
        {
            user.UserName = userName;
        }

        return user;
    }
}
