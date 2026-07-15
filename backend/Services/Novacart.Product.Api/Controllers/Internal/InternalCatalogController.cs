using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Catalog;

namespace Novacart.Api.Controllers.Internal;

/// <summary>Service-to-service catalog reads (Refit). Not routed through the public gateway.</summary>
[ApiController]
[Route("api/internal/catalog")]
[ApiExplorerSettings(IgnoreApi = true)]
public class InternalCatalogController(AppDbContext db) : ControllerBase
{
    [HttpGet("products/{id:guid}")]
    [ProducesResponseType(typeof(CatalogProductSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogProductSnapshot>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new CatalogProductSnapshot(
                p.Id,
                p.Name,
                p.Price,
                p.Currency,
                p.StockQuantity,
                p.CategoryId,
                p.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost("pricing-rules")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogPriceRuleSnapshot>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CatalogPriceRuleSnapshot>>> GetActivePriceRules(
        [FromBody] CatalogPricingQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ProductIds.Count == 0)
            return Ok(Array.Empty<CatalogPriceRuleSnapshot>());

        var now = DateTime.UtcNow;
        var rules = await db.PriceRules.AsNoTracking()
            .Where(r => r.IsActive &&
                        (r.StartsAt == null || r.StartsAt <= now) &&
                        (r.EndsAt == null || r.EndsAt >= now) &&
                        ((r.ProductId != null && query.ProductIds.Contains(r.ProductId.Value)) ||
                         (r.CategoryId != null && query.CategoryIds.Contains(r.CategoryId.Value)) ||
                         (r.ProductId == null && r.CategoryId == null)))
            .Select(r => new CatalogPriceRuleSnapshot(
                r.Id,
                r.RuleType,
                r.Value,
                r.ProductId,
                r.CategoryId,
                r.IsActive,
                r.StartsAt,
                r.EndsAt))
            .ToListAsync(cancellationToken);

        return Ok(rules);
    }
}
