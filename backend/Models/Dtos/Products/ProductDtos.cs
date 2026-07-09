namespace Novacart.Api.Models.Dtos.Products;

/// <summary>Lightweight product item returned in list/search results.</summary>
public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "AUD";
    public int StockQuantity { get; set; }
    public string? CategoryName { get; set; }
    public int? CategoryId { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>Full product detail including metadata (type-specific attributes).</summary>
public class ProductDetailDto : ProductListItemDto
{
    /// <summary>Raw jsonb string from the DB — the frontend renders it as a dynamic attribute table.</summary>
    public string? Metadata { get; set; }
}

/// <summary>Paginated wrapper for list responses.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
