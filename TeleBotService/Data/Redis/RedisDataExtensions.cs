using System.Reflection;
using StackExchange.Redis;

namespace TeleBotService.Data.Redis;

public static class RedisDataExtensions
{
    // .Select(s => new HashEntry(s.Key, s.Value)).ToArray();
    public static HashEntry[] ToRedisHashSet<T>(this T obj, Func<PropertyInfo, bool>? propertyFilter = null) =>
        typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                 .Where(p => p.CanRead)
                 .Where(p => propertyFilter == null || propertyFilter.Invoke(p))
                 .Select(p => new HashEntry(p.Name, p.GetValue(obj)?.ToString()))
                 .ToArray();
}
