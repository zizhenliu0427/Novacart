using Novacart.Api.Services;

namespace Novacart.Api.Tests;

/// <summary>
/// In-memory no-op Redis cache for unit/integration tests — stores values in a
/// ConcurrentDictionary so Set/Get round-trips work without a real Redis instance.
/// Allows health-check pings and cache lookups to behave correctly under WebApplicationFactory.
/// </summary>
public class NullRedisCacheService : IRedisCacheService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, object?> _store = new();

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _store.TryGetValue(key, out var val);
        return Task.FromResult(val as T);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        foreach (var k in _store.Keys.Where(k => k.StartsWith(prefix)))
            _store.TryRemove(k, out _);
        return Task.CompletedTask;
    }
}
