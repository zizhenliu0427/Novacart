using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Infrastructure.Sharding;

/// <summary>Copies legacy commerce orders into UserId-routed shards (PE-7 backfill).</summary>
public sealed class OrderShardBackfillService(
    AppDbContext legacyDb,
    IOrderDbContextFactory shardFactory,
    IOrderShardRouteStore routeStore,
    IOrderShardResolver resolver,
    IOptions<OrderShardingOptions> options)
{
    public async Task<OrderShardBackfillResult> RunAsync(
        bool dryRun = true,
        bool deleteLegacy = false,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled || resolver.ShardCount <= 1)
            throw new InvalidOperationException("Order sharding must be enabled with ShardCount > 1.");

        var legacyOrders = await legacyDb.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var migrated = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var order in legacyOrders)
        {
            var existingRoute = await routeStore.FindAsync(order.Id, cancellationToken);
            if (existingRoute is not null)
            {
                skipped++;
                continue;
            }

            var shardIndex = resolver.GetShardIndex(order.UserId);

            try
            {
                if (!dryRun)
                {
                    await CopyOrderToShardAsync(order, shardIndex, cancellationToken);
                    await routeStore.RegisterAsync(order.Id, order.UserId, shardIndex, cancellationToken);

                    if (deleteLegacy)
                    {
                        var tracked = await legacyDb.Orders
                            .Include(o => o.Items)
                            .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);
                        if (tracked is not null)
                        {
                            legacyDb.OrderItems.RemoveRange(tracked.Items);
                            legacyDb.Orders.Remove(tracked);
                            await legacyDb.SaveChangesAsync(cancellationToken);
                        }
                    }
                }

                migrated++;
            }
            catch (Exception ex)
            {
                errors.Add($"Order {order.Id}: {ex.Message}");
            }
        }

        return new OrderShardBackfillResult(legacyOrders.Count, migrated, skipped, errors, dryRun, deleteLegacy);
    }

    private async Task CopyOrderToShardAsync(Order order, int shardIndex, CancellationToken cancellationToken)
    {
        await using var shardDb = shardFactory.CreateShardContext(shardIndex);

        if (await shardDb.Orders.AnyAsync(o => o.Id == order.Id, cancellationToken))
            return;

        var payments = await legacyDb.Payments.AsNoTracking()
            .Where(p => p.OrderId == order.Id)
            .ToListAsync(cancellationToken);

        var histories = await legacyDb.OrderStatusHistories.AsNoTracking()
            .Where(h => h.OrderId == order.Id)
            .ToListAsync(cancellationToken);

        shardDb.Orders.Add(CloneOrder(order));
        foreach (var item in order.Items)
            shardDb.OrderItems.Add(CloneItem(item, order.Id));

        foreach (var payment in payments)
            shardDb.Payments.Add(ClonePayment(payment));

        foreach (var history in histories)
            shardDb.OrderStatusHistories.Add(CloneHistory(history));

        await shardDb.SaveChangesAsync(cancellationToken);
    }

    private static Order CloneOrder(Order source) => new()
    {
        Id = source.Id,
        UserId = source.UserId,
        OrderNumber = source.OrderNumber,
        CustomerEmail = source.CustomerEmail,
        Subtotal = source.Subtotal,
        ShippingCost = source.ShippingCost,
        Tax = source.Tax,
        Total = source.Total,
        Currency = source.Currency,
        CurrentStatus = source.CurrentStatus,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        ShippingName = source.ShippingName,
        ShippingLine1 = source.ShippingLine1,
        ShippingLine2 = source.ShippingLine2,
        ShippingCity = source.ShippingCity,
        ShippingState = source.ShippingState,
        ShippingPostcode = source.ShippingPostcode,
        ShippingCountry = source.ShippingCountry,
    };

    private static OrderItem CloneItem(OrderItem source, Guid orderId) => new()
    {
        Id = source.Id,
        OrderId = orderId,
        ProductId = source.ProductId,
        ProductNameSnapshot = source.ProductNameSnapshot,
        PriceAtPurchase = source.PriceAtPurchase,
        Quantity = source.Quantity,
    };

    private static Payment ClonePayment(Payment source) => new()
    {
        Id = source.Id,
        OrderId = source.OrderId,
        PaymentMethodId = source.PaymentMethodId,
        ProviderTransactionId = source.ProviderTransactionId,
        Amount = source.Amount,
        Currency = source.Currency,
        Status = source.Status,
        RawResponse = source.RawResponse,
        CreatedAt = source.CreatedAt,
    };

    private static OrderStatusHistory CloneHistory(OrderStatusHistory source) => new()
    {
        Id = source.Id,
        OrderId = source.OrderId,
        FromStatus = source.FromStatus,
        ToStatus = source.ToStatus,
        ActorUserId = source.ActorUserId,
        Notes = source.Notes,
        CreatedAt = source.CreatedAt,
    };
}

public record OrderShardBackfillResult(
    int LegacyOrderCount,
    int MigratedOrPlanned,
    int SkippedExistingRoute,
    IReadOnlyList<string> Errors,
    bool DryRun,
    bool DeleteLegacy);
