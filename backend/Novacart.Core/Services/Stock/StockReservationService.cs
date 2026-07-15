using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Novacart.Api.Data;
using Novacart.Api.Messaging;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Services.Stock;

public enum StockReservationOutcome
{
    AlreadyProcessed,
    Reserved,
    InsufficientStock,
    LockNotAcquired,
}

public interface IStockReservationService
{
    Task<StockReservationOutcome> TryReserveAsync(
        PaymentCompleted payment,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Idempotent stock confirmation from <see cref="PaymentCompleted"/> (Product DB).
/// Confirms checkout holds and atomically decrements stock.
/// </summary>
public class StockReservationService(
    AppDbContext db,
    IRedisDistributedLockService locks,
    IProductStockRepository stock,
    ILogger<StockReservationService> logger) : IStockReservationService
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(5);

    public async Task<StockReservationOutcome> TryReserveAsync(
        PaymentCompleted payment,
        CancellationToken cancellationToken = default)
    {
        var orderId = payment.OrderId;

        if (await db.Set<ProcessedStockOrder>().AnyAsync(p => p.OrderId == orderId, cancellationToken))
        {
            logger.LogInformation("StockReservation: order {OrderId} already processed", orderId);
            return StockReservationOutcome.AlreadyProcessed;
        }

        if (payment.Lines.Count == 0)
        {
            logger.LogWarning("StockReservation: no lines on payment event for order {OrderId}", orderId);
            return StockReservationOutcome.InsufficientStock;
        }

        var lockKeys = payment.Lines
            .Select(l => StockLockKeys.ForProduct(l.ProductId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var handles = await locks.TryAcquireAllAsync(lockKeys, LockExpiry, LockWait, cancellationToken);
        if (handles is null)
        {
            StockInventoryMetrics.LockNotAcquired.Add(1);
            logger.LogWarning(
                "StockReservation: could not acquire locks for order {OrderId} ({KeyCount} products)",
                orderId,
                lockKeys.Count);
            return StockReservationOutcome.LockNotAcquired;
        }

        await using var _ = new LockScope(handles);

        if (await db.Set<ProcessedStockOrder>().AnyAsync(p => p.OrderId == orderId, cancellationToken))
            return StockReservationOutcome.AlreadyProcessed;

        var requiredByProduct = payment.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var activeHolds = await db.Set<StockHold>()
            .Where(h => h.OrderId == orderId && h.Status == StockHoldStatuses.Active && h.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (activeHolds.Count > 0)
        {
            var heldByProduct = activeHolds
                .GroupBy(h => h.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(h => h.Quantity));

            foreach (var (productId, qty) in requiredByProduct)
            {
                if (!heldByProduct.TryGetValue(productId, out var heldQty) || heldQty < qty)
                {
                    await RecordInsufficientAsync(orderId, cancellationToken);
                    return StockReservationOutcome.InsufficientStock;
                }
            }
        }
        else
        {
            foreach (var (productId, qty) in requiredByProduct)
            {
                var product = await db.Products.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

                var heldByOthers = await stock.GetActiveHoldQuantityAsync(productId, orderId, cancellationToken);
                var available = (product?.StockQuantity ?? 0) - heldByOthers;
                if (product is null || !product.IsActive || available < qty)
                {
                    await RecordInsufficientAsync(orderId, cancellationToken);
                    return StockReservationOutcome.InsufficientStock;
                }
            }
        }

        foreach (var (productId, qty) in requiredByProduct)
        {
            var remaining = await stock.TryDecrementStockAsync(productId, qty, cancellationToken);
            if (remaining is null)
            {
                StockInventoryMetrics.AtomicDecrementFailure.Add(1);
                StockInventoryMetrics.ReservationInsufficient.Add(1);
                await RecordInsufficientAsync(orderId, cancellationToken);
                logger.LogWarning(
                    "StockReservation: atomic decrement failed for product {ProductId} on order {OrderId}",
                    productId,
                    orderId);
                return StockReservationOutcome.InsufficientStock;
            }

            StockInventoryMetrics.AtomicDecrementSuccess.Add(1);
        }

        if (activeHolds.Count > 0)
        {
            foreach (var hold in activeHolds)
                hold.Status = StockHoldStatuses.Confirmed;

            StockInventoryMetrics.HoldsConfirmed.Add(activeHolds.Count);
        }

        db.Set<ProcessedStockOrder>().Add(new ProcessedStockOrder
        {
            OrderId = orderId,
            Outcome = StockReservationOutcomes.Reserved,
            ProcessedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("StockReservation: stock reserved for order {OrderId}", orderId);
        return StockReservationOutcome.Reserved;
    }

    private async Task RecordInsufficientAsync(Guid orderId, CancellationToken cancellationToken)
    {
        db.Set<ProcessedStockOrder>().Add(new ProcessedStockOrder
        {
            OrderId = orderId,
            Outcome = StockReservationOutcomes.InsufficientStock,
            ProcessedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
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
