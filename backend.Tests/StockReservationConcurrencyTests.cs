using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Data;
using Novacart.Api.Messaging;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Novacart.Api.Services.Stock;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Novacart.Api.Tests;

/// <summary>
/// PE-4: concurrent checkout against a low-stock SKU with real Redis locks.
/// Requires Docker — skipped when the daemon is unavailable.
/// </summary>
public class StockReservationConcurrencyTests : IAsyncLifetime
{
    private RedisContainer? _redis;
    private bool _started;

    public async Task InitializeAsync()
    {
        try
        {
            _redis = new RedisBuilder().WithImage("redis:7-alpine").Build();
            await _redis.StartAsync();
            _started = true;
        }
        catch
        {
            _started = false;
        }
    }

    public Task DisposeAsync() => _started && _redis is not null
        ? _redis.DisposeAsync().AsTask()
        : Task.CompletedTask;

    [Fact]
    public async Task TryReserveAsync_ConcurrentLowStock_ReservesExactlyAvailableQuantity()
    {
        if (!_started || _redis is null)
            return;

        const int initialStock = 5;
        const int concurrentOrders = 50;

        var dbName = Guid.NewGuid().ToString("N");
        await using var seedDb = TestDbFactory.Create(dbName);
        var product = await seedDb.Products.FirstAsync();
        product.StockQuantity = initialStock;
        await seedDb.SaveChangesAsync();
        var productId = product.Id;

        var mux = ConnectionMultiplexer.Connect(_redis.GetConnectionString());
        var locks = new RedisDistributedLockService(mux);

        var outcomes = new StockReservationOutcome[concurrentOrders];
        var tasks = Enumerable.Range(0, concurrentOrders).Select(async i =>
        {
            await using var db = TestDbFactory.Create(dbName);
            var svc = new StockReservationService(
                db, locks, new ProductStockRepository(db), NullLogger<StockReservationService>.Instance);

            var payment = new PaymentCompleted(
                Guid.NewGuid(),
                $"ORD-LOAD-{i:D3}",
                Guid.NewGuid(),
                $"evt_load_{i}",
                "load@test.com",
                [new PaymentStockLineItem(productId, 1)]);

            outcomes[i] = await svc.TryReserveAsync(payment);
        });

        await Task.WhenAll(tasks);

        outcomes.Count(o => o == StockReservationOutcome.Reserved).Should().Be(initialStock);
        outcomes.Count(o => o == StockReservationOutcome.InsufficientStock).Should().Be(concurrentOrders - initialStock);
        outcomes.Should().NotContain(StockReservationOutcome.LockNotAcquired);

        await using var verifyDb = TestDbFactory.Create(dbName);
        var finalProduct = await verifyDb.Products.SingleAsync(p => p.Id == productId);
        finalProduct.StockQuantity.Should().Be(0);

        var processed = await verifyDb.Set<ProcessedStockOrder>().CountAsync();
        processed.Should().Be(concurrentOrders);
    }
}
