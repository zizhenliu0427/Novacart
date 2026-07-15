namespace Novacart.Api.Models.Entities;

/// <summary>
/// Shopping cart. Supports both authenticated users (UserId set) and guests
/// (SessionId set, UserId null) so P2 guest-cart-merge is data-compatible.
/// </summary>
public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Null for guest carts.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Browser session id for guest carts (optional for P1).</summary>
    public string? SessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

/// <summary>A single line item in a cart.</summary>
public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
