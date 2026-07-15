using Xunit;
using FluentAssertions;
using Novacart.Api.Search;
using Novacart.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Square.Models;

namespace Novacart.Api.Tests;

public class SquareCatalogueServiceTests
{

    [Fact]
    public async Task SyncProductsAsync_SimulatesSync_WhenTokenIsPlaceholder()
    {
        // Arrange
        using var db = TestDbFactory.Create();

        var inMemorySettings = new Dictionary<string, string> {
            {"Square:AccessToken", "placeholder"},
            {"Square:Environment", "sandbox"}
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Gateway is never invoked on the simulation path; pass a throwing stub to prove it.
        var svc = new SquareCatalogueService(
            db, config, NullLogger<SquareCatalogueService>.Instance,
            new ThrowingGateway(), NullProductSearchIndexer.Instance);

        // Act
        var result = await svc.SyncProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.CategoriesCreated.Should().BeGreaterThan(0);
        result.ProductsCreated.Should().Be(3);
        result.Message.Should().Contain("[Simulation Mode]");

        // Verify products are added to DB
        var products = await db.Products.Where(p => p.Slug.StartsWith("square-")).ToListAsync();
        products.Should().HaveCount(3);
        products.Any(p => p.Name == "Square Premium Headphones").Should().BeTrue();
    }

    [Fact]
    public async Task SyncProductsAsync_RealPath_MapsCategoriesAndItems_AndAppliesPricing()
    {
        // Arrange — a real (non-placeholder) token routes through the gateway.
        using var db = TestDbFactory.Create();

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Square:AccessToken", "EAAAreal_token_for_test"},
                {"Square:Environment", "sandbox"}
            }!)
            .Build();

        var gateway = new StubGateway();
        var svc = new SquareCatalogueService(
            db, config, NullLogger<SquareCatalogueService>.Instance, gateway,
            NullProductSearchIndexer.Instance);

        // Act
        var result = await svc.SyncProductsAsync();

        // Assert — counts
        result.Success.Should().BeTrue();
        result.CategoriesCreated.Should().Be(1); // "Audio Gear" (not in seed data)
        result.ProductsCreated.Should().Be(2);   // Headphones + Keyboard

        // Assert — category persisted
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Audio Gear");
        category.Should().NotBeNull();

        // Assert — product mapping: slug derived, price = amount/100, currency, category link, square_id metadata
        var headphones = await db.Products.FirstOrDefaultAsync(p => p.Slug == "wireless-headphones");
        headphones.Should().NotBeNull();
        headphones!.Name.Should().Be("Wireless Headphones");
        headphones.Description.Should().Be("Noise-cancelling over-ear");
        headphones.Price.Should().Be(199.99m);        // 19999 cents -> 199.99
        headphones.Currency.Should().Be("AUD");
        headphones.CategoryId.Should().Be(category!.Id);
        headphones.Metadata.Should().Contain("square_cat_item_1");

        var keyboard = await db.Products.FirstOrDefaultAsync(p => p.Slug == "mechanical-keyboard");
        keyboard.Should().NotBeNull();
        keyboard!.Price.Should().Be(0m);              // no variation price -> defaults to 0

        // Assert — gateway was actually used (not the simulation branch)
        gateway.TypesRequested.Should().ContainInOrder("CATEGORY", "ITEM");
    }

    [Fact]
    public async Task SyncProductsAsync_RealPath_UpdatesExistingProductsBySlug()
    {
        // Arrange
        using var db = TestDbFactory.Create();

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Square:AccessToken", "EAAAreal_token_for_test"},
                {"Square:Environment", "sandbox"}
            }!)
            .Build();

        var gateway = new StubGateway();
        var svc = new SquareCatalogueService(
            db, config, NullLogger<SquareCatalogueService>.Instance, gateway,
            NullProductSearchIndexer.Instance);

        // Act — first sync creates, second sync updates
        var first = await svc.SyncProductsAsync();
        var second = await svc.SyncProductsAsync();

        // Assert — second run updates instead of duplicating
        first.ProductsCreated.Should().Be(2);
        second.ProductsCreated.Should().Be(0);
        second.ProductsUpdated.Should().Be(2);

        var count = await db.Products.CountAsync(p => p.Slug == "wireless-headphones");
        count.Should().Be(1, "re-syncing should update in place, not duplicate");
    }

    /// <summary>A fake gateway returning a fixed Square-shaped catalogue payload built from SDK POCOs.</summary>
    private class StubGateway : ISquareCatalogueGateway
    {
        public List<string> TypesRequested { get; } = new();

        public Task<IList<CatalogObject>> ListAsync(string types)
        {
            TypesRequested.Add(types);
            IList<CatalogObject> objects = types == "CATEGORY" ? StubCategories() : StubItems();
            return Task.FromResult(objects);
        }

        private static List<CatalogObject> StubCategories() => new()
        {
            new CatalogObject(
                type: "CATEGORY",
                id: "square_cat_1",
                categoryData: new CatalogCategory(name: "Audio Gear"))
        };

        private static List<CatalogObject> StubItems() => new()
        {
            // Item with a priced variation
            new CatalogObject(
                type: "ITEM",
                id: "square_cat_item_1",
                itemData: new CatalogItem(
                    name: "Wireless Headphones",
                    description: "Noise-cancelling over-ear",
                    categoryId: "square_cat_1",
                    variations: new List<CatalogObject>
                    {
                        new CatalogObject(
                            type: "ITEM_VARIATION",
                            id: "var_1",
                            itemVariationData: new CatalogItemVariation(
                                itemId: "square_cat_item_1",
                                name: "Regular",
                                priceMoney: new Money(amount: 19999L, currency: "AUD")))
                    })),
            // Item with no variation price -> price stays 0
            new CatalogObject(
                type: "ITEM",
                id: "square_cat_item_2",
                itemData: new CatalogItem(
                    name: "Mechanical Keyboard",
                    description: "Clicky switches",
                    categoryId: "square_cat_1"))
        };
    }

    /// <summary>A gateway that always throws — used to prove the simulation path never calls it.</summary>
    private class ThrowingGateway : ISquareCatalogueGateway
    {
        public Task<IList<CatalogObject>> ListAsync(string types)
            => throw new InvalidOperationException("Gateway must not be called on the simulation path.");
    }
}
