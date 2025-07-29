using StackExchange.Redis;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;

namespace TeleBotService.Data.Redis;

public class InternetRadioRedisRepository : IInternetRadioRepository
{
    private readonly LazyRedis redis;
    private readonly ILogger<InternetRadioRedisRepository> logger;

    public InternetRadioRedisRepository(LazyRedis redis, ILogger<InternetRadioRedisRepository> logger)
    {
        this.redis = redis;
        this.logger = logger;
    }

    public async Task<RadioDiscoverResponse.ResultData.Stream?> GetStreamData(string radioId)
    {
        if (!this.redis.IsRedisConfigured)
        {
            this.logger.LogWarning("Can't get radio stream data. Redis host not set.");
            return null;
        }
        
        try
        {
            var database = await redis.GetDatabaseAsync();
            var streamData = await database.StringGetAsync(GetHashKey(radioId));
            if (streamData.HasValue)
            {
                return RadioDiscoverResponse.ResultData.Stream.FromJson(streamData!);
            }
        }
        catch (Exception e)
        {
            this.logger.LogSimpleException("An error occurred while getting radio stream data", e);
        }

        return null;
    }

    public async Task<RadioDiscoverResponse.ResultData.Stream?> SaveStreamData(string radioId, RadioDiscoverResponse.ResultData.Stream? streamData)
    {
        if (!this.redis.IsRedisConfigured)
        {
            this.logger.LogWarning("Can't save radio stream data. Redis host not set.");
            return streamData;
        }
        
        try
        {
            if (streamData is not null)
            {
                var database = await redis.GetDatabaseAsync();
                await database.StringSetAsync(GetHashKey(radioId), streamData.ToString());
            }
        }
        catch (Exception e)
        {
            this.logger.LogSimpleException("An error occurred while getting radio stream data", e);
        }
        
        return streamData;
    }

    public async Task<Dictionary<string, RadioDiscoverResponse.ResultData.Stream>?> ListStreamData()
    {
        if (!this.redis.IsRedisConfigured)
        {
            this.logger.LogWarning("Can't save radio stream data. Redis host not set.");
            return null;
        }

        var server = await redis.GetServerAsync();
        var database = await redis.GetDatabaseAsync();
        var result = new Dictionary<string, RadioDiscoverResponse.ResultData.Stream>();
        await foreach (var key in server.KeysAsync(pattern: "telebot:radio:*"))
        {
            var streamData = await database.StringGetAsync(key);
            if (streamData.HasValue)
            {
                result[key.ToString().GetLastPart(':')] = RadioDiscoverResponse.ResultData.Stream.FromJson(streamData!);
            }
        }

        return result;
    }

    private static RedisKey GetHashKey(string radioId) => $"telebot:radio:{radioId}";
}
