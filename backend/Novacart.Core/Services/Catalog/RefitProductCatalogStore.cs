using Novacart.Api.Clients;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services.Catalog;

/// <summary>Catalog reads via Refit + Product API (PE-1 service discovery).</summary>
public class RefitProductCatalogStore(IProductCatalogApi catalogApi) : IProductCatalogStore
{
    public async Task<Product?> FindProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var snapshot = await catalogApi.GetProductAsync(productId, cancellationToken);
        if (snapshot is null)
            return null;

        return new Product
        {
            Id = snapshot.Id,
            Name = snapshot.Name,
            Price = snapshot.Price,
            Currency = snapshot.Currency,
            StockQuantity = snapshot.StockQuantity,
            CategoryId = snapshot.CategoryId,
            IsActive = snapshot.IsActive,
        };
    }

    public async Task<IReadOnlyList<PriceRule>> LoadActiveRulesForProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
            return Array.Empty<PriceRule>();

        var snapshots = await catalogApi.GetActivePriceRulesAsync(
            new Models.Dtos.Catalog.CatalogPricingQuery(productIds.ToList(), categoryIds.ToList()),
            cancellationToken);

        return snapshots.Select(r => new PriceRule
        {
            Id = r.Id,
            RuleType = r.RuleType,
            Value = r.Value,
            ProductId = r.ProductId,
            CategoryId = r.CategoryId,
            IsActive = r.IsActive,
            StartsAt = r.StartsAt,
            EndsAt = r.EndsAt,
        }).ToList();
    }
}
