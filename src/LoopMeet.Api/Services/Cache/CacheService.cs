using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace LoopMeet.Api.Services.Cache;

public sealed class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;

    public CacheService(IMemoryCache memoryCache, IDistributedCache? distributedCache = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        if (_distributedCache is not null)
        {
            var cached = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                return JsonSerializer.Deserialize<T>(cached);
            }

            var value = await factory();
            await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(value), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });
            return value;
        }

        if (_memoryCache.TryGetValue(key, out T? existing))
        {
            return existing;
        }

        var created = await factory();
        _memoryCache.Set(key, created, ttl);
        return created;
    }

    public async Task RemoveAsync(string key)
    {
        if (_distributedCache is not null)
        {
            await _distributedCache.RemoveAsync(key);
        }

        _memoryCache.Remove(key);
    }
}
