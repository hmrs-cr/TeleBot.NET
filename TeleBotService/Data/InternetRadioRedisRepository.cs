
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace TeleBotService.Data;

public class InternetRadioRedisRepository : IInternetRadioRepository
{
    private readonly LazyRedis redis;
    private readonly ILogger<InternetRadioRedisRepository> logger;

    public InternetRadioRedisRepository(LazyRedis redis, ILogger<InternetRadioRedisRepository> logger)
    {
        this.redis = redis;
        this.logger = logger;
    }

    public async ValueTask SaveDiscoveredUrl(string radioId, Uri? url)
    {
         if (!this.redis.IsRedisConfigured)
        {
            this.logger.LogWarning("Can't save radio URL. Redis host not set.");
            return;
        }

        if (url == null)
        {
            return;
        }

        try
        {
            var entries = new[] { new HashEntry(url.ToString(), DateTime.UtcNow.ToString("s")) };
            var database = await redis.GetDatabaseAsync();
            await database.HashSetAsync(GetHashKey(radioId), entries);
        }
        catch (Exception e)
        {
            this.logger.LogWarning(e, "An error ocurred while saving user settings");
        }
    }

    private RedisKey GetHashKey(string radioId) => $"telebot:radio:{radioId}:discovered-urls";
}
