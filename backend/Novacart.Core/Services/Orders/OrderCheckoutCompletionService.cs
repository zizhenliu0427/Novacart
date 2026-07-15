using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Novacart.Api.Services.Payments;

namespace Novacart.Api.Services.Orders;

public record OrderCheckoutCompletionResult(Guid UserId, string Email);

public interface IOrderCheckoutCompletionService
{
    /// <summary>Mark order paid. Returns null when already processed (idempotent).</summary>
    Task<OrderCheckoutCompletionResult?> TryMarkPaidAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task CancelAfterStockFailureAsync(Guid orderId, string reason, CancellationToken cancellationToken = default);
}

/// <summary>Order-side checkout mutations shared by the checkout saga.</summary>
public class OrderCheckoutCompletionService(
    AppDbContext db,
    IRedisCacheService cache,
    IStripeRefundService refundService,
    ILogger<OrderCheckoutCompletionService> logger) : IOrderCheckoutCompletionService
{
    public async Task<OrderCheckoutCompletionResult?> TryMarkPaidAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || order.CurrentStatus != OrderStatuses.Pending)
            return null;

        order.CurrentStatus = OrderStatuses.Paid;

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        if (payment is not null)
            payment.Status = PaymentStatuses.Succeeded;

        await db.SaveChangesAsync(cancellationToken);

        var email = !string.IsNullOrWhiteSpace(order.CustomerEmail)
            ? order.CustomerEmail
            : (await db.Users.FindAsync([order.UserId], cancellationToken))?.Email;

        if (string.IsNullOrWhiteSpace(email))
            return null;

        await cache.RemoveByPrefixAsync($"orders:user:{order.UserId}:");
        await cache.RemoveByPrefixAsync("products:list:");

        logger.LogInformation("Order {OrderId} marked Paid via checkout saga", orderId);
        return new OrderCheckoutCompletionResult(order.UserId, email);
    }

    public async Task CancelAfterStockFailureAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null || order.CurrentStatus != OrderStatuses.Pending)
            return;

        order.CurrentStatus = OrderStatuses.Cancelled;

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        if (payment is not null)
        {
            if (payment.Status == PaymentStatuses.Succeeded)
            {
                var refund = await refundService.TryRefundAsync(payment, cancellationToken);
                payment.Status = refund.Success ? PaymentStatuses.Refunded : PaymentStatuses.Failed;
                if (!refund.Success)
                {
                    logger.LogWarning(
                        "Order {OrderId} cancelled after stock failure but Stripe refund failed: {Error}",
                        orderId,
                        refund.ErrorMessage);
                }
            }
            else
            {
                payment.Status = PaymentStatuses.Failed;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning(
            "Order {OrderId} cancelled after stock failure: {Reason}",
            orderId,
            reason);
    }
}
