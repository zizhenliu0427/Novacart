namespace Novacart.Api.Search;

/// <summary>Elasticsearch document shape for storefront product search.</summary>
public sealed class ProductSearchDocument
{
    public string Id { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public double Price { get; set; }

    public string Currency { get; set; } = "AUD";

    public int StockQuantity { get; set; }

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>Flattened jsonb metadata for full-text matching on material/style fields.</summary>
    public string? MetadataText { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
