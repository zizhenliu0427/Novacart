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

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public string Currency { get; set; } = "AUD";

    public string CurrentStatus { get; set; } = OrderStatuses.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
