using StackExchange.Redis;
using TeleBotService.Config;

namespace TeleBotService.Data.Redis;

public class UserSettingsRedisRepository : IUserSettingsRepository
{
    private readonly LazyRedis redis;
    private readonly ILogger<UserSettingsRedisRepository> logger;

    public UserSettingsRedisRepository(LazyRedis redis, ILogger<UserSettingsRedisRepository> logger)
    {
        this.redis = redis;
        this.logger = logger;
    }

    public async ValueTask<bool> SaveUserSettings(UserData user, UserSettings settings)
    {
        if (!this.redis.IsRedisConfigured)
        {
            this.logger.LogWarning("Can't save user settings. Redis host not set.");
            return false;
        }

        try
        {
            var entries = settings.Select(s => new HashEntry(s.Key, s.Value)).ToArray();
            if (entries.Length > 0)
            {
                var database = await redis.GetDatabaseAsync();
                await database.HashSetAsync(GetHashKey(user), entries);
            }
        }
        catch (Exception e)
        {
            this.logger.LogWarning(e, "An error ocurred while saving user settings");
        }

        return true;
    }

    public async ValueTask<UserSettings> GetUserSettings(UserData user)
    {
        if (!this.redis.IsRedisConfigured)
        {
            this.logger.LogWarning("Can't load user settings. Redis host not set.");
            return UserSettings.Empty;
        }

        try
        {
            var database = await redis.GetDatabaseAsync();
            var values = (await database.HashGetAllAsync(GetHashKey(user))).Select(h => new KeyValuePair<string, string?>(h.Name!, h.Value));
            return new(values);
        }
        catch (Exception e)
        {
            this.logger.LogWarning(e, "An error ocurred while loading user settings");
        }

        return UserSettings.Empty;
    }

    private static string GetHashKey(UserData user) => $"telebot:user:{user.UserName}:settings";
}
