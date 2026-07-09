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

    public ProductService(AppDbContext db) => _db = db;

    // ── Static helper (safe inside EF LINQ expressions) ───────
    /// <summary>P1 pass-through. P2 dynamic pricing rules replace this body.</summary>
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

        // Project in DB; use p.Price directly (EF can translate it).
        // GetEffectivePrice/ResolvePrice is called after materialisation where needed.
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto
            {
                Id            = p.Id,
                Slug          = p.Slug,
                Name          = p.Name,
                Description   = p.Description,
                Price         = p.Price,   // P2: swap for resolved price post-query
                Currency      = p.Currency,
                StockQuantity = p.StockQuantity,
                CategoryId    = p.CategoryId,
                CategoryName  = p.Category != null ? p.Category.Name : null,
                Tags          = p.Tags,
            })
            .ToListAsync();

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

        return new ProductDetailDto
        {
            Id            = p.Id,
            Slug          = p.Slug,
            Name          = p.Name,
            Description   = p.Description,
            Price         = ResolvePrice(p),
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
}
