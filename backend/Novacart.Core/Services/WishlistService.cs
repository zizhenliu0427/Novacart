using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

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
/// P2-3: per-user wishlist backed by the <c>wishlist_items</c> table. Dedupe is enforced
/// by the unique <c>(UserId, ProductId)</c> index in <c>AppDbContext</c>.
/// </summary>
public class WishlistService : IWishlistService
{
    private readonly AppDbContext _db;
    public WishlistService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WishlistItemDto>> GetAsync(Guid userId)
    {
        return await _db.WishlistItems
            .Include(w => w.Product)
            .Where(w => w.UserId == userId && w.Product.IsActive)
            .OrderByDescending(w => w.AddedAt)
            .Select(w => new WishlistItemDto(
                w.ProductId,
                w.Product.Name,
                w.Product.Slug,
                w.Product.Price,
                w.AddedAt))
            .ToListAsync();
    }

    public async Task AddAsync(Guid userId, Guid productId)
    {
        // Verify the product exists and is active.
        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive))
            throw AppException.NotFound("Product");

        // Idempotent: if already wishlisted, do nothing.
        if (await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId))
            return;

        _db.WishlistItems.Add(new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            AddedAt = DateTime.UtcNow,
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Race condition: unique index may reject a concurrent insert — treat as success.
        }
    }

    public async Task RemoveAsync(Guid userId, Guid productId)
    {
        var item = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

        if (item is null) return; // idempotent removal

        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync();
    }
}
