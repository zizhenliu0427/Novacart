using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Novacart.Api.Data;
using Novacart.Api.Messaging;
using Novacart.Api.Models.Dtos.Admin;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Services.Orders;

public interface ICheckoutSagaAdminService
{
    Task<CheckoutSagaListResponse> ListSagasAsync(string? state, int limit, CancellationToken cancellationToken = default);

    Task RetryCheckoutAsync(Guid orderId, CancellationToken cancellationToken = default);
}

/// <summary>Admin visibility and manual retry for checkout sagas (PE-5).</summary>
public class CheckoutSagaAdminService(
    AppDbContext db,
    IPublishEndpoint publishEndpoint,
    ILogger<CheckoutSagaAdminService> logger) : ICheckoutSagaAdminService
{
    private static readonly HashSet<string> RetryableStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "AwaitingStock",
        "Failed",
    };

    public async Task<CheckoutSagaListResponse> ListSagasAsync(
        string? state,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);

        var query = db.OrderCheckoutStates.AsNoTracking();

        if (string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(s =>
                s.CurrentState == "Failed" || s.CurrentState == "AwaitingStock");
        }
        else if (!state.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.CurrentState == state);
        }

        var sagas = await query
            .OrderByDescending(s => s.OrderNumber)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (sagas.Count == 0)
            return new CheckoutSagaListResponse(DateTime.UtcNow, Array.Empty<CheckoutSagaSummaryDto>());

        var orderIds = sagas.Select(s => s.OrderId).ToList();
        var orders = await db.Orders.AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.CurrentStatus })
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var items = sagas.Select(s =>
        {
            orders.TryGetValue(s.OrderId, out var order);
            var canRetry = RetryableStates.Contains(s.CurrentState)
                && order?.CurrentStatus == OrderStatuses.Pending;

            return new CheckoutSagaSummaryDto(
                s.CorrelationId,
                s.OrderId,
                s.CurrentState,
                s.OrderNumber,
                s.UserId,
                s.UserEmail,
                order?.CurrentStatus,
                canRetry);
        }).ToList();

        return new CheckoutSagaListResponse(DateTime.UtcNow, items);
    }

    public async Task RetryCheckoutAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var saga = await db.OrderCheckoutStates.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken)
            ?? throw AppException.NotFound("Checkout saga");

        if (!RetryableStates.Contains(saga.CurrentState))
            throw new AppException(
                $"Saga is in state '{saga.CurrentState}' and cannot be retried.",
                StatusCodes.Status409Conflict);

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw AppException.NotFound("Order");

        if (order.CurrentStatus != OrderStatuses.Pending)
            throw new AppException(
                $"Order is '{order.CurrentStatus}'; only pending orders can be retried.",
                StatusCodes.Status409Conflict);

        if (order.Items.Count == 0)
            throw new AppException("Order has no line items.", StatusCodes.Status422UnprocessableEntity);

        var lines = order.Items
            .Select(i => new PaymentStockLineItem(i.ProductId, i.Quantity))
            .ToList();

        var eventId = $"admin-retry-{Guid.NewGuid():N}";

        await publishEndpoint.Publish(
            new PaymentCompleted(
                order.Id,
                order.OrderNumber,
                order.UserId,
                eventId,
                order.CustomerEmail,
                lines),
            cancellationToken);

        logger.LogWarning(
            "Admin retry: republished PaymentCompleted for order {OrderId} (saga state {SagaState})",
            orderId,
            saga.CurrentState);
    }
}
