using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Search;
using Novacart.Api.Services;
using Xunit;

namespace Novacart.Api.Tests;

public class ProductSearchDocumentMapperTests
{
    [Fact]
    public void ExtractMetadataText_FlattensJsonObject()
    {
        var text = ProductSearchDocumentMapper.ExtractMetadataText(
            """{"material":"merino wool","style":"crew-neck","sizes":["S","M","L"]}""");

        text.Should().Contain("merino wool");
        text.Should().Contain("crew-neck");
        text.Should().Contain("S");
    }

    [Fact]
    public void ToDocument_MapsCategoryAndTags()
    {
        var product = new Novacart.Api.Models.Entities.Product
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Slug = "test-product",
            Name = "Test Product",
            Description = "A searchable description",
            Price = 19.99m,
            Currency = "AUD",
            StockQuantity = 5,
            CategoryId = 2,
            Tags = new[] { "organic", "bestseller" },
            Metadata = """{"color":"navy"}""",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var doc = ProductSearchDocumentMapper.ToDocument(product, "Apparel");

        doc.Id.Should().Be(product.Id.ToString());
        doc.CategoryName.Should().Be("Apparel");
        doc.Tags.Should().Contain("organic");
        doc.MetadataText.Should().Contain("navy");
    }
}
