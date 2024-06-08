using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TeleBotService.Config;

namespace TeleBotService.Data;

public class UserSettingsRedisRepository : IUserSettingsRepository
{
    private readonly RedisConfig config;
    private readonly ILogger<UserSettingsRedisRepository> logger;

    public UserSettingsRedisRepository(IOptions<RedisConfig> config, ILogger<UserSettingsRedisRepository> logger)
    {
        this.config = config.Value;
        this.logger = logger;
    }

    public bool SaveUserSettings(UserData user, UserSettings settings)
    {
        if (string.IsNullOrEmpty(this.config.Host))
        {
            this.logger.LogWarning("Can't save user settings. Redis host not set.");
            return false;
        }

        try
        {
            var entries = settings.Select(s => new HashEntry(s.Key, s.Value)).ToArray();
            if (entries.Length > 0)
            {
                using var redis = ConnectionMultiplexer.Connect(this.config.Host);
                var database = redis.GetDatabase();
                database.HashSet(GetHashKey(user), entries);
            }
        }
        catch (Exception e)
        {
            this.logger.LogWarning(e, "An error ocurred while saving user settings");
        }

        return true;
    }

    public UserSettings GetUserSettings(UserData user)
    {
        if (string.IsNullOrEmpty(this.config.Host))
        {
            this.logger.LogWarning("Can't load user settings. Redis host not set.");
            return UserSettings.Empty;
        }

         try
        {
            using var redis = ConnectionMultiplexer.Connect(this.config.Host);
            var database = redis.GetDatabase();
            var values = database.HashGetAll(GetHashKey(user)).Select(h => new KeyValuePair<string, string?>(h.Name!, h.Value));
            return new (values);
        }
        catch (Exception e)
        {
            this.logger.LogWarning(e, "An error ocurred while loading user settings");
        }

        return UserSettings.Empty;;
    }

    private static string GetHashKey(UserData user) => $"telebot:user:{user.UserName}:settings";
}
