using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novacart.Api.Data;
using Novacart.Api.Infrastructure;
using Novacart.Api.Models.Dtos.Stock;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Services.Stock;

public enum StockHoldOutcome
{
    Held,
    InsufficientStock,
    LockNotAcquired,
    AlreadyHeld,
}

public interface IStockHoldService
{
    Task<StockHoldOutcome> TryHoldForOrderAsync(
        Guid orderId,
        IReadOnlyList<StockHoldLine> lines,
        CancellationToken cancellationToken = default);

    Task ReleaseForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<int> ExpireStaleHoldsAsync(CancellationToken cancellationToken = default);
}

public class StockHoldService(
    AppDbContext db,
    IRedisDistributedLockService locks,
    IProductStockRepository stock,
    IOptions<StockHoldOptions> options,
    ILogger<StockHoldService> logger) : IStockHoldService
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(5);

    public async Task<StockHoldOutcome> TryHoldForOrderAsync(
        Guid orderId,
        IReadOnlyList<StockHoldLine> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
            return StockHoldOutcome.InsufficientStock;

        var existing = await db.Set<StockHold>()
            .Where(h => h.OrderId == orderId && h.Status == StockHoldStatuses.Active && h.ExpiresAt > DateTime.UtcNow)
            .AnyAsync(cancellationToken);

        if (existing)
            return StockHoldOutcome.AlreadyHeld;

        var requiredByProduct = lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var lockKeys = requiredByProduct.Keys
            .Select(StockLockKeys.ForProduct)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var handles = await locks.TryAcquireAllAsync(lockKeys, LockExpiry, LockWait, cancellationToken);
        if (handles is null)
        {
            StockInventoryMetrics.LockNotAcquired.Add(1);
            return StockHoldOutcome.LockNotAcquired;
        }

        await using var _ = new LockScope(handles);

        var productIds = requiredByProduct.Keys.ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var (productId, qty) in requiredByProduct)
        {
            if (!products.TryGetValue(productId, out var product) || !product.IsActive)
            {
                StockInventoryMetrics.ReservationInsufficient.Add(1);
                return StockHoldOutcome.InsufficientStock;
            }

            var heldByOthers = await stock.GetActiveHoldQuantityAsync(productId, orderId, cancellationToken);
            var available = product.StockQuantity - heldByOthers;
            if (available < qty)
            {
                StockInventoryMetrics.ReservationInsufficient.Add(1);
                logger.LogWarning(
                    "StockHold: insufficient available stock for product {ProductId} order {OrderId} (need {Need}, available {Available})",
                    productId,
                    orderId,
                    qty,
                    available);
                return StockHoldOutcome.InsufficientStock;
            }
        }

        var expiresAt = DateTime.UtcNow.Add(options.Value.Ttl);
        foreach (var (productId, qty) in requiredByProduct)
        {
            db.Set<StockHold>().Add(new StockHold
            {
                OrderId = orderId,
                ProductId = productId,
                Quantity = qty,
                Status = StockHoldStatuses.Active,
                ExpiresAt = expiresAt,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        StockInventoryMetrics.HoldsCreated.Add(requiredByProduct.Count);
        logger.LogInformation("StockHold: held inventory for order {OrderId} until {ExpiresAt}", orderId, expiresAt);
        return StockHoldOutcome.Held;
    }

    public async Task ReleaseForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var holds = await db.Set<StockHold>()
            .Where(h => h.OrderId == orderId && h.Status == StockHoldStatuses.Active)
            .ToListAsync(cancellationToken);

        if (holds.Count == 0)
            return;

        foreach (var hold in holds)
            hold.Status = StockHoldStatuses.Released;

        await db.SaveChangesAsync(cancellationToken);
        StockInventoryMetrics.HoldsReleased.Add(holds.Count);
        logger.LogInformation("StockHold: released {Count} holds for order {OrderId}", holds.Count, orderId);
    }

    public async Task<int> ExpireStaleHoldsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var stale = await db.Set<StockHold>()
            .Where(h => h.Status == StockHoldStatuses.Active && h.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
            return 0;

        foreach (var hold in stale)
            hold.Status = StockHoldStatuses.Expired;

        await db.SaveChangesAsync(cancellationToken);
        StockInventoryMetrics.HoldsExpired.Add(stale.Count);
        logger.LogInformation("StockHold: expired {Count} stale holds", stale.Count);
        return stale.Count;
    }

    private sealed class LockScope(IReadOnlyList<IDistributedLockHandle> handles) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            for (var i = handles.Count - 1; i >= 0; i--)
                await handles[i].DisposeAsync();
        }
    }
}
