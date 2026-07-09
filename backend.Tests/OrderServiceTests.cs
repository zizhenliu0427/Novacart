using Xunit;
using FluentAssertions;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for OrderService — covers paginated orders listing and detail fetching with ownership security checks.
/// </summary>
public class OrderServiceTests
{
    [Fact]
    public async Task GetOrdersAsync_ReturnsOnlyUserOrders_PaginatedAndOrderedByNewest()
    {
        using var db = TestDbFactory.Create();
        var svc = new OrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db, "user1@example.com");
        var otherUserId = await TestDbFactory.SeedTestUserAsync(db, "user2@example.com");

        // Seed 3 orders for user1 and 1 order for user2
        var baseDate = DateTime.UtcNow;
        db.Orders.AddRange(new List<Order>
        {
            new Order { Id = Guid.NewGuid(), UserId = userId, OrderNumber = "NC-001", CreatedAt = baseDate.AddMinutes(-10) },
            new Order { Id = Guid.NewGuid(), UserId = userId, OrderNumber = "NC-002", CreatedAt = baseDate.AddMinutes(-5) },
            new Order { Id = Guid.NewGuid(), UserId = userId, OrderNumber = "NC-003", CreatedAt = baseDate },
            new Order { Id = Guid.NewGuid(), UserId = otherUserId, OrderNumber = "NC-004", CreatedAt = baseDate }
        });
        await db.SaveChangesAsync();

        var result = await svc.GetOrdersAsync(userId, page: 1, pageSize: 2);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);

        // Verify ordering: NC-003 is newest, so it should be first
        result.Items[0].OrderNumber.Should().Be("NC-003");
        result.Items[1].OrderNumber.Should().Be("NC-002");
    }

    [Fact]
    public async Task GetOrderByIdAsync_ReturnsCorrectOrderDetails()
    {
        using var db = TestDbFactory.Create();
        var svc = new OrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderNumber = "NC-TEST-001",
            Subtotal = 100m,
            ShippingCost = 10m,
            Tax = 11m,
            Total = 121m,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    ProductNameSnapshot = "Snapshotted Name",
                    PriceAtPurchase = 50m,
                    Quantity = 2
                }
            }
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await svc.GetOrderByIdAsync(userId, order.Id);

        result.Should().NotBeNull();
        result.OrderNumber.Should().Be("NC-TEST-001");
        result.Subtotal.Should().Be(100m);
        result.ShippingCost.Should().Be(10m);
        result.Tax.Should().Be(11m);
        result.Total.Should().Be(121m);

        result.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.ProductId.Should().Be(product.Id);
        item.ProductName.Should().Be("Snapshotted Name");
        item.Price.Should().Be(50m);
        item.Quantity.Should().Be(2);
        item.LineTotal.Should().Be(100m);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ThrowsAppException_WhenOrderDoesNotExist()
    {
        using var db = TestDbFactory.Create();
        var svc = new OrderService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var act = () => svc.GetOrderByIdAsync(userId, Guid.NewGuid());

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ThrowsAppException_WhenUserDoesNotOwnOrder()
    {
        using var db = TestDbFactory.Create();
        var svc = new OrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db, "owner@example.com");
        var maliciousUserId = await TestDbFactory.SeedTestUserAsync(db, "attacker@example.com");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderNumber = "NC-SECURE-001"
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var act = () => svc.GetOrderByIdAsync(maliciousUserId, order.Id);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }
}
