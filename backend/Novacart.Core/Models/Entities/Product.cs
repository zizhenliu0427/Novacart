namespace Novacart.Api.Models.Entities;

/// <summary>
/// A sellable product. <see cref="Tags"/> and <see cref="Metadata"/> are mapped to
/// PostgreSQL text[] / jsonb for future faceted search and type-specific fields.
/// </summary>
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "AUD";

    public int StockQuantity { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Denormalised tags for fast filtering (Postgres text[]).</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>Product-type-specific fields as raw JSON (Postgres jsonb).</summary>
    public string? Metadata { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
