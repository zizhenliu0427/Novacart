using MassTransit;
using Microsoft.Extensions.Logging;
using Novacart.Api.Messaging;
using Novacart.Api.Services.Stock;

namespace Novacart.Api.Messaging.Consumers;

/// <summary>Product service: idempotent stock reservation after payment (Redlock + Product DB).</summary>
public class ReserveStockConsumer(
    IStockReservationService stockReservation,
    ILogger<ReserveStockConsumer> logger) : IConsumer<PaymentCompleted>
{
    public async Task Consume(ConsumeContext<PaymentCompleted> context)
    {
        var msg = context.Message;

        var outcome = await stockReservation.TryReserveAsync(msg, context.CancellationToken);

        switch (outcome)
        {
            case StockReservationOutcome.AlreadyProcessed:
                return;
            case StockReservationOutcome.Reserved:
                await context.Publish(new StockReserved(msg.OrderId));
                return;
            case StockReservationOutcome.InsufficientStock:
                await context.Publish(new StockReservationFailed(
                    msg.OrderId,
                    "Insufficient stock at payment completion."));
                return;
            case StockReservationOutcome.LockNotAcquired:
                logger.LogWarning(
                    "ReserveStock: lock contention for order {OrderId}, retrying via MassTransit",
                    msg.OrderId);
                throw new InvalidOperationException(
                    $"Could not acquire stock lock for order {msg.OrderId}.");
        }
    }
}
