using MassTransit;
using Novacart.Api.Messaging;

namespace Novacart.Api.Messaging.Saga;

/// <summary>
/// Checkout orchestration: payment confirmed → await stock → paid + email + clear cart,
/// or cancel on stock failure. Product service still handles <see cref="PaymentCompleted"/> stock work.
/// </summary>
public class OrderCheckoutStateMachine : MassTransitStateMachine<OrderCheckoutState>
{
    public State AwaitingStock { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<PaymentCompleted> PaymentCompleted { get; private set; } = null!;
    public Event<StockReserved> StockReserved { get; private set; } = null!;
    public Event<StockReservationFailed> StockReservationFailed { get; private set; } = null!;

    public OrderCheckoutStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => PaymentCompleted, e =>
        {
            e.CorrelateById(ctx => ctx.Message.OrderId);
            e.SelectId(ctx => ctx.Message.OrderId);
        });

        Event(() => StockReserved, e => e.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReservationFailed, e => e.CorrelateById(ctx => ctx.Message.OrderId));

        Initially(
            When(PaymentCompleted)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.OrderNumber = ctx.Message.OrderNumber;
                })
                .TransitionTo(AwaitingStock));

        During(AwaitingStock,
            Ignore(PaymentCompleted),
            When(StockReserved)
                .Activity(x => x.OfType<MarkOrderPaidActivity>())
                .IfElse(
                    ctx => ctx.Saga.PublishDownstream,
                    paid => paid
                        .Publish(ctx => new OrderPaid(ctx.Saga.OrderId, ctx.Saga.UserId, ctx.Saga.UserEmail!))
                        .Publish(ctx => new ClearCartForOrder(ctx.Saga.OrderId, ctx.Saga.UserId))
                        .TransitionTo(Completed)
                        .Finalize(),
                    skipped => skipped
                        .TransitionTo(Completed)
                        .Finalize()),
            When(StockReservationFailed)
                .Activity(x => x.OfType<CancelOrderAfterStockFailureActivity>())
                .TransitionTo(Failed)
                .Finalize());

        During(Completed,
            Ignore(PaymentCompleted),
            Ignore(StockReserved),
            Ignore(StockReservationFailed));

        During(Failed,
            Ignore(PaymentCompleted),
            Ignore(StockReserved),
            Ignore(StockReservationFailed));

        SetCompletedWhenFinalized();
    }
}
