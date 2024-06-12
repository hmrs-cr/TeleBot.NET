
using System.Reflection;

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

    public void RemoveUser(string userName)
    {
        var path = Path.Combine(TelebotServiceApp.LocalConfigPath, $"Users__{userName}__{nameof(UserData.Enabled)}");
        _ = File.WriteAllTextAsync(path, bool.FalseString);
        this.Remove(userName);
    }

    public void AddNewUser(string userName, string lang = "en")
    {
        var user = new UserData
        {
            UserName = userName,
            Language = lang,
            Enabled = true,
        };


        this[userName] = user;
        SaveUserConfig(user);
    }

    private static void SaveUserConfig(UserData user)
    {
        foreach (var property in typeof(UserData).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                                 .Where(p => p.CanRead)
                                                 .Where(p => p.Name != nameof(UserData.UserName)))
        {
            var value = property.GetValue(user)?.ToString();
            if (value is { })
            {
                var path = Path.Combine(TelebotServiceApp.LocalConfigPath, $"Users__{user.UserName}__{property.Name}");
                _ = File.WriteAllTextAsync(path, value);
            }
        }
    }
}
