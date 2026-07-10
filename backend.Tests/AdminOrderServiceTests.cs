using Xunit;
using FluentAssertions;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Orders;
using Microsoft.EntityFrameworkCore;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for AdminOrderService — covers listing, detail, and the
/// order status state machine (legal + illegal transitions).
/// </summary>
public class AdminOrderServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsAllOrders_WithCustomerEmail()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db, "shopper@example.com");
        db.Orders.Add(new Order
        {
            UserId = userId,
            OrderNumber = "NC-LIST-001",
            Total = 99m,
            CurrentStatus = OrderStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync(null, null, 1, 20);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(o => o.OrderNumber == "NC-LIST-001");
        result.Items[0].CustomerEmail.Should().Be("shopper@example.com");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatus_AndSearchesOrderNumber()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db, "user@example.com");
        db.Orders.AddRange(
            new Order { UserId = userId, OrderNumber = "NC-A-001", CurrentStatus = OrderStatuses.Pending },
            new Order { UserId = userId, OrderNumber = "NC-B-002", CurrentStatus = OrderStatuses.Paid },
            new Order { UserId = userId, OrderNumber = "NC-B-003", CurrentStatus = OrderStatuses.Paid });
        await db.SaveChangesAsync();

        var paidOnly = await svc.GetAllAsync(null, OrderStatuses.Paid, 1, 20);
        paidOnly.TotalCount.Should().Be(2);

        var searchResult = await svc.GetAllAsync("NC-B", null, 1, 20);
        searchResult.TotalCount.Should().Be(2);
        searchResult.Items.Should().OnlyContain(o => o.OrderNumber.StartsWith("NC-B"));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsOrderWithItems()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db, "detail@example.com");
        var product = await TestDbFactory.GetFirstProductAsync(db);

        var order = new Order
        {
            UserId = userId,
            OrderNumber = "NC-DETAIL-001",
            Total = 100m,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    ProductNameSnapshot = "Test Product",
                    PriceAtPurchase = 50m,
                    Quantity = 2,
                },
            },
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await svc.GetByIdAsync(order.Id);

        result.OrderNumber.Should().Be("NC-DETAIL-001");
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductName.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFound_WhenOrderMissing()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var act = () => svc.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task UpdateStatusAsync_AdvancesPendingToPaid_AndWritesHistory()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);
        var adminId = Guid.NewGuid();

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var order = new Order
        {
            UserId = userId,
            OrderNumber = "NC-TRANS-001",
            CurrentStatus = OrderStatuses.Pending,
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await svc.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { ToStatus = OrderStatuses.Paid, Notes = "Stripe confirmed" },
            adminId);

        result.CurrentStatus.Should().Be(OrderStatuses.Paid);
        result.UpdatedAt.Should().NotBeNull();

        var history = await db.OrderStatusHistories.Where(h => h.OrderId == order.Id).ToListAsync();
        history.Should().ContainSingle(h =>
            h.FromStatus == OrderStatuses.Pending &&
            h.ToStatus == OrderStatuses.Paid &&
            h.ActorUserId == adminId &&
            h.Notes == "Stripe confirmed");
    }

    [Fact]
    public async Task UpdateStatusAsync_AllowsFullForwardChain_PendingToCompleted()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var order = new Order { UserId = userId, OrderNumber = "NC-CHAIN", CurrentStatus = OrderStatuses.Pending };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Walk the full happy path step by step.
        foreach (var next in new[] { OrderStatuses.Paid, OrderStatuses.Processing, OrderStatuses.Shipped, OrderStatuses.Completed })
        {
            var result = await svc.UpdateStatusAsync(order.Id,
                new UpdateOrderStatusRequest { ToStatus = next }, null);
            result.CurrentStatus.Should().Be(next);
        }

        var history = await db.OrderStatusHistories.Where(h => h.OrderId == order.Id).ToListAsync();
        history.Should().HaveCount(4);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsIllegalTransition()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var order = new Order { UserId = userId, OrderNumber = "NC-BAD", CurrentStatus = OrderStatuses.Pending };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // pending → shipped is not allowed (must go through paid → processing first)
        var act = () => svc.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { ToStatus = OrderStatuses.Shipped }, null);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 422);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsUnknownStatus()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var order = new Order { UserId = userId, OrderNumber = "NC-UNKNOWN", CurrentStatus = OrderStatuses.Pending };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var act = () => svc.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { ToStatus = "delivered" }, null);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 422);
    }

    [Fact]
    public async Task UpdateStatusAsync_AllowsCancellationFromPending()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var order = new Order { UserId = userId, OrderNumber = "NC-CANCEL", CurrentStatus = OrderStatuses.Pending };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await svc.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { ToStatus = OrderStatuses.Cancelled, Notes = "Customer request" }, null);

        result.CurrentStatus.Should().Be(OrderStatuses.Cancelled);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsTransitionFromTerminalStatus()
    {
        using var db = TestDbFactory.Create();
        var svc = new AdminOrderService(db);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var order = new Order { UserId = userId, OrderNumber = "NC-TERM", CurrentStatus = OrderStatuses.Completed };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Completed is terminal — cannot move anywhere
        var act = () => svc.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { ToStatus = OrderStatuses.Shipped }, null);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 422);
    }
}
