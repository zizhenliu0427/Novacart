using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Mappers;

/// <summary>
/// P3-3 Mapper layer: static mappers for Product → DTO conversions.
/// Completes the Controller → Service → Mapper → Entity layering (README #13).
/// </summary>
public static class ProductMapper
{
    public static ProductListItemDto ToListItemDto(Product product, decimal effectivePrice)
    {
        return new ProductListItemDto
        {
            Id            = product.Id,
            Slug          = product.Slug,
            Name          = product.Name,
            Description   = product.Description,
            Price         = effectivePrice,
            Currency      = product.Currency,
            StockQuantity = product.StockQuantity,
            CategoryId    = product.CategoryId,
            CategoryName  = product.Category?.Name,
            Tags          = product.Tags,
        };
    }

    public static ProductDetailDto ToDetailDto(Product product, decimal effectivePrice)
    {
        return new ProductDetailDto
        {
            Id            = product.Id,
            Slug          = product.Slug,
            Name          = product.Name,
            Description   = product.Description,
            Price         = effectivePrice,
            Currency      = product.Currency,
            StockQuantity = product.StockQuantity,
            CategoryId    = product.CategoryId,
            CategoryName  = product.Category?.Name,
            Tags          = product.Tags,
            Metadata      = product.Metadata,
        };
    }
}
