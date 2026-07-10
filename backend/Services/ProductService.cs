using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetAllAsync(
        string? q, int? categoryId, string? sort, int page, int pageSize);

    Task<ProductDetailDto> GetByIdAsync(Guid id);

    /// <summary>
    /// Price resolver seam — P1 returns the product's base price.
    /// P2 dynamic pricing rules plug in here without refactoring callers.
    /// </summary>
    decimal GetEffectivePrice(Product product);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IPricingService _pricing;

    public ProductService(AppDbContext db, IPricingService pricing)
    {
        _db = db;
        _pricing = pricing;
    }

    // ── Static helper (safe inside EF LINQ expressions) ───────
    /// <summary>Fallback base-price resolver used where dynamic pricing isn't available.</summary>
    public static decimal ResolvePrice(Product product) => product.Price;

    public async Task<PagedResult<ProductListItemDto>> GetAllAsync(
        string? q, int? categoryId, string? sort, int page, int pageSize)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        // Keyword search (case-insensitive ILIKE via EF.Functions.ILike)
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                (p.Description != null && EF.Functions.ILike(p.Description, pattern)));
        }

        // Category filter
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        // Sorting
        query = sort switch
        {
            "price_asc"  => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc"   => query.OrderBy(p => p.Name),
            _            => query.OrderByDescending(p => p.CreatedAt), // "newest" default
        };

        var total = await query.CountAsync();

        // Materialise page first, then apply dynamic pricing post-query.
        var pageItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var activeRules = await LoadActiveRulesAsync(pageItems);

        var items = pageItems.Select(p => new ProductListItemDto
        {
            Id            = p.Id,
            Slug          = p.Slug,
            Name          = p.Name,
            Description   = p.Description,
            Price         = _pricing.ResolveEffectivePrice(p, activeRules),
            Currency      = p.Currency,
            StockQuantity = p.StockQuantity,
            CategoryId    = p.CategoryId,
            CategoryName  = p.Category != null ? p.Category.Name : null,
            Tags          = p.Tags,
        }).ToList();

        return new PagedResult<ProductListItemDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    public async Task<ProductDetailDto> GetByIdAsync(Guid id)
    {
        var p = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw AppException.NotFound("Product");

        var activeRules = await LoadActiveRulesAsync(new[] { p });

        return new ProductDetailDto
        {
            Id            = p.Id,
            Slug          = p.Slug,
            Name          = p.Name,
            Description   = p.Description,
            Price         = _pricing.ResolveEffectivePrice(p, activeRules),
            Currency      = p.Currency,
            StockQuantity = p.StockQuantity,
            CategoryId    = p.CategoryId,
            CategoryName  = p.Category?.Name,
            Tags          = p.Tags,
            Metadata      = p.Metadata,
        };
    }

    /// <inheritdoc/>
    public decimal GetEffectivePrice(Product product) => ResolvePrice(product);

    /// <summary>
    /// Load the pricing rules that could apply to any of the given products:
    /// product-scoped (by id), category-scoped (by category id), and global.
    /// </summary>
    private async Task<IReadOnlyCollection<PriceRule>> LoadActiveRulesAsync(
        IReadOnlyCollection<Product> products)
    {
        var productIds = products.Select(p => p.Id).Distinct().ToList();
        var categoryIds = products
            .Select(p => p.CategoryId)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .Distinct()
            .ToList();

        return await _db.PriceRules
            .Where(r => r.IsActive &&
                        ((r.StartsAt == null || r.StartsAt <= DateTime.UtcNow) &&
                         (r.EndsAt == null || r.EndsAt >= DateTime.UtcNow)) &&
                        ((r.ProductId != null && productIds.Contains(r.ProductId.Value)) ||
                         (r.CategoryId != null && categoryIds.Contains(r.CategoryId.Value)) ||
                         (r.ProductId == null && r.CategoryId == null)))
            .ToListAsync();
    }
}
