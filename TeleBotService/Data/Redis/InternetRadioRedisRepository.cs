using StackExchange.Redis;

namespace TeleBotService.Data.Redis;

public class InternetRadioRedisRepository : IInternetRadioRepository
{
    private static readonly DateTime startDateTimeCount = new(2024, 6, 8);

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
            var database = await redis.GetDatabaseAsync();
            await database.SortedSetAddAsync(GetHashKey(radioId), url.ToString(), -(DateTime.UtcNow - startDateTimeCount).TotalSeconds);
        }
        catch (Exception e)
        {
            this.logger.LogWarning(e, "An error ocurred while saving user settings");
        }
    }

    private static RedisKey GetHashKey(string radioId) => $"telebot:radio:{radioId}:urls";
}
