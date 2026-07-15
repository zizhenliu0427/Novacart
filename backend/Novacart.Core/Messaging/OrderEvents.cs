namespace Novacart.Api.Messaging;

using Novacart.Api.Services;

/// <summary>Line item for stock reservation (included in <see cref="PaymentCompleted"/>).</summary>
public record PaymentStockLineItem(Guid ProductId, int Quantity);

/// <summary>Published after Stripe confirms payment (Order service, via outbox).</summary>
public record PaymentCompleted(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    string StripeEventId,
    string CustomerEmail,
    IReadOnlyList<PaymentStockLineItem> Lines);

public record StockReserved(Guid OrderId);
public record StockReservationFailed(Guid OrderId, string Reason);

public record OrderPaid(Guid OrderId, Guid UserId, string Email);
public record ClearCartForOrder(Guid OrderId, Guid UserId);

/// <summary>Async email command published when MassTransit replaces in-process <see cref="Services.EmailQueue"/>.</summary>
public record SendEmailRequested(
    EmailKind Kind,
    string Recipient,
    string? OrderNumber,
    decimal? OrderTotal,
    string? NewStatus);
