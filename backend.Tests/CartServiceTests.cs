using Xunit;
using FluentAssertions;
using Novacart.Api.Data;
using Novacart.Api.Services;
using Novacart.Api.Services.CartRedis;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Cart;
using Microsoft.EntityFrameworkCore;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for CartService — covers full cart CRUD, stock validation,
/// active status validation, quantity updates, and cart clearing.
/// </summary>
public class CartServiceTests
{
    private static CartService CreateService(AppDbContext db) =>
        new(db, new PricingService(), new DbProductCatalogStoreAdapter(db), DisabledCartRedisStore.Instance);

    [Fact]
    public async Task GetCartAsync_ReturnsEmptyCart_WhenCartDoesNotExist()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var cart = await svc.GetCartAsync(userId);

        cart.Should().NotBeNull();
        cart.Items.Should().BeEmpty();
        cart.TotalItems.Should().Be(0);
        cart.Subtotal.Should().Be(0m);
    }

    [Fact]
    public async Task AddItemAsync_AddsNewItem_WhenCartIsEmpty()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        var request = new AddCartItemRequest { ProductId = product.Id, Quantity = 2 };
        var cart = await svc.AddItemAsync(userId, request);

        cart.Should().NotBeNull();
        cart.Items.Should().HaveCount(1);
        cart.TotalItems.Should().Be(2);

        var item = cart.Items.First();
        item.ProductId.Should().Be(product.Id);
        item.Quantity.Should().Be(2);
        item.ProductName.Should().Be(product.Name);
        item.UnitPrice.Should().Be(product.Price);
        item.LineTotal.Should().Be(product.Price * 2);
        cart.Subtotal.Should().Be(product.Price * 2);
    }

    [Fact]
    public async Task AddItemAsync_MergesQuantity_WhenItemAlreadyInCart()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        // Add 1st item
        await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });

        // Add 2nd item of same product
        var cart = await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 2 });

        cart.Items.Should().HaveCount(1);
        cart.TotalItems.Should().Be(3);
        cart.Items.First().Quantity.Should().Be(3);
    }

    [Fact]
    public async Task AddItemAsync_ThrowsAppException_WhenProductDoesNotExist()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var act = () => svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 });

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task AddItemAsync_ThrowsAppException_WhenProductIsInactive()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        product.IsActive = false;
        await db.SaveChangesAsync();

        var act = () => svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 410);
    }

    [Fact]
    public async Task AddItemAsync_ThrowsAppException_WhenExceedsStockQuantity()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        // Seed product stock to 5
        product.StockQuantity = 5;
        await db.SaveChangesAsync();

        // Try adding 6
        var act = () => svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 6 });

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 422);
    }

    [Fact]
    public async Task AddItemAsync_ThrowsAppException_WhenMergedQuantityExceedsStock()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        product.StockQuantity = 5;
        await db.SaveChangesAsync();

        // Add 3
        await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 3 });

        // Add 3 more (total 6, stock 5)
        var act = () => svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 3 });

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 422);
    }

    [Fact]
    public async Task UpdateItemAsync_UpdatesQuantity()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        var cart = await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var cartItemId = cart.Items.First().Id;

        var updatedCart = await svc.UpdateItemAsync(userId, cartItemId, new UpdateCartItemRequest { Quantity = 3 });

        updatedCart.Items.First().Quantity.Should().Be(3);
        updatedCart.TotalItems.Should().Be(3);
    }

    [Fact]
    public async Task UpdateItemAsync_RemovesItem_WhenQuantityIsZero()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        var cart = await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var cartItemId = cart.Items.First().Id;

        var updatedCart = await svc.UpdateItemAsync(userId, cartItemId, new UpdateCartItemRequest { Quantity = 0 });

        updatedCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateItemAsync_ThrowsAppException_WhenQuantityExceedsStock()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        product.StockQuantity = 5;
        await db.SaveChangesAsync();

        var cart = await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var cartItemId = cart.Items.First().Id;

        var act = () => svc.UpdateItemAsync(userId, cartItemId, new UpdateCartItemRequest { Quantity = 6 });

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 422);
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesItemSuccessfully()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        var cart = await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var cartItemId = cart.Items.First().Id;

        var updatedCart = await svc.RemoveItemAsync(userId, cartItemId);

        updatedCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearCartAsync_WipesAllItems()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        
        var products = await db.Products.Take(2).ToListAsync();
        await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = products[0].Id, Quantity = 1 });
        await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = products[1].Id, Quantity = 1 });

        await svc.ClearCartAsync(userId);

        var finalCart = await svc.GetCartAsync(userId);
        finalCart.Items.Should().BeEmpty();
        finalCart.TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task GuestCartOperations_WorkCorrectly()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var sessionId = "guest_session_123";
        var product = await TestDbFactory.GetFirstProductAsync(db);

        // 1. Get empty guest cart
        var cart = await svc.GetCartAsync(sessionId);
        cart.Items.Should().BeEmpty();

        // 2. Add item to guest cart
        cart = await svc.AddItemAsync(sessionId, new AddCartItemRequest { ProductId = product.Id, Quantity = 3 });
        cart.Items.Should().HaveCount(1);
        cart.TotalItems.Should().Be(3);

        // 3. Update item in guest cart
        var cartItemId = cart.Items.First().Id;
        cart = await svc.UpdateItemAsync(sessionId, cartItemId, new UpdateCartItemRequest { Quantity = 5 });
        cart.Items.First().Quantity.Should().Be(5);

        // 4. Remove item from guest cart
        cart = await svc.RemoveItemAsync(sessionId, cartItemId);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task MergeGuestCartAsync_MergesItems_AndWipesGuestCart()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var sessionId = "guest_session_456";

        var products = await db.Products.Take(2).ToListAsync();
        var prodA = products[0];
        var prodB = products[1];

        // Add prodA to user's cart (Qty = 1)
        await svc.AddItemAsync(userId, new AddCartItemRequest { ProductId = prodA.Id, Quantity = 1 });

        // Add prodA (Qty = 2) and prodB (Qty = 4) to guest cart
        await svc.AddItemAsync(sessionId, new AddCartItemRequest { ProductId = prodA.Id, Quantity = 2 });
        await svc.AddItemAsync(sessionId, new AddCartItemRequest { ProductId = prodB.Id, Quantity = 4 });

        // Merge!
        await svc.MergeGuestCartAsync(sessionId, userId);

        // Verify user's merged cart:
        // prodA Qty = 1 + 2 = 3
        // prodB Qty = 4
        var userCart = await svc.GetCartAsync(userId);
        userCart.Items.Should().HaveCount(2);
        userCart.TotalItems.Should().Be(7);

        var itemA = userCart.Items.First(i => i.ProductId == prodA.Id);
        itemA.Quantity.Should().Be(3);

        var itemB = userCart.Items.First(i => i.ProductId == prodB.Id);
        itemB.Quantity.Should().Be(4);

        // Verify guest cart is cleared/deleted
        var guestCart = await db.Carts.FirstOrDefaultAsync(c => c.SessionId == sessionId);
        guestCart.Should().BeNull("guest cart entity should be deleted from DB");
    }
}
