using Novacart.Api.Data;

namespace Novacart.Api.Services;

// ── DTOs (P2-3) ────────────────────────────────────────────
public record WishlistItemDto(Guid ProductId, string Name, string Slug, decimal Price, DateTime AddedAt);

/// <summary>P2-3 (Wishlist). See HANDOFF §7 P2-3.</summary>
public interface IWishlistService
{
    Task<IReadOnlyList<WishlistItemDto>> GetAsync(Guid userId);
    Task AddAsync(Guid userId, Guid productId);
    Task RemoveAsync(Guid userId, Guid productId);
}

/// <summary>
/// SCAFFOLD STUB — throws 501 until implemented. Fill in with `WishlistItems` CRUD
/// (dedupe on the unique (UserId, ProductId) index; project to <see cref="WishlistItemDto"/>).
/// </summary>
public class WishlistService : IWishlistService
{
    private readonly AppDbContext _db;
    public WishlistService(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<WishlistItemDto>> GetAsync(Guid userId) =>
        throw AppException.NotImplemented("P2-3: GET /api/wishlist not implemented yet.");

    public Task AddAsync(Guid userId, Guid productId) =>
        throw AppException.NotImplemented("P2-3: POST /api/wishlist/items not implemented yet.");

    public Task RemoveAsync(Guid userId, Guid productId) =>
        throw AppException.NotImplemented("P2-3: DELETE /api/wishlist/items/{productId} not implemented yet.");
}
