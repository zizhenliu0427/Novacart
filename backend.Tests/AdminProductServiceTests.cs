using FluentAssertions;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Services;
using Xunit;

namespace Novacart.Api.Tests;

public class AdminProductServiceTests
{
    [Fact]
    public async Task GetAllAsync_IncludesInactiveProducts_AndSupportsStatusFilter()
    {
        using var db = TestDbFactory.Create();
        var product = await TestDbFactory.GetFirstProductAsync(db);
        product.IsActive = false;
        await db.SaveChangesAsync();
        var service = new AdminProductService(db, new NullRedisCacheService());

        var all = await service.GetAllAsync(null, null, 1, 50);
        var inactive = await service.GetAllAsync(null, false, 1, 50);

        all.TotalCount.Should().Be(12);
        inactive.Items.Should().ContainSingle(p => p.Id == product.Id && !p.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_SearchesNameAndSlugCaseInsensitively()
    {
        using var db = TestDbFactory.Create();
        var service = new AdminProductService(db, new NullRedisCacheService());

        var result = await service.GetAllAsync("ATOMIC-HABITS", null, 1, 20);

        result.Items.Should().ContainSingle(p => p.Slug == "atomic-habits");
    }

    [Fact]
    public async Task CreateAsync_PersistsAndNormalizesProduct()
    {
        using var db = TestDbFactory.Create();
        var service = new AdminProductService(db, new NullRedisCacheService());
        var request = ValidRequest();
        request.Tags = new[] { " New ", "new", "Featured" };

        var created = await service.CreateAsync(request);

        created.Id.Should().NotBeEmpty();
        created.Name.Should().Be("Admin Test Product");
        created.Currency.Should().Be("AUD");
        created.CategoryName.Should().Be("Electronics");
        created.Tags.Should().Equal("new", "featured");
        db.Products.Should().Contain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflict_WhenSlugAlreadyExists()
    {
        using var db = TestDbFactory.Create();
        var service = new AdminProductService(db, new NullRedisCacheService());
        var existing = await TestDbFactory.GetFirstProductAsync(db);
        var request = ValidRequest();
        request.Slug = existing.Slug;

        var act = () => service.CreateAsync(request);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task CreateAsync_ThrowsNotFound_WhenCategoryDoesNotExist()
    {
        using var db = TestDbFactory.Create();
        var service = new AdminProductService(db, new NullRedisCacheService());
        var request = ValidRequest();
        request.CategoryId = 999;

        var act = () => service.CreateAsync(request);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task CreateAsync_RejectsMetadataThatIsNotAJsonObject()
    {
        using var db = TestDbFactory.Create();
        var service = new AdminProductService(db, new NullRedisCacheService());
        var request = ValidRequest();
        request.Metadata = "[1, 2, 3]";

        var act = () => service.CreateAsync(request);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.Message.Contains("JSON object"));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesInventoryCategoryAndActiveStatus()
    {
        using var db = TestDbFactory.Create();
        var service = new AdminProductService(db, new NullRedisCacheService());
        var existing = await TestDbFactory.GetFirstProductAsync(db);
        var request = ValidRequest();
        request.Name = "Updated Product";
        request.Slug = "updated-product";
        request.StockQuantity = 3;
        request.CategoryId = 2;
        request.IsActive = false;

        var updated = await service.UpdateAsync(existing.Id, request);

        updated.Name.Should().Be("Updated Product");
        updated.StockQuantity.Should().Be(3);
        updated.CategoryId.Should().Be(2);
        updated.CategoryName.Should().Be("Apparel");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_SoftDeletesProduct()
    {
        using var db = TestDbFactory.Create();
        var service = new AdminProductService(db, new NullRedisCacheService());
        var product = await TestDbFactory.GetFirstProductAsync(db);

        await service.DeactivateAsync(product.Id);

        product.IsActive.Should().BeFalse();
        (await service.GetByIdAsync(product.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFound_WhenProductDoesNotExist()
    {
        using var db = TestDbFactory.Create();
        var service = new AdminProductService(db, new NullRedisCacheService());

        var act = () => service.UpdateAsync(Guid.NewGuid(), ValidRequest());

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    private static AdminProductUpsertRequest ValidRequest() => new()
    {
        Name = "Admin Test Product",
        Slug = "admin-test-product",
        Description = "Created through the admin catalogue service.",
        Price = 49.95m,
        Currency = "aud",
        StockQuantity = 10,
        CategoryId = 1,
        Tags = new[] { "new" },
        Metadata = "{\"brand\":\"Nova\"}",
        IsActive = true,
    };
}
