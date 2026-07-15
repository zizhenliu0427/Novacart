using System.ComponentModel.DataAnnotations;

namespace Novacart.Api.Models.Dtos.Products;

/// <summary>Admin catalogue row. Unlike the public DTO, inactive products are included.</summary>
public sealed class AdminProductDto : ProductDetailDto
{
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CategoryOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

/// <summary>Shared create/update payload for an admin-managed product.</summary>
public sealed class AdminProductUpsertRequest
{
    [Required, StringLength(300, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug must contain lowercase letters, numbers, and single hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "99999999")]
    public decimal Price { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "AUD";

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public int? CategoryId { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();

    [StringLength(50000)]
    public string? Metadata { get; set; }

    [Url, StringLength(1000)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
}
