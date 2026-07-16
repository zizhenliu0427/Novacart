using Microsoft.Extensions.Options;
using Novacart.Api.Data;

namespace Novacart.Api.Infrastructure.Sharding;

public interface IShardedOrderDb
{
    bool Enabled { get; }

    Task ExecuteForUserAsync(
        Guid userId,
        Func<AppDbContext, Task> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteForUserAsync<T>(
        Guid userId,
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task ExecuteForOrderIdAsync(
        Guid orderId,
        Func<AppDbContext, Task> action,
        CancellationToken cancellationToken = default);

    Task<T?> ExecuteForOrderIdAsync<T>(
        Guid orderId,
        Func<AppDbContext, Task<T?>> action,
        CancellationToken cancellationToken = default);

    Task ExecuteAllShardContextsAsync(
        Func<AppDbContext, Task> action,
        bool includeLegacyDefault,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> QueryAllShardContextsAsync<T>(
        Func<AppDbContext, Task<T>> action,
        bool includeLegacyDefault,
        CancellationToken cancellationToken = default);

    Task RegisterRouteAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default);

    Task<T> ExecuteDefaultAsync<T>(
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default);
}

/// <summary>Routes order reads/writes to commerce shards; disabled = pass-through on scoped <see cref="AppDbContext"/>.</summary>
public sealed class ShardedOrderDb : IShardedOrderDb
{
    private readonly AppDbContext _defaultDb;
    private readonly IOrderDbContextFactory _factory;
    private readonly IOrderShardResolver _resolver;
    private readonly IOrderShardRouteStore _routes;

    public ShardedOrderDb(
        AppDbContext defaultDb,
        IOrderDbContextFactory factory,
        IOrderShardResolver resolver,
        IOrderShardRouteStore routes,
        IOptions<OrderShardingOptions> options)
    {
        _defaultDb = defaultDb;
        _factory = factory;
        _resolver = resolver;
        _routes = routes;
        _ = options.Value;
    }

    public bool Enabled => _resolver.Enabled;

    public Task ExecuteForUserAsync(
        Guid userId,
        Func<AppDbContext, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled)
            return action(_defaultDb);

        return ExecuteOnShardAsync(_resolver.GetShardIndex(userId), action, cancellationToken);
    }

    public Task<T> ExecuteForUserAsync<T>(
        Guid userId,
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled)
            return action(_defaultDb);

        return ExecuteOnShardAsync(_resolver.GetShardIndex(userId), action, cancellationToken);
    }

    public async Task ExecuteForOrderIdAsync(
        Guid orderId,
        Func<AppDbContext, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled)
        {
            await action(_defaultDb);
            return;
        }

        var route = await _routes.FindAsync(orderId, cancellationToken);
        if (route is not null)
            await ExecuteOnShardAsync(route.ShardIndex, action, cancellationToken);
        else
            await action(_defaultDb);
    }

    public async Task<T?> ExecuteForOrderIdAsync<T>(
        Guid orderId,
        Func<AppDbContext, Task<T?>> action,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled)
            return await action(_defaultDb);

        var route = await _routes.FindAsync(orderId, cancellationToken);
        if (route is not null)
            return await ExecuteOnShardAsync(route.ShardIndex, action, cancellationToken);

        return await action(_defaultDb);
    }

    public async Task ExecuteAllShardContextsAsync(
        Func<AppDbContext, Task> action,
        bool includeLegacyDefault,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled)
        {
            await action(_defaultDb);
            return;
        }

        for (var shard = 0; shard < _resolver.ShardCount; shard++)
            await ExecuteOnShardAsync(shard, action, cancellationToken);

        if (includeLegacyDefault)
            await action(_defaultDb);
    }

    public async Task<IReadOnlyList<T>> QueryAllShardContextsAsync<T>(
        Func<AppDbContext, Task<T>> action,
        bool includeLegacyDefault,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled)
            return [await action(_defaultDb)];

        var results = new List<T>(_resolver.ShardCount + (includeLegacyDefault ? 1 : 0));
        for (var shard = 0; shard < _resolver.ShardCount; shard++)
            results.Add(await ExecuteOnShardAsync(shard, action, cancellationToken));

        if (includeLegacyDefault)
            results.Add(await action(_defaultDb));

        return results;
    }

    public Task RegisterRouteAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!Enabled)
            return Task.CompletedTask;

        return _routes.RegisterAsync(orderId, userId, _resolver.GetShardIndex(userId), cancellationToken);
    }

    public Task<T> ExecuteDefaultAsync<T>(
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        action(_defaultDb);

    private async Task ExecuteOnShardAsync(
        int shardIndex,
        Func<AppDbContext, Task> action,
        CancellationToken cancellationToken)
    {
        await using var ctx = _factory.CreateShardContext(shardIndex);
        await action(ctx);
    }

    private async Task<T> ExecuteOnShardAsync<T>(
        int shardIndex,
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using var ctx = _factory.CreateShardContext(shardIndex);
        return await action(ctx);
    }
}

/// <summary>Test/monolith helper — always uses a single in-memory or scoped context.</summary>
public sealed class SingleDbShardedOrderDb(AppDbContext db) : IShardedOrderDb
{
    public bool Enabled => false;

    public Task ExecuteForUserAsync(
        Guid userId,
        Func<AppDbContext, Task> action,
        CancellationToken cancellationToken = default) =>
        action(db);

    public Task<T> ExecuteForUserAsync<T>(
        Guid userId,
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        action(db);

    public Task ExecuteForOrderIdAsync(
        Guid orderId,
        Func<AppDbContext, Task> action,
        CancellationToken cancellationToken = default) =>
        action(db);

    public Task<T?> ExecuteForOrderIdAsync<T>(
        Guid orderId,
        Func<AppDbContext, Task<T?>> action,
        CancellationToken cancellationToken = default) =>
        action(db);

    public Task ExecuteAllShardContextsAsync(
        Func<AppDbContext, Task> action,
        bool includeLegacyDefault,
        CancellationToken cancellationToken = default) =>
        action(db);

    public async Task<IReadOnlyList<T>> QueryAllShardContextsAsync<T>(
        Func<AppDbContext, Task<T>> action,
        bool includeLegacyDefault,
        CancellationToken cancellationToken = default) =>
        [await action(db)];

    public Task RegisterRouteAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<T> ExecuteDefaultAsync<T>(
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        action(db);
}
