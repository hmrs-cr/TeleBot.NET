using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace TeleBotService.Data.Redis;

public class LazyRedis : Lazy<Task<ConnectionMultiplexer>>
{
    public bool IsRedisConfigured { get; }

    public LazyRedis(IOptions<RedisConfig> config) : base(() => ConnectionMultiplexer.ConnectAsync(config.Value.Host!), isThreadSafe: true)
    {
        this.IsRedisConfigured = !string.IsNullOrEmpty(config.Value.Host);
    }

    public async ValueTask<IDatabase> GetDatabaseAsync()
    {
        var redis = this.Value.IsCompletedSuccessfully ? this.Value.Result : await this.Value.ConfigureAwait(false);
        return redis.GetDatabase();
    }

    public async ValueTask<IServer> GetServerAsync()
    {
        var redis = this.Value.IsCompletedSuccessfully ? this.Value.Result : await this.Value.ConfigureAwait(false);
        var servers =  redis.GetServers();
        return servers.First();
    }
}
