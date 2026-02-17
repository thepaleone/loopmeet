namespace LoopMeet.Api.Services.Cache;

public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory);
}
