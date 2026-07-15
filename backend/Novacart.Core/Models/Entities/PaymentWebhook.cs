namespace Novacart.Api.Models.Entities;

/// <summary>
/// Log of received webhooks to ensure idempotency and prevent duplicate processing.
/// </summary>
public class PaymentWebhook
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;

    /// <summary>The unique event ID issued by the provider (e.g. Stripe evt_XXX) to enforce uniqueness.</summary>
    public string EventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    /// <summary>Raw event JSON payload (Postgres jsonb).</summary>
    public string Payload { get; set; } = string.Empty;

    public bool Processed { get; set; } = false;

    public string? ErrorMessage { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }
}
