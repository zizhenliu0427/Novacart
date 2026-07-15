using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Data;
using Novacart.Api.Messaging;
using Novacart.Api.Messaging.Saga;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services.Orders;
using Xunit;

namespace Novacart.Api.Tests;

public class CheckoutSagaAdminServiceTests
{
    [Fact]
    public async Task ListSagasAsync_ReturnsFailedAndAwaitingStock_ByDefault()
    {
        await using var db = TestDbFactory.Create();
        db.OrderCheckoutStates.AddRange(
            new OrderCheckoutState { CorrelationId = Guid.NewGuid(), OrderId = Guid.NewGuid(), CurrentState = "Failed", OrderNumber = "A" },
            new OrderCheckoutState { CorrelationId = Guid.NewGuid(), OrderId = Guid.NewGuid(), CurrentState = "AwaitingStock", OrderNumber = "B" },
            new OrderCheckoutState { CorrelationId = Guid.NewGuid(), OrderId = Guid.NewGuid(), CurrentState = "Completed", OrderNumber = "C" });
        await db.SaveChangesAsync();

        var svc = new CheckoutSagaAdminService(db, new FakePublishEndpoint(), NullLogger<CheckoutSagaAdminService>.Instance);
        var result = await svc.ListSagasAsync(state: null, limit: 50);

        result.Sagas.Should().HaveCount(2);
        result.Sagas.Select(s => s.CurrentState).Should().BeEquivalentTo(["Failed", "AwaitingStock"]);
    }

    [Fact]
    public async Task RetryCheckoutAsync_PublishesPaymentCompleted_ForPendingAwaitingStock()
    {
        await using var db = TestDbFactory.Create();
        var product = await db.Products.FirstAsync();
        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var orderId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            OrderNumber = "ORD-RETRY-1",
            CurrentStatus = OrderStatuses.Pending,
            Subtotal = 10,
            Total = 10,
            Currency = "AUD",
            CustomerEmail = "retry@test.com",
            ShippingName = "Test",
            ShippingLine1 = "1 St",
            ShippingCity = "Sydney",
            ShippingState = "NSW",
            ShippingPostcode = "2000",
        });
        db.OrderItems.Add(new OrderItem
        {
            OrderId = orderId,
            ProductId = product.Id,
            Quantity = 1,
            ProductNameSnapshot = product.Name,
            PriceAtPurchase = product.Price,
        });
        db.OrderCheckoutStates.Add(new OrderCheckoutState
        {
            CorrelationId = orderId,
            OrderId = orderId,
            UserId = userId,
            CurrentState = "AwaitingStock",
            OrderNumber = "ORD-RETRY-1",
        });
        await db.SaveChangesAsync();

        var publisher = new FakePublishEndpoint();
        var svc = new CheckoutSagaAdminService(db, publisher, NullLogger<CheckoutSagaAdminService>.Instance);

        await svc.RetryCheckoutAsync(orderId);

        publisher.Published.Should().ContainSingle(p => p is PaymentCompleted);
        var evt = (PaymentCompleted)publisher.Published.Single();
        evt.OrderId.Should().Be(orderId);
        evt.Lines.Should().ContainSingle(l => l.ProductId == product.Id);
    }

    private sealed class FakePublishEndpoint : IPublishEndpoint
    {
        public List<object> Published { get; } = [];

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotImplementedException();

        public Task Publish(object message, CancellationToken cancellationToken = default)
        {
            Published.Add(message);
            return Task.CompletedTask;
        }

        public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
            Publish(message, cancellationToken);

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) =>
            Publish(message, cancellationToken);

        public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
            Publish(message, cancellationToken);

        public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            Published.Add(message!);
            return Task.CompletedTask;
        }

        public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class =>
            Publish(message, cancellationToken);

        public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class =>
            Publish(message, cancellationToken);

        public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class =>
            throw new NotImplementedException();

        public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class =>
            throw new NotImplementedException();

        public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class =>
            throw new NotImplementedException();
    }
}
