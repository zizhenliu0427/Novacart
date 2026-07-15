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
/// Idempotent stock reservation from <see cref="PaymentCompleted"/> payload (Product DB only).
/// </summary>
public class StockReservationService(
    AppDbContext db,
    IRedisDistributedLockService locks,
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

        var productIds = requiredByProduct.Keys.ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var (productId, qty) in requiredByProduct)
        {
            if (!products.TryGetValue(productId, out var product) || product.StockQuantity < qty)
            {
                db.Set<ProcessedStockOrder>().Add(new ProcessedStockOrder
                {
                    OrderId = orderId,
                    Outcome = StockReservationOutcomes.InsufficientStock,
                    ProcessedAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync(cancellationToken);
                logger.LogWarning(
                    "StockReservation: insufficient stock for product {ProductId} on order {OrderId}",
                    productId,
                    orderId);
                return StockReservationOutcome.InsufficientStock;
            }
        }

        foreach (var (productId, qty) in requiredByProduct)
            products[productId].StockQuantity -= qty;

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

    private sealed class LockScope(IReadOnlyList<IDistributedLockHandle> handles) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            for (var i = handles.Count - 1; i >= 0; i--)
                await handles[i].DisposeAsync();
        }
    }
}
