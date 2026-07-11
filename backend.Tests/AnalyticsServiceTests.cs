using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Xunit;

namespace Novacart.Api.Tests;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_AggregatesPaidOrdersCorrectly_AndExcludesPendingOrCancelled()
    {
        using var db = TestDbFactory.Create();
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        // Paid order
        var paidOrder = new Order
        {
            UserId = userId,
            OrderNumber = "NC-PAID-01",
            Subtotal = 100m,
            Total = 110m,
            CurrentStatus = OrderStatuses.Paid,
            Items = new List<OrderItem>
            {
                new() { ProductId = product.Id, ProductNameSnapshot = product.Name, PriceAtPurchase = 100m, Quantity = 2 }
            }
        };

        // Completed order
        var completedOrder = new Order
        {
            UserId = userId,
            OrderNumber = "NC-COMP-01",
            Subtotal = 50m,
            Total = 55m,
            CurrentStatus = OrderStatuses.Completed,
            Items = new List<OrderItem>
            {
                new() { ProductId = product.Id, ProductNameSnapshot = product.Name, PriceAtPurchase = 50m, Quantity = 1 }
            }
        };

        // Pending order (should be excluded)
        var pendingOrder = new Order
        {
            UserId = userId,
            OrderNumber = "NC-PEND-01",
            Subtotal = 200m,
            Total = 220m,
            CurrentStatus = OrderStatuses.Pending,
            Items = new List<OrderItem>
            {
                new() { ProductId = product.Id, ProductNameSnapshot = product.Name, PriceAtPurchase = 200m, Quantity = 5 }
            }
        };

        // Cancelled order (should be excluded)
        var cancelledOrder = new Order
        {
            UserId = userId,
            OrderNumber = "NC-CANC-01",
            Subtotal = 300m,
            Total = 330m,
            CurrentStatus = OrderStatuses.Cancelled,
            Items = new List<OrderItem>
            {
                new() { ProductId = product.Id, ProductNameSnapshot = product.Name, PriceAtPurchase = 300m, Quantity = 10 }
            }
        };

        db.Orders.AddRange(paidOrder, completedOrder, pendingOrder, cancelledOrder);
        await db.SaveChangesAsync();

        var svc = new AnalyticsService(db);
        var result = await svc.GetSummaryAsync();

        // Total Revenue = 110 + 55 = 165
        result.TotalRevenue.Should().Be(165m);
        // Total Orders = 2 (Paid + Completed)
        result.TotalOrders.Should().Be(2);
        // Total Units Sold = 2 + 1 = 3
        result.TotalUnitsSold.Should().Be(3);
        // Average Order Value = 165 / 2 = 82.5
        result.AverageOrderValue.Should().Be(82.5m);
    }

    [Fact]
    public async Task GetSalesOverTimeAsync_FillsGapsInDates_AndGroupsByDayCorrectly()
    {
        using var db = TestDbFactory.Create();
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var today = DateTime.UtcNow;
        var threeDaysAgo = today.AddDays(-3);

        var order1 = new Order
        {
            UserId = userId,
            OrderNumber = "NC-DATE-01",
            Total = 100m,
            CurrentStatus = OrderStatuses.Paid,
            CreatedAt = today
        };

        var order2 = new Order
        {
            UserId = userId,
            OrderNumber = "NC-DATE-02",
            Total = 50m,
            CurrentStatus = OrderStatuses.Completed,
            CreatedAt = threeDaysAgo
        };

        db.Orders.AddRange(order1, order2);
        await db.SaveChangesAsync();

        var svc = new AnalyticsService(db);
        var result = await svc.GetSalesOverTimeAsync(5);

        result.Should().HaveCount(5, "should contain exactly 5 points for a 5-day query");
        
        // Find today's sales
        var todaySales = result.First(r => r.Date == DateOnly.FromDateTime(today));
        todaySales.Revenue.Should().Be(100m);
        todaySales.Orders.Should().Be(1);

        // Find sales 3 days ago
        var threeDaysAgoSales = result.First(r => r.Date == DateOnly.FromDateTime(threeDaysAgo));
        threeDaysAgoSales.Revenue.Should().Be(50m);
        threeDaysAgoSales.Orders.Should().Be(1);

        // Others should be 0
        var emptyDays = result.Where(r => r.Date != DateOnly.FromDateTime(today) && r.Date != DateOnly.FromDateTime(threeDaysAgo));
        emptyDays.Should().OnlyContain(r => r.Revenue == 0m && r.Orders == 0);
    }

    [Fact]
    public async Task GetBestSellersAsync_ReturnsTopSellingProductsByUnits()
    {
        using var db = TestDbFactory.Create();
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        // Get two different seeded products
        var products = await db.Products.Take(2).ToListAsync();
        var prodA = products[0];
        var prodB = products[1];

        // Order 1: 5 units of prodA, 2 units of prodB
        var order1 = new Order
        {
            UserId = userId,
            OrderNumber = "NC-BEST-01",
            CurrentStatus = OrderStatuses.Paid,
            Items = new List<OrderItem>
            {
                new() { ProductId = prodA.Id, ProductNameSnapshot = prodA.Name, PriceAtPurchase = 10m, Quantity = 5 },
                new() { ProductId = prodB.Id, ProductNameSnapshot = prodB.Name, PriceAtPurchase = 20m, Quantity = 2 }
            }
        };

        // Order 2: 1 unit of prodB
        var order2 = new Order
        {
            UserId = userId,
            OrderNumber = "NC-BEST-02",
            CurrentStatus = OrderStatuses.Completed,
            Items = new List<OrderItem>
            {
                new() { ProductId = prodB.Id, ProductNameSnapshot = prodB.Name, PriceAtPurchase = 20m, Quantity = 1 }
            }
        };

        db.Orders.AddRange(order1, order2);
        await db.SaveChangesAsync();

        var svc = new AnalyticsService(db);
        var result = await svc.GetBestSellersAsync(10);

        result.Should().HaveCount(2);

        // prodA units sold = 5, revenue = 50
        // prodB units sold = 3, revenue = 60
        // Since prodA has more units, it should be #1 best seller (ordered by descending units sold)
        result[0].ProductId.Should().Be(prodA.Id);
        result[0].UnitsSold.Should().Be(5);
        result[0].Revenue.Should().Be(50m);

        result[1].ProductId.Should().Be(prodB.Id);
        result[1].UnitsSold.Should().Be(3);
        result[1].Revenue.Should().Be(60m);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsOnlyActiveProducts_UnderThreshold()
    {
        using var db = TestDbFactory.Create();
        
        // Active, stock = 5 (should be returned)
        var p1 = new Product { Name = "Low Stock Active", Slug = "p1", Price = 10m, StockQuantity = 5, IsActive = true };
        // Active, stock = 15 (should be excluded)
        var p2 = new Product { Name = "High Stock Active", Slug = "p2", Price = 10m, StockQuantity = 15, IsActive = true };
        // Inactive, stock = 3 (should be excluded)
        var p3 = new Product { Name = "Low Stock Inactive", Slug = "p3", Price = 10m, StockQuantity = 3, IsActive = false };

        db.Products.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        var svc = new AnalyticsService(db);
        var result = await svc.GetLowStockProductsAsync(10);

        result.Should().ContainSingle();
        result[0].ProductId.Should().Be(p1.Id);
        result[0].Name.Should().Be("Low Stock Active");
        result[0].StockQuantity.Should().Be(5);
    }
}
