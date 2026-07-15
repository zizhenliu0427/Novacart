using System.Text;
using System.Text.Json;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Search;

public static class ProductSearchDocumentMapper
{
    public static ProductSearchDocument ToDocument(Product product, string? categoryName) => new()
    {
        Id = product.Id.ToString(),
        Slug = product.Slug,
        Name = product.Name,
        Description = product.Description,
        Price = (double)product.Price,
        Currency = product.Currency,
        StockQuantity = product.StockQuantity,
        CategoryId = product.CategoryId,
        CategoryName = categoryName ?? product.Category?.Name,
        Tags = product.Tags ?? Array.Empty<string>(),
        MetadataText = ExtractMetadataText(product.Metadata),
        ImageUrl = product.ImageUrl,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
    };

    public static string ExtractMetadataText(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(metadata);
            var builder = new StringBuilder();
            FlattenJson(document.RootElement, builder);
            return builder.ToString().Trim();
        }
        catch (JsonException)
        {
            return metadata.Trim();
        }
    }

    private static void FlattenJson(JsonElement element, StringBuilder builder)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (builder.Length > 0)
                        builder.Append(' ');
                    builder.Append(property.Name);
                    builder.Append(' ');
                    FlattenJson(property.Value, builder);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    FlattenJson(item, builder);
                break;
            case JsonValueKind.String:
                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (builder.Length > 0)
                        builder.Append(' ');
                    builder.Append(text);
                }
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                if (builder.Length > 0)
                    builder.Append(' ');
                builder.Append(element.ToString());
                break;
        }
    }
}
