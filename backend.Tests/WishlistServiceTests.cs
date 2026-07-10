using Xunit;
using FluentAssertions;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Novacart.Api.Tests;

/// <summary>Unit tests for WishlistService — P2-3.</summary>
public class WishlistServiceTests
{
    [Fact]
    public async Task AddAsync_AddsItem_AndIsIdempotent()
    {
        using var db = TestDbFactory.Create();
        var svc = new WishlistService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        await svc.AddAsync(userId, product.Id);
        await svc.AddAsync(userId, product.Id); // duplicate — should not throw

        db.WishlistItems.Should().ContainSingle(w => w.UserId == userId && w.ProductId == product.Id);
    }

    [Fact]
    public async Task GetAsync_ReturnsOnlyUsersItems()
    {
        using var db = TestDbFactory.Create();
        var svc = new WishlistService(db);
        var user1 = await TestDbFactory.SeedTestUserAsync(db, "u1@example.com");
        var user2 = await TestDbFactory.SeedTestUserAsync(db, "u2@example.com");
        var product = await TestDbFactory.GetFirstProductAsync(db);

        await svc.AddAsync(user1, product.Id);
        await svc.AddAsync(user2, product.Id);

        var result = await svc.GetAsync(user1);
        result.Should().ContainSingle(w => w.ProductId == product.Id);
    }

    [Fact]
    public async Task RemoveAsync_RemovesItem_AndIsIdempotent()
    {
        using var db = TestDbFactory.Create();
        var svc = new WishlistService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        await svc.AddAsync(userId, product.Id);
        await svc.RemoveAsync(userId, product.Id);
        await svc.RemoveAsync(userId, product.Id); // already removed — no throw

        db.WishlistItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_ThrowsNotFound_WhenProductMissing()
    {
        using var db = TestDbFactory.Create();
        var svc = new WishlistService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var act = () => svc.AddAsync(userId, Guid.NewGuid());

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task GetAsync_ExcludesInactiveProducts()
    {
        using var db = TestDbFactory.Create();
        var svc = new WishlistService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);
        product.IsActive = false;
        await db.SaveChangesAsync();

        // Insert the wishlist row directly (AddAsync correctly rejects inactive products).
        db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = product.Id });
        await db.SaveChangesAsync();

        var result = await svc.GetAsync(userId);
        result.Should().BeEmpty(); // inactive product filtered out
    }
}
