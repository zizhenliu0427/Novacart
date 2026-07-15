namespace Novacart.Api.Models.Entities;

/// <summary>Checkout-time inventory hold (Product DB). Released on TTL, payment failure, or confirmed on payment success.</summary>
public class StockHold
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = StockHoldStatuses.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }
}

public static class StockHoldStatuses
{
    public const string Active = "active";
    public const string Confirmed = "confirmed";
    public const string Released = "released";
    public const string Expired = "expired";
}
