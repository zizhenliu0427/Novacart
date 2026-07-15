using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Mappers;
using Novacart.Api.Search;

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
    private readonly IProductSearchService _search;
    private readonly ILogger<ProductService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    public ProductService(
        AppDbContext db,
        IPricingService pricing,
        IRedisCacheService cache,
        IProductSearchService search,
        ILogger<ProductService> logger)
    {
        _db = db;
        _pricing = pricing;
        _cache = cache;
        _search = search;
        _logger = logger;
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

        PagedResult<ProductListItemDto> result;
        if (!string.IsNullOrWhiteSpace(q) && _search.IsEnabled)
        {
            result = await TryElasticsearchSearchAsync(
                q, categoryId, categoryIds, sort, minPrice, maxPrice, tag, page, pageSize)
                ?? await QueryPostgresAsync(
                    q, categoryId, categoryIds, sort, minPrice, maxPrice, tag, page, pageSize, searchEngine: "postgres");
        }
        else
        {
            result = await QueryPostgresAsync(
                q, categoryId, categoryIds, sort, minPrice, maxPrice, tag, page, pageSize, searchEngine: null);
        }

        await _cache.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    private async Task<PagedResult<ProductListItemDto>?> TryElasticsearchSearchAsync(
        string? q, int? categoryId, int[]? categoryIds, string? sort,
        decimal? minPrice, decimal? maxPrice, string? tag,
        int page, int pageSize)
    {
        try
        {
            if (!await _search.IsHealthyAsync())
                return null;

            var esResult = await _search.SearchAsync(new ProductSearchQuery(
                q!, categoryId, categoryIds, sort, minPrice, maxPrice, tag, page, pageSize));

            if (!esResult.UsedElasticsearch)
                return null;

            return await BuildResultFromIdsAsync(
                esResult.ProductIds, esResult.TotalCount, page, pageSize, searchEngine: "elasticsearch");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch search failed — falling back to Postgres ILIKE.");
            return null;
        }
    }

    private async Task<PagedResult<ProductListItemDto>> QueryPostgresAsync(
        string? q, int? categoryId, int[]? categoryIds, string? sort,
        decimal? minPrice, decimal? maxPrice, string? tag,
        int page, int pageSize, string? searchEngine)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                (p.Description != null && EF.Functions.ILike(p.Description, pattern)));
        }

        if (categoryIds is { Length: > 0 })
            query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
        else if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(p => p.Tags.Contains(tag.Trim()));

        query = sort switch
        {
            "price_asc"  => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc"   => query.OrderBy(p => p.Name),
            _            => query.OrderByDescending(p => p.CreatedAt),
        };

        var total = await query.CountAsync();

        var pageItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return await MapPageAsync(pageItems, total, page, pageSize, searchEngine);
    }

    private async Task<PagedResult<ProductListItemDto>> BuildResultFromIdsAsync(
        IReadOnlyList<Guid> orderedIds,
        int totalCount,
        int page,
        int pageSize,
        string searchEngine)
    {
        if (orderedIds.Count == 0)
        {
            return new PagedResult<ProductListItemDto>
            {
                Items = Array.Empty<ProductListItemDto>(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                SearchEngine = searchEngine,
            };
        }

        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => orderedIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        var byId = products.ToDictionary(p => p.Id);
        var ordered = orderedIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();

        return await MapPageAsync(ordered, totalCount, page, pageSize, searchEngine);
    }

    private async Task<PagedResult<ProductListItemDto>> MapPageAsync(
        IReadOnlyList<Product> pageItems,
        int totalCount,
        int page,
        int pageSize,
        string? searchEngine)
    {
        var activeRules = await LoadActiveRulesAsync(pageItems);

        return new PagedResult<ProductListItemDto>
        {
            Items = pageItems.Select(p => ProductMapper.ToListItemDto(
                p, _pricing.ResolveEffectivePrice(p, activeRules)
            )).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            SearchEngine = searchEngine,
        };
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
