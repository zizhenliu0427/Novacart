using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Novacart.Api.Messaging;
using Novacart.Api.Messaging.Saga;
using Novacart.Api.Services.Orders;
using Xunit;

namespace Novacart.Api.Tests;

/// <summary>MassTransit in-memory harness for the checkout saga state machine.</summary>
public class CheckoutSagaIntegrationTests
{
    [Fact]
    public async Task PaymentCompleted_ThenStockReserved_FinalizesSaga()
    {
        var completion = new RecordingCheckoutCompletionService { MarkPaidResult = new OrderCheckoutCompletionResult(Guid.NewGuid(), "buyer@test.com") };

        await using var provider = BuildProvider(completion);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await harness.Bus.Publish(new PaymentCompleted(
            orderId,
            "NC-1001",
            userId,
            "evt_test",
            "buyer@test.com",
            []));

        (await harness.Published.Any<PaymentCompleted>()).Should().BeTrue();

        var sagaHarness = provider.GetRequiredService<ISagaStateMachineTestHarness<OrderCheckoutStateMachine, OrderCheckoutState>>();
        (await sagaHarness.Consumed.Any<PaymentCompleted>()).Should().BeTrue();

        await harness.Bus.Publish(new StockReserved(orderId));

        (await sagaHarness.Consumed.Any<StockReserved>()).Should().BeTrue();
        completion.MarkPaidCalls.Should().Be(1);
    }

    [Fact]
    public async Task PaymentCompleted_ThenStockFailure_CancelsOrder()
    {
        var completion = new RecordingCheckoutCompletionService();

        await using var provider = BuildProvider(completion);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();

        await harness.Bus.Publish(new PaymentCompleted(
            orderId,
            "NC-1002",
            Guid.NewGuid(),
            "evt_test_2",
            "buyer@test.com",
            []));

        await harness.Bus.Publish(new StockReservationFailed(orderId, "Insufficient stock"));

        var sagaHarness = provider.GetRequiredService<ISagaStateMachineTestHarness<OrderCheckoutStateMachine, OrderCheckoutState>>();
        (await sagaHarness.Consumed.Any<StockReservationFailed>()).Should().BeTrue();
        completion.CancelCalls.Should().Be(1);
        completion.LastCancelReason.Should().Be("Insufficient stock");
    }

    private static ServiceProvider BuildProvider(RecordingCheckoutCompletionService completion)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddScoped<IOrderCheckoutCompletionService>(_ => completion);
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddSagaStateMachine<OrderCheckoutStateMachine, OrderCheckoutState>()
                .InMemoryRepository();
        });

        return services.BuildServiceProvider(true);
    }

    private sealed class RecordingCheckoutCompletionService : IOrderCheckoutCompletionService
    {
        public int MarkPaidCalls { get; private set; }
        public int CancelCalls { get; private set; }
        public string? LastCancelReason { get; private set; }
        public OrderCheckoutCompletionResult? MarkPaidResult { get; set; }

        public Task<OrderCheckoutCompletionResult?> TryMarkPaidAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            MarkPaidCalls++;
            return Task.FromResult(MarkPaidResult);
        }

        public Task CancelAfterStockFailureAsync(Guid orderId, string reason, CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            LastCancelReason = reason;
            return Task.CompletedTask;
        }
    }
}
