namespace Novacart.Api.Models.Entities;

/// <summary>
/// A placed order. <see cref="CurrentStatus"/> is a denormalised cache of the latest
/// status (pending → paid → processing → shipped → completed / cancelled).
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OrderNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Customer email snapshot at checkout (database-per-service; no Auth DB join).</summary>
    public string CustomerEmail { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public string Currency { get; set; } = "AUD";

    // Shipping Address Snapshot (P2-7)
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingLine1 { get; set; } = string.Empty;
    public string? ShippingLine2 { get; set; }
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingState { get; set; } = string.Empty;
    public string ShippingPostcode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = "Australia";

    public string CurrentStatus { get; set; } = OrderStatuses.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the order (or its status) was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

/// <summary>Well-known order status values (state machine defined in the ER doc).</summary>
public static class OrderStatuses
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Processing = "processing";
    public const string Shipped = "shipped";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
}
