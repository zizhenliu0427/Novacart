using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Novacart.Api.Data;
using Novacart.Api.Infrastructure.Sharding;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Tests;

/// <summary>Builds an enabled in-memory sharded order DB harness for integration tests.</summary>
public static class TestShardingFactory
{
    public sealed class Harness : IAsyncDisposable
    {
        public AppDbContext RoutingDb { get; }
        public IShardedOrderDb ShardedDb { get; }
        public IOrderShardResolver Resolver { get; }
        public IReadOnlyList<string> ShardDatabaseNames { get; }

        internal Harness(
            AppDbContext routingDb,
            IShardedOrderDb shardedDb,
            IOrderShardResolver resolver,
            IReadOnlyList<string> shardDatabaseNames)
        {
            RoutingDb = routingDb;
            ShardedDb = shardedDb;
            Resolver = resolver;
            ShardDatabaseNames = shardDatabaseNames;
        }

        public ValueTask DisposeAsync() => RoutingDb.DisposeAsync();
    }

    public static Harness CreateEnabled(int shardCount = 2)
    {
        var id = Guid.NewGuid().ToString("N");
        var routing = TestDbFactory.Create($"routing-{id}");
        var shardNames = Enumerable.Range(0, shardCount)
            .Select(i => $"shard{i}-{id}")
            .ToList();

        foreach (var name in shardNames)
            TestDbFactory.Create(name);

        var options = Microsoft.Extensions.Options.Options.Create(new OrderShardingOptions
        {
            Enabled = true,
            ShardCount = shardCount,
        });

        var resolver = new OrderShardResolver(options);
        var factory = new InMemoryOrderDbContextFactory(shardNames);
        var routes = new OrderShardRouteStore(routing, options);
        var sharded = new ShardedOrderDb(routing, factory, resolver, routes, options);

        return new Harness(routing, sharded, resolver, shardNames);
    }

    public static async Task<(Guid userId, Order order)> AddPaidOrderAsync(
        IShardedOrderDb shardedDb,
        IOrderShardResolver resolver,
        IReadOnlyList<string> shardNames,
        int shardIndex,
        decimal total,
        string orderNumber)
    {
        var userId = FindUserIdForShard(resolver, shardIndex);
        Order? created = null;

        await shardedDb.ExecuteForUserAsync(userId, async db =>
        {
            var product = await db.Products.FirstAsync();
            created = new Order
            {
                UserId = userId,
                OrderNumber = orderNumber,
                Subtotal = total,
                Total = total,
                CurrentStatus = OrderStatuses.Paid,
                CustomerEmail = $"{orderNumber}@example.com",
                Items =
                [
                    new OrderItem
                    {
                        ProductId = product.Id,
                        ProductNameSnapshot = product.Name,
                        PriceAtPurchase = total,
                        Quantity = 1,
                    },
                ],
            };

            db.Orders.Add(created);
            await db.SaveChangesAsync();
        });

        await shardedDb.RegisterRouteAsync(created!.Id, userId);
        return (userId, created);
    }

    public static Guid FindUserIdForShard(IOrderShardResolver resolver, int targetShard)
    {
        for (var i = 0; i < 10_000; i++)
        {
            var candidate = Guid.NewGuid();
            if (resolver.GetShardIndex(candidate) == targetShard)
                return candidate;
        }

        throw new InvalidOperationException($"Could not find userId for shard {targetShard}.");
    }
}
