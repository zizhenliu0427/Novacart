using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Mappers;

namespace Novacart.Api.Services;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetAllAsync(
        string? q, int? categoryId, int[]? categoryIds, string? sort,
        decimal? minPrice, decimal? maxPrice, string? tag,
        int page, int pageSize);

    Task<ProductDetailDto> GetByIdAsync(Guid id);

    /// <summary>
    /// Price resolver seam — P1 returns the product's base price.
    /// P2 dynamic pricing rules plug in here without refactoring callers.
    /// </summary>
    decimal GetEffectivePrice(Product product);

    /// <summary>Invalidate all cached product lists (call after admin product changes).</summary>
    Task InvalidateProductCacheAsync();
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IPricingService _pricing;
    private readonly IRedisCacheService _cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    public ProductService(AppDbContext db, IPricingService pricing, IRedisCacheService cache)
    {
        _db = db;
        _pricing = pricing;
        _cache = cache;
    }

    // ── Static helper (safe inside EF LINQ expressions) ───────
    /// <summary>Fallback base-price resolver used where dynamic pricing isn't available.</summary>
    public static decimal ResolvePrice(Product product) => product.Price;

    public async Task<PagedResult<ProductListItemDto>> GetAllAsync(
        string? q, int? categoryId, int[]? categoryIds, string? sort,
        decimal? minPrice, decimal? maxPrice, string? tag,
        int page, int pageSize)
    {
        // Build a cache key that includes all query parameters
        var catIds = categoryIds is { Length: > 0 } ? string.Join(",", categoryIds) : "";
        var cacheKey = $"products:list:q={q ?? ""}:c={categoryId}:cs={catIds}:s={sort ?? "newest"}:min={minPrice}:max={maxPrice}:tag={tag ?? ""}:p{page}:s{pageSize}";

        var cached = await _cache.GetAsync<PagedResult<ProductListItemDto>>(cacheKey);
        if (cached is not null) return cached;

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

        // Category filter — single or multi
        if (categoryIds is { Length: > 0 })
            query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
        else if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        // Price range filter
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        // Tag filter
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(p => p.Tags.Contains(tag.Trim()));

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

        var items = pageItems.Select(p => ProductMapper.ToListItemDto(
            p, _pricing.ResolveEffectivePrice(p, activeRules)
        )).ToList();

        var result = new PagedResult<ProductListItemDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };

        await _cache.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<ProductDetailDto> GetByIdAsync(Guid id)
    {
        var p = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw AppException.NotFound("Product");

        var activeRules = await LoadActiveRulesAsync(new[] { p });

        return ProductMapper.ToDetailDto(p, _pricing.ResolveEffectivePrice(p, activeRules));
    }

    /// <inheritdoc/>
    public decimal GetEffectivePrice(Product product) => ResolvePrice(product);

    public async Task InvalidateProductCacheAsync()
    {
        await _cache.RemoveByPrefixAsync("products:list:");
    }

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
