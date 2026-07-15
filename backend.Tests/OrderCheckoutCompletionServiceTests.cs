using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Novacart.Api.Services.Orders;
using Xunit;

namespace Novacart.Api.Tests;

public class OrderCheckoutCompletionServiceTests
{
    [Fact]
    public async Task TryMarkPaidAsync_MarksOrderPaid_AndReturnsEmail()
    {
        await using var db = TestDbFactory.Create();
        var product = await TestDbFactory.GetFirstProductAsync(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var user = await db.Users.FindAsync(userId);

        var order = await SeedPendingOrderAsync(db, userId, product);
        await SeedPendingPaymentAsync(db, order.Id);

        var svc = new OrderCheckoutCompletionService(
            db,
            new NullRedisCacheService(),
            new FakeStripeRefundService(),
            new FakeStockHoldGateway(),
            NullLogger<OrderCheckoutCompletionService>.Instance);

        var result = await svc.TryMarkPaidAsync(order.Id);

        Assert.NotNull(result);
        Assert.Equal(userId, result!.UserId);
        Assert.Equal(user!.Email, result.Email);

        await db.Entry(order).ReloadAsync();
        Assert.Equal(OrderStatuses.Paid, order.CurrentStatus);

        var payment = await db.Payments.FirstAsync(p => p.OrderId == order.Id);
        Assert.Equal(PaymentStatuses.Succeeded, payment.Status);
    }

    [Fact]
    public async Task TryMarkPaidAsync_IsIdempotent_WhenAlreadyPaid()
    {
        await using var db = TestDbFactory.Create();
        var product = await TestDbFactory.GetFirstProductAsync(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var order = await SeedPendingOrderAsync(db, userId, product);
        order.CurrentStatus = OrderStatuses.Paid;
        await db.SaveChangesAsync();

        var svc = new OrderCheckoutCompletionService(
            db,
            new NullRedisCacheService(),
            new FakeStripeRefundService(),
            new FakeStockHoldGateway(),
            NullLogger<OrderCheckoutCompletionService>.Instance);

        var result = await svc.TryMarkPaidAsync(order.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task CancelAfterStockFailureAsync_CancelsPendingOrder()
    {
        await using var db = TestDbFactory.Create();
        var product = await TestDbFactory.GetFirstProductAsync(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var order = await SeedPendingOrderAsync(db, userId, product);
        await SeedPendingPaymentAsync(db, order.Id);

        var svc = new OrderCheckoutCompletionService(
            db,
            new NullRedisCacheService(),
            new FakeStripeRefundService(),
            new FakeStockHoldGateway(),
            NullLogger<OrderCheckoutCompletionService>.Instance);

        await svc.CancelAfterStockFailureAsync(order.Id, "Insufficient stock");

        await db.Entry(order).ReloadAsync();
        Assert.Equal(OrderStatuses.Cancelled, order.CurrentStatus);

        var payment = await db.Payments.FirstAsync(p => p.OrderId == order.Id);
        Assert.Equal(PaymentStatuses.Failed, payment.Status);
    }

    [Fact]
    public async Task CancelAfterStockFailureAsync_RefundsWhenPaymentAlreadySucceeded()
    {
        await using var db = TestDbFactory.Create();
        var product = await TestDbFactory.GetFirstProductAsync(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var order = await SeedPendingOrderAsync(db, userId, product);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentMethodId = 1,
            ProviderTransactionId = "cs_test_session",
            Amount = 10,
            Currency = "AUD",
            Status = PaymentStatuses.Succeeded,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var refund = new FakeStripeRefundService();
        var svc = new OrderCheckoutCompletionService(
            db,
            new NullRedisCacheService(),
            refund,
            new FakeStockHoldGateway(),
            NullLogger<OrderCheckoutCompletionService>.Instance);

        await svc.CancelAfterStockFailureAsync(order.Id, "Insufficient stock");

        refund.RefundAttempts.Should().Be(1);
        await db.Entry(payment).ReloadAsync();
        payment.Status.Should().Be(PaymentStatuses.Refunded);
    }

    private static async Task<Order> SeedPendingOrderAsync(AppDbContext db, Guid userId, Product product)
    {
        var user = await db.Users.FindAsync(userId);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}"[..12],
            CurrentStatus = OrderStatuses.Pending,
            Subtotal = product.Price,
            Total = product.Price,
            Currency = "AUD",
            ShippingName = "Test",
            ShippingLine1 = "1 Test St",
            ShippingCity = "Sydney",
            ShippingState = "NSW",
            ShippingPostcode = "2000",
            CustomerEmail = user?.Email ?? "test@example.com",
        };
        db.Orders.Add(order);
        db.OrderItems.Add(new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1,
            ProductNameSnapshot = product.Name,
            PriceAtPurchase = product.Price,
        });
        await db.SaveChangesAsync();
        return order;
    }

    private static async Task SeedPendingPaymentAsync(AppDbContext db, Guid orderId)
    {
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PaymentMethodId = 1,
            ProviderTransactionId = "pi_test",
            Amount = 10,
            Currency = "AUD",
            Status = PaymentStatuses.Pending,
        });
        await db.SaveChangesAsync();
    }
}
