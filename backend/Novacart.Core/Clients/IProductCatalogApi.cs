using Novacart.Api.Models.Dtos.Catalog;
using Refit;

namespace Novacart.Api.Clients;

/// <summary>Typed Refit client for Cart/Order → Product catalog reads (PE-1).</summary>
public interface IProductCatalogApi
{
    [Get("/api/internal/catalog/products/{productId}")]
    Task<CatalogProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);

    [Post("/api/internal/catalog/pricing-rules")]
    Task<IReadOnlyList<CatalogPriceRuleSnapshot>> GetActivePriceRulesAsync(
        [Body] CatalogPricingQuery query,
        CancellationToken cancellationToken = default);
}
