using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Data;
using Novacart.Api.Messaging;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Novacart.Api.Services.Stock;
using Xunit;

namespace Novacart.Api.Tests;

public class StockReservationServiceTests
{
    [Fact]
    public async Task TryReserveAsync_DecrementsStock_WhenLinesAvailable()
    {
        await using var db = TestDbFactory.Create();
        var product = await db.Products.FirstAsync();
        product.StockQuantity = 10;
        await db.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        var payment = new PaymentCompleted(
            orderId,
            "ORD-1",
            Guid.NewGuid(),
            "evt_1",
            "buyer@test.com",
            [new PaymentStockLineItem(product.Id, 2)]);

        var locks = new FakeDistributedLockService();
        var svc = new StockReservationService(
            db, locks, new ProductStockRepository(db), NullLogger<StockReservationService>.Instance);

        var outcome = await svc.TryReserveAsync(payment);

        Assert.Equal(StockReservationOutcome.Reserved, outcome);
        await db.Entry(product).ReloadAsync();
        Assert.Equal(8, product.StockQuantity);
        Assert.True(await db.Set<ProcessedStockOrder>().AnyAsync(p => p.OrderId == orderId));
    }

    [Fact]
    public async Task TryReserveAsync_IsIdempotent_WhenAlreadyProcessed()
    {
        await using var db = TestDbFactory.Create();
        var product = await db.Products.FirstAsync();
        product.StockQuantity = 10;
        db.Set<ProcessedStockOrder>().Add(new ProcessedStockOrder
        {
            OrderId = Guid.NewGuid(),
            Outcome = StockReservationOutcomes.Reserved,
        });
        await db.SaveChangesAsync();

        var orderId = db.Set<ProcessedStockOrder>().Select(p => p.OrderId).First();
        var payment = new PaymentCompleted(
            orderId, "ORD-2", Guid.NewGuid(), "evt_2", "buyer@test.com",
            [new PaymentStockLineItem(product.Id, 1)]);

        var svc = new StockReservationService(
            db, new FakeDistributedLockService(), new ProductStockRepository(db), NullLogger<StockReservationService>.Instance);

        var outcome = await svc.TryReserveAsync(payment);

        Assert.Equal(StockReservationOutcome.AlreadyProcessed, outcome);
        await db.Entry(product).ReloadAsync();
        Assert.Equal(10, product.StockQuantity);
    }

    private sealed class FakeDistributedLockService : IRedisDistributedLockService
    {
        public Task<IDistributedLockHandle?> TryAcquireAsync(
            string key, TimeSpan expiry, TimeSpan wait, CancellationToken cancellationToken = default)
            => Task.FromResult<IDistributedLockHandle?>(new FakeHandle(key));

        public async Task<IReadOnlyList<IDistributedLockHandle>?> TryAcquireAllAsync(
            IEnumerable<string> keys, TimeSpan expiry, TimeSpan wait, CancellationToken cancellationToken = default)
        {
            var list = new List<IDistributedLockHandle>();
            foreach (var key in keys.Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal))
            {
                var h = await TryAcquireAsync(key, expiry, wait, cancellationToken);
                if (h is null) return null;
                list.Add(h);
            }
            return list;
        }

        private sealed class FakeHandle(string key) : IDistributedLockHandle
        {
            public string Key { get; } = key;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
