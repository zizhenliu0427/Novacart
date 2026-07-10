using Xunit;
using FluentAssertions;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for ProductService — covers DB-backed listing, search, filter, sort, detail, and pricing seam.
/// Uses EF InMemory with the 12 seed products from AppDbContext.OnModelCreating.
/// </summary>
public class ProductServiceTests
{
    // ── Listing / Pagination ──────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveProducts()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        var result = await svc.GetAllAsync(null, null, null, page: 1, pageSize: 50);

        result.Items.Should().HaveCount(12, "there are 12 seeded active products");
        result.TotalCount.Should().Be(12);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_PaginatesCorrectly()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        var page1 = await svc.GetAllAsync(null, null, null, page: 1, pageSize: 5);
        var page2 = await svc.GetAllAsync(null, null, null, page: 2, pageSize: 5);
        var page3 = await svc.GetAllAsync(null, null, null, page: 3, pageSize: 5);

        page1.Items.Should().HaveCount(5);
        page2.Items.Should().HaveCount(5);
        page3.Items.Should().HaveCount(2, "12 total, pages of 5 → last page has 2");
        page1.TotalCount.Should().Be(12);
    }

    // ── Category filter ──────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_FiltersByCategory()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        // Category 1 = Electronics (3 products seeded)
        var result = await svc.GetAllAsync(null, categoryId: 1, null, page: 1, pageSize: 50);

        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(p => p.CategoryId == 1);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResultForNonexistentCategory()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        var result = await svc.GetAllAsync(null, categoryId: 999, null, page: 1, pageSize: 50);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ── Sorting ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_SortsByPriceAscending()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        var result = await svc.GetAllAsync(null, null, "price_asc", page: 1, pageSize: 50);

        result.Items.Should().BeInAscendingOrder(p => p.Price);
    }

    [Fact]
    public async Task GetAllAsync_SortsByPriceDescending()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        var result = await svc.GetAllAsync(null, null, "price_desc", page: 1, pageSize: 50);

        result.Items.Should().BeInDescendingOrder(p => p.Price);
    }

    [Fact]
    public async Task GetAllAsync_SortsByNameAscending()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        var result = await svc.GetAllAsync(null, null, "name_asc", page: 1, pageSize: 50);

        result.Items.Should().BeInAscendingOrder(p => p.Name);
    }

    // ── Detail (GetByIdAsync) ────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsProductWithMetadata()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());
        var product = await TestDbFactory.GetFirstProductAsync(db);

        var detail = await svc.GetByIdAsync(product.Id);

        detail.Should().NotBeNull();
        detail.Id.Should().Be(product.Id);
        detail.Name.Should().Be(product.Name);
        detail.Slug.Should().Be(product.Slug);
        detail.Metadata.Should().NotBeNullOrEmpty("seeded products have jsonb metadata");
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsForNonexistentProduct()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        var act = () => svc.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsForInactiveProduct()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        // Deactivate the first product
        var product = await TestDbFactory.GetFirstProductAsync(db);
        product.IsActive = false;
        await db.SaveChangesAsync();

        var act = () => svc.GetByIdAsync(product.Id);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404);
    }

    // ── Price seam ───────────────────────────────────────────

    [Fact]
    public void ResolvePrice_ReturnsBasePrice()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Slug = "test",
            Price = 42.50m,
            CategoryId = 1,
        };

        var result = ProductService.ResolvePrice(product);

        result.Should().Be(42.50m);
    }

    [Fact]
    public void GetEffectivePrice_DelegatesToResolvePrice()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Slug = "test",
            Price = 99.99m,
            CategoryId = 1,
        };

        svc.GetEffectivePrice(product).Should().Be(99.99m);
    }

    // ── DTO field mapping ────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_DtoContainsCategoryName()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(db, new PricingService());

        var result = await svc.GetAllAsync(null, categoryId: 1, null, page: 1, pageSize: 1);

        result.Items.First().CategoryName.Should().Be("Electronics");
    }
}
