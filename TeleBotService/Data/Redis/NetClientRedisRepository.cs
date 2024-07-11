using Omada.OpenApi.Client.Responses;

namespace TeleBotService.Data.Redis;

public class NetClientRedisRepository : INetClientRepository
{
    private const string KeyBase = "telebot:netclients:disconnected:";

    private readonly LazyRedis redis;

    public NetClientRedisRepository(LazyRedis redis)
    {
        this.redis = redis;
    }

    public async IAsyncEnumerable<BasicClientData> GetDisconnectedNetClientsInfo()
    {
        if (this.redis.IsRedisConfigured)
        {
            var server = await redis.GetServerAsync();
            var database = await redis.GetDatabaseAsync();
            await foreach (var key in server.KeysAsync(pattern: KeyBase + "*"))
            {
                var hashSet = await database.HashGetAllAsync(key);

                yield return new BasicClientData
                {
                    Mac = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.Mac)).Value!,
                    Name = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.Name)).Value!,
                    Active = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.Active)).Value == bool.TrueString,
                    ApName = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.ApName)).Value,
                    GatewayName = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.GatewayName)).Value,
                    LastSeen = (long)hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.LastSeen)).Value,
                    NetworkName = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.NetworkName)).Value,
                    SignalRank = (int)hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.SignalRank)).Value,
                    Ssid = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.Ssid)).Value,
                    SwitchName = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.SwitchName)).Value,
                    Wireless = hashSet.FirstOrDefault(v => v.Name == nameof(BasicClientData.Wireless)).Value == bool.TrueString,
                };
            }
        }
    }

    public Task RemoveDisconnectedNetClientInfo(BasicClientData clientData)
    {
        /*if (!this.redis.IsRedisConfigured)
        {
            return;
        }

        var database = await redis.GetDatabaseAsync();
        await database.KeyDeleteAsync(KeyBase + clientData.Mac);*/
        return Task.CompletedTask;
    }

    public async Task SaveDisconnectedNetClientInfo(BasicClientData clientData)
    {
        if (!this.redis.IsRedisConfigured)
        {
            return;
        }

        var database = await redis.GetDatabaseAsync();
        var hashSet = clientData.ToRedisHashSet(pi => pi.CanWrite && pi.Name != nameof(BasicClientData.Active) && pi.Name != nameof(BasicClientData.Index));
        try
        {
            await database.HashSetAsync(KeyBase + clientData.Mac, hashSet);
        }
        catch (Exception e)
        {
            _ = e;
        }

    }
}
