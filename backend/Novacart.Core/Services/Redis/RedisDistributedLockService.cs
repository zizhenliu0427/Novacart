using StackExchange.Redis;

namespace Novacart.Api.Services;

/// <summary>
/// Redis-based distributed lock (Redlock-style single-master; extend with multiple
/// independent Redis masters for full Redlock quorum in production).
/// </summary>
public interface IRedisDistributedLockService
{
    /// <summary>Acquire one lock, or null if not acquired within <paramref name="wait"/>.</summary>
    Task<IDistributedLockHandle?> TryAcquireAsync(
        string key,
        TimeSpan expiry,
        TimeSpan wait,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire locks for all keys in sorted order (deadlock-safe).
    /// Releases partial acquisitions and returns null if any key cannot be locked.
    /// </summary>
    Task<IReadOnlyList<IDistributedLockHandle>?> TryAcquireAllAsync(
        IEnumerable<string> keys,
        TimeSpan expiry,
        TimeSpan wait,
        CancellationToken cancellationToken = default);
}

public interface IDistributedLockHandle : IAsyncDisposable
{
    string Key { get; }
}

public sealed class RedisDistributedLockService(IConnectionMultiplexer mux) : IRedisDistributedLockService
{
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IDatabase _db = mux.GetDatabase();

    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        string key,
        TimeSpan expiry,
        TimeSpan wait,
        CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow + wait;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await _db.StringSetAsync(key, token, expiry, When.NotExists))
                return new RedisLockHandle(_db, key, token);

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        return null;
    }

    public async Task<IReadOnlyList<IDistributedLockHandle>?> TryAcquireAllAsync(
        IEnumerable<string> keys,
        TimeSpan expiry,
        TimeSpan wait,
        CancellationToken cancellationToken = default)
    {
        var sortedKeys = keys.Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal).ToList();
        if (sortedKeys.Count == 0)
            return Array.Empty<IDistributedLockHandle>();

        var acquired = new List<IDistributedLockHandle>(sortedKeys.Count);

        foreach (var key in sortedKeys)
        {
            var handle = await TryAcquireAsync(key, expiry, wait, cancellationToken);
            if (handle is null)
            {
                foreach (var h in acquired)
                    await h.DisposeAsync();
                return null;
            }

            acquired.Add(handle);
        }

        return acquired;
    }

    private sealed class RedisLockHandle(IDatabase database, string key, string token) : IDistributedLockHandle
    {
        private int _released;

        public string Key { get; } = key;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
                return;

            try
            {
                await database.ScriptEvaluateAsync(
                    ReleaseScript,
                    new RedisKey[] { Key },
                    new RedisValue[] { token });
            }
            catch (RedisException)
            {
                // Lock expires via TTL if release fails (instance crash).
            }
        }
    }
}

public static class StockLockKeys
{
    public const string Prefix = "novacart:stock:lock:";

    public static string ForProduct(Guid productId) => $"{Prefix}{productId:N}";
}
