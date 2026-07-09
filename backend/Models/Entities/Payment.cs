namespace Novacart.Api.Models.Entities;

/// <summary>
/// A logged payment transaction associated with an order.
/// </summary>
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;

    /// <summary>The gateway's transaction or session reference (e.g. Stripe Checkout Session ID).</summary>
    public string ProviderTransactionId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "AUD";

    /// <summary>Transaction status: pending, succeeded, failed, refunded.</summary>
    public string Status { get; set; } = PaymentStatuses.Pending;

    /// <summary>Raw JSON payload from the gateway (Postgres jsonb).</summary>
    public string? RawResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class PaymentStatuses
{
    public const string Pending = "pending";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Refunded = "refunded";
}
