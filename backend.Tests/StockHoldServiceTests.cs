using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Novacart.Api.Infrastructure;
using Novacart.Api.Models.Dtos.Stock;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Novacart.Api.Services.Stock;
using Xunit;

namespace Novacart.Api.Tests;

public class StockHoldServiceTests
{
    [Fact]
    public async Task TryHoldForOrderAsync_ReducesAvailableStock_ForConcurrentOrders()
    {
        await using var db = TestDbFactory.Create();
        var product = await db.Products.FirstAsync();
        product.StockQuantity = 3;
        await db.SaveChangesAsync();

        var locks = new FakeLockService();
        var svc = new StockHoldService(
            db,
            locks,
            new ProductStockRepository(db),
            Options.Create(new StockHoldOptions { TtlMinutes = 15 }),
            NullLogger<StockHoldService>.Instance);

        var order1 = Guid.NewGuid();
        var order2 = Guid.NewGuid();

        (await svc.TryHoldForOrderAsync(order1, [new StockHoldLine(product.Id, 2)])).Should().Be(StockHoldOutcome.Held);
        (await svc.TryHoldForOrderAsync(order2, [new StockHoldLine(product.Id, 2)])).Should().Be(StockHoldOutcome.InsufficientStock);

        var holds = await db.Set<StockHold>().CountAsync(h => h.Status == StockHoldStatuses.Active);
        holds.Should().Be(1);
    }

    [Fact]
    public async Task ExpireStaleHoldsAsync_ReleasesExpiredRows()
    {
        await using var db = TestDbFactory.Create();
        var product = await db.Products.FirstAsync();
        db.Set<StockHold>().Add(new StockHold
        {
            OrderId = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 1,
            Status = StockHoldStatuses.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();

        var svc = new StockHoldService(
            db,
            new FakeLockService(),
            new ProductStockRepository(db),
            Options.Create(new StockHoldOptions()),
            NullLogger<StockHoldService>.Instance);

        var expired = await svc.ExpireStaleHoldsAsync();
        expired.Should().Be(1);

        var hold = await db.Set<StockHold>().SingleAsync();
        hold.Status.Should().Be(StockHoldStatuses.Expired);
    }

    [Fact]
    public async Task ProductStockRepository_TryDecrementStockAsync_IsAtomic()
    {
        await using var db = TestDbFactory.Create();
        var product = await db.Products.FirstAsync();
        product.StockQuantity = 2;
        await db.SaveChangesAsync();

        var repo = new ProductStockRepository(db);
        (await repo.TryDecrementStockAsync(product.Id, 1)).Should().Be(1);
        (await repo.TryDecrementStockAsync(product.Id, 5)).Should().BeNull();

        var finalQty = await db.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync();
        finalQty.Should().Be(1);
    }

    private sealed class FakeLockService : IRedisDistributedLockService
    {
        public Task<IDistributedLockHandle?> TryAcquireAsync(
            string key,
            TimeSpan expiry,
            TimeSpan wait,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IDistributedLockHandle?>(new FakeHandle(key));

        public Task<IReadOnlyList<IDistributedLockHandle>?> TryAcquireAllAsync(
            IEnumerable<string> keys,
            TimeSpan expiry,
            TimeSpan wait,
            CancellationToken cancellationToken = default)
        {
            var list = keys.Select(k => (IDistributedLockHandle)new FakeHandle(k)).ToList();
            return Task.FromResult<IReadOnlyList<IDistributedLockHandle>?>(list);
        }

        private sealed class FakeHandle(string key) : IDistributedLockHandle
        {
            public string Key { get; } = key;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
