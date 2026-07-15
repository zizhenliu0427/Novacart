using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services.Catalog;

public interface IProductCatalogStore
{
    Task<Product?> FindProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceRule>> LoadActiveRulesForProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default);
}

/// <summary>Uses commerce DB (monolith / shared schema).</summary>
public class DbProductCatalogStore(AppDbContext db) : IProductCatalogStore
{
    public Task<Product?> FindProductAsync(Guid productId, CancellationToken cancellationToken = default)
        => db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    public async Task<IReadOnlyList<PriceRule>> LoadActiveRulesForProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
            return Array.Empty<PriceRule>();

        return await db.PriceRules
            .Where(r => r.IsActive &&
                        ((r.StartsAt == null || r.StartsAt <= DateTime.UtcNow) &&
                         (r.EndsAt == null || r.EndsAt >= DateTime.UtcNow)) &&
                        ((r.ProductId != null && productIds.Contains(r.ProductId.Value)) ||
                         (r.CategoryId != null && categoryIds.Contains(r.CategoryId.Value)) ||
                         (r.ProductId == null && r.CategoryId == null)))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Reads catalog from Product service database (Phase 5).</summary>
public class IsolatedProductCatalogStore(ProductReadDbContext productDb) : IProductCatalogStore
{
    public Task<Product?> FindProductAsync(Guid productId, CancellationToken cancellationToken = default)
        => productDb.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    public async Task<IReadOnlyList<PriceRule>> LoadActiveRulesForProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
            return Array.Empty<PriceRule>();

        return await productDb.PriceRules.AsNoTracking()
            .Where(r => r.IsActive &&
                        ((r.StartsAt == null || r.StartsAt <= DateTime.UtcNow) &&
                         (r.EndsAt == null || r.EndsAt >= DateTime.UtcNow)) &&
                        ((r.ProductId != null && productIds.Contains(r.ProductId.Value)) ||
                         (r.CategoryId != null && categoryIds.Contains(r.CategoryId.Value)) ||
                         (r.ProductId == null && r.CategoryId == null)))
            .ToListAsync(cancellationToken);
    }
}
