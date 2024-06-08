
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace TeleBotService.Data;

public class InternetRadioRedisRepository : IInternetRadioRepository
{
    private readonly RedisConfig config;
    private readonly ILogger<InternetRadioRedisRepository> logger;

    public InternetRadioRedisRepository(IOptions<RedisConfig> config, ILogger<InternetRadioRedisRepository> logger)
    {
        this.config = config.Value;
        this.logger = logger;
    }

    public void SaveDiscoveredUrl(string radioId, Uri? url)
    {
        if (string.IsNullOrEmpty(this.config.Host))
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
            using var redis = ConnectionMultiplexer.Connect(this.config.Host);
            var database = redis.GetDatabase();
            database.HashSet(GetHashKey(radioId), entries);
        }
        catch (Exception e)
        {
            this.logger.LogWarning(e, "An error ocurred while saving user settings");
        }
    }

    private RedisKey GetHashKey(string radioId) => $"telebot:radio:{radioId}:discovered-urls";
}
