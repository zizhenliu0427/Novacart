namespace Novacart.Api.Models.Entities;

/// <summary>
/// A single line in an order. Name and price are snapshotted ("frozen") at purchase
/// time so historical orders are unaffected by later product edits.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string ProductNameSnapshot { get; set; } = string.Empty;

    public decimal PriceAtPurchase { get; set; }

    public int Quantity { get; set; }
}
