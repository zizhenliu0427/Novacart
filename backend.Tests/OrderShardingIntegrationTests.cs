using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Xunit;

namespace Novacart.Api.Tests;

/// <summary>PE-7: analytics and admin paths with sharding enabled (in-memory multi-DB).</summary>
public class OrderShardingIntegrationTests
{
    [Fact]
    public async Task AnalyticsService_AggregatesAcrossAllShards_WhenShardingEnabled()
    {
        await using var harness = TestShardingFactory.CreateEnabled(shardCount: 2);

        await TestShardingFactory.AddPaidOrderAsync(
            harness.ShardedDb, harness.Resolver, harness.ShardDatabaseNames, shardIndex: 0, total: 100m, "NC-S0-001");
        await TestShardingFactory.AddPaidOrderAsync(
            harness.ShardedDb, harness.Resolver, harness.ShardDatabaseNames, shardIndex: 1, total: 50m, "NC-S1-001");

        var analytics = new AnalyticsService(harness.RoutingDb, harness.ShardedDb);
        var summary = await analytics.GetSummaryAsync();

        summary.TotalOrders.Should().Be(2);
        summary.TotalRevenue.Should().Be(150m);
        summary.TotalUnitsSold.Should().Be(2);
    }

    [Fact]
    public async Task OrderShardBackfillService_CopiesLegacyOrdersAndRegistersRoutes()
    {
        await using var harness = TestShardingFactory.CreateEnabled(shardCount: 2);
        var userId = await TestDbFactory.SeedTestUserAsync(harness.RoutingDb);
        var product = await TestDbFactory.GetFirstProductAsync(harness.RoutingDb);

        var legacyOrder = new Order
        {
            UserId = userId,
            OrderNumber = "NC-LEGACY-01",
            Subtotal = 80m,
            Total = 88m,
            CurrentStatus = OrderStatuses.Paid,
            CustomerEmail = "legacy@example.com",
            Items =
            [
                new OrderItem
                {
                    ProductId = product.Id,
                    ProductNameSnapshot = product.Name,
                    PriceAtPurchase = 80m,
                    Quantity = 1,
                },
            ],
        };

        harness.RoutingDb.Orders.Add(legacyOrder);
        await harness.RoutingDb.SaveChangesAsync();

        var options = Microsoft.Extensions.Options.Options.Create(new Novacart.Api.Infrastructure.Sharding.OrderShardingOptions
        {
            Enabled = true,
            ShardCount = 2,
        });

        var factory = new InMemoryOrderDbContextFactory(harness.ShardDatabaseNames);
        var routes = new Novacart.Api.Infrastructure.Sharding.OrderShardRouteStore(harness.RoutingDb, options);
        var backfill = new Novacart.Api.Infrastructure.Sharding.OrderShardBackfillService(
            harness.RoutingDb,
            factory,
            routes,
            harness.Resolver,
            options);

        var dryRun = await backfill.RunAsync(dryRun: true);
        dryRun.MigratedOrPlanned.Should().Be(1);

        var result = await backfill.RunAsync(dryRun: false);
        result.MigratedOrPlanned.Should().Be(1);
        result.Errors.Should().BeEmpty();

        var route = await routes.FindAsync(legacyOrder.Id);
        route.Should().NotBeNull();

        var shardIndex = harness.Resolver.GetShardIndex(userId);
        await using var shardDb = factory.CreateShardContext(shardIndex);
        (await shardDb.Orders.AnyAsync(o => o.Id == legacyOrder.Id)).Should().BeTrue();

        var analytics = new AnalyticsService(harness.RoutingDb, harness.ShardedDb);
        (await analytics.GetSummaryAsync()).TotalRevenue.Should().Be(88m);
    }
}
