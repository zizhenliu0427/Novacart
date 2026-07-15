namespace Novacart.Api.Services.CartRedis;

public record CartItemSnapshot(Guid Id, Guid ProductId, int Quantity);

/// <summary>Redis-serialised cart payload (line items only; prices loaded from catalog on read).</summary>
public class CartRedisSnapshot
{
    public Guid CartId { get; set; }

    public Guid? UserId { get; set; }

    public string? SessionId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<CartItemSnapshot> Items { get; set; } = [];
}
