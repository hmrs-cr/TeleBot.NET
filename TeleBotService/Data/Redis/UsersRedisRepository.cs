using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TeleBotService.Config;
using TeleBotService.Extensions;

namespace TeleBotService.Data.Redis;

public class UsersRedisRepository : IUsersRepository
{
    private readonly UsersConfig users;
    private readonly LazyRedis redis;
    private readonly ILogger<UsersRedisRepository> logger;

    public UsersRedisRepository(
        IOptions<UsersConfig> users,
        LazyRedis redis,
        ILogger<UsersRedisRepository> logger)
    {
        this.users = users.Value;
        this.redis = redis;
        this.logger = logger;
    }

    public async Task<bool> AreSettingsAvailable()
    {
        try
        {
            await ProcessExtensions.Retry(PingDatabaseAsync, 15, 2000);
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Redis connection failure.");
            return false;
        }

        async ValueTask PingDatabaseAsync(CancellationToken cancellationToken)
        {
            var database = await redis.GetDatabaseAsync();
            database.Ping();
        }
    }

    public async IAsyncEnumerable<long> GetNetClientMonitorChatIds()
    {
        var database = await redis.GetDatabaseAsync();
        var keys = users.Select(u => GetHashKey(u.Key));
        foreach (var key in keys)
        {
            var value = await database.HashGetAsync(key, UserData.NetClientMonitorChatIdKeyName);
            if (value is { HasValue: true })
            {
                yield return (long)value;
            }
        }
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
            var entries = settings.Where(s => s.Value != UserData.DeleteSettingValue).Select(s => new HashEntry(s.Key, s.Value)).ToArray();
            var deletedEntries = settings.Where(s => s.Value == UserData.DeleteSettingValue).Select(s => new RedisValue(s.Key)).ToArray();
            if (entries.Length > 0 || deletedEntries.Length > 0)
            {
                var database = await redis.GetDatabaseAsync();
                var key = GetHashKey(user);
                if (entries.Length > 0)
                {
                    await database.HashSetAsync(key, entries);
                }

                if (deletedEntries.Length > 0)
                {
                    await database.HashDeleteAsync(key, deletedEntries);
                }
            }
        }
        catch (Exception e)
        {
            this.logger.LogSimpleException("An error occurred while saving user settings", e);
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
            this.logger.LogSimpleException("An error ocurred while loading user settings", e);
        }

        return UserSettings.Empty;
    }

    private static string GetHashKey(UserData user) => GetHashKey(user.UserName);

    private static string GetHashKey(string? userName) => $"telebot:user:{userName}:settings";
}
