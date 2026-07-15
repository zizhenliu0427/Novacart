namespace Novacart.Api.Models.Entities;

/// <summary>
/// P2-3 (Wishlist): one saved product per user. Unique on (UserId, ProductId)
/// so the same product can't be added twice. See HANDOFF §7 P2-3.
/// </summary>
public class WishlistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
