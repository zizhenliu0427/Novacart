using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IAdminProductService
{
    Task<PagedResult<AdminProductDto>> GetAllAsync(string? q, bool? isActive, int page, int pageSize);
    Task<AdminProductDto> GetByIdAsync(Guid id);
    Task<IReadOnlyList<CategoryOptionDto>> GetCategoriesAsync();
    Task<AdminProductDto> CreateAsync(AdminProductUpsertRequest request);
    Task<AdminProductDto> UpdateAsync(Guid id, AdminProductUpsertRequest request);
    Task DeactivateAsync(Guid id);
}

/// <summary>P2-8 catalogue and inventory management.</summary>
public sealed class AdminProductService : IAdminProductService
{
    private readonly AppDbContext _db;
    private readonly IRedisCacheService _cache;

    public AdminProductService(AppDbContext db, IRedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PagedResult<AdminProductDto>> GetAllAsync(
        string? q, bool? isActive, int page, int pageSize)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Slug.ToLower().Contains(term));
        }

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AdminProductDto>
        {
            Items = products.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AdminProductDto> GetByIdAsync(Guid id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw AppException.NotFound("Product");

        return Map(product);
    }

    public async Task<IReadOnlyList<CategoryOptionDto>> GetCategoriesAsync() =>
        await _db.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryOptionDto { Id = c.Id, Name = c.Name, Slug = c.Slug })
            .ToListAsync();

    public async Task<AdminProductDto> CreateAsync(AdminProductUpsertRequest request)
    {
        var normalized = Normalize(request);
        await ValidateReferencesAsync(normalized, null);

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Name = normalized.Name,
            Slug = normalized.Slug,
            Description = normalized.Description,
            Price = normalized.Price,
            Currency = normalized.Currency,
            StockQuantity = normalized.StockQuantity,
            CategoryId = normalized.CategoryId,
            Tags = normalized.Tags,
            Metadata = normalized.Metadata,
            IsActive = normalized.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Products.Add(product);
        await SaveAsync();
        await _cache.RemoveByPrefixAsync("products:list:");
        await ReloadCategoryAsync(product);
        return Map(product);
    }

    public async Task<AdminProductDto> UpdateAsync(Guid id, AdminProductUpsertRequest request)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw AppException.NotFound("Product");

        var normalized = Normalize(request);
        await ValidateReferencesAsync(normalized, id);

        product.Name = normalized.Name;
        product.Slug = normalized.Slug;
        product.Description = normalized.Description;
        product.Price = normalized.Price;
        product.Currency = normalized.Currency;
        product.StockQuantity = normalized.StockQuantity;
        product.CategoryId = normalized.CategoryId;
        product.Tags = normalized.Tags;
        product.Metadata = normalized.Metadata;
        product.IsActive = normalized.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await SaveAsync();
        await _cache.RemoveByPrefixAsync("products:list:");
        await ReloadCategoryAsync(product);
        return Map(product);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var product = await _db.Products.FindAsync(id)
            ?? throw AppException.NotFound("Product");

        if (!product.IsActive)
            return;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync("products:list:");
    }

    private async Task ValidateReferencesAsync(AdminProductUpsertRequest request, Guid? currentId)
    {
        var slugInUse = await _db.Products.AnyAsync(p =>
            p.Slug == request.Slug && (!currentId.HasValue || p.Id != currentId.Value));
        if (slugInUse)
            throw AppException.Conflict("A product with this slug already exists.");

        if (request.CategoryId.HasValue &&
            !await _db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value))
            throw AppException.NotFound("Category");
    }

    private static AdminProductUpsertRequest Normalize(AdminProductUpsertRequest request)
    {
        var metadata = string.IsNullOrWhiteSpace(request.Metadata) ? null : request.Metadata.Trim();
        if (metadata is not null)
        {
            try
            {
                using var document = JsonDocument.Parse(metadata);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    throw new JsonException();
            }
            catch (JsonException)
            {
                throw new AppException("Metadata must be a valid JSON object.");
            }
        }

        return new AdminProductUpsertRequest
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            Tags = request.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(30)
                .ToArray(),
            Metadata = metadata,
            IsActive = request.IsActive,
        };
    }

    private async Task SaveAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw AppException.Conflict("A product with this slug already exists.");
        }
    }

    private async Task ReloadCategoryAsync(Product product)
    {
        var reference = _db.Entry(product).Reference(p => p.Category);
        reference.IsLoaded = false;
        await reference.LoadAsync();
    }

    private static AdminProductDto Map(Product p) => new()
    {
        Id = p.Id,
        Slug = p.Slug,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Currency = p.Currency,
        StockQuantity = p.StockQuantity,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        Tags = p.Tags,
        Metadata = p.Metadata,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
    };
}
