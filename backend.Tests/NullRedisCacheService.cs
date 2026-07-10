using Novacart.Api.Services;

namespace Novacart.Api.Tests;

/// <summary>
/// No-op Redis cache for unit tests — never caches, never errors.
/// Allows services to be tested without a real Redis instance.
/// </summary>
public class NullRedisCacheService : IRedisCacheService
{
    public Task<T?> GetAsync<T>(string key) where T : class => Task.FromResult<T?>(null);
    public Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class => Task.CompletedTask;
    public Task RemoveAsync(string key) => Task.CompletedTask;
    public Task RemoveByPrefixAsync(string prefix) => Task.CompletedTask;
}
