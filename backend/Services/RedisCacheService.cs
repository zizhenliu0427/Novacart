using System.Text.Json;
using StackExchange.Redis;

namespace Novacart.Api.Services;

public interface IRedisCacheService
{
    /// <summary>Get a cached value, or null if not found / expired.</summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>Set a value with the specified TTL.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class;

    /// <summary>Remove a single key.</summary>
    Task RemoveAsync(string key);

    /// <summary>Remove all keys matching a prefix (e.g. "products:list:*").</summary>
    Task RemoveByPrefixAsync(string prefix);
}

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _db;
    private readonly IConnectionMultiplexer _mux;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public RedisCacheService(IConnectionMultiplexer mux)
    {
        _mux = mux;
        _db = mux.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        // Use SCAN to find matching keys — safe for production (non-blocking).
        var endpoints = _mux.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _mux.GetServer(endpoint);
            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }
}
