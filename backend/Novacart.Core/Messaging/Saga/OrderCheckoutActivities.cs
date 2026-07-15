using MassTransit;
using Novacart.Api.Services.Orders;

namespace Novacart.Api.Messaging.Saga;

public class MarkOrderPaidActivity(IOrderCheckoutCompletionService completion)
    : IStateMachineActivity<OrderCheckoutState, StockReserved>
{
    public void Probe(ProbeContext context) => context.CreateScope("mark-order-paid");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderCheckoutState, StockReserved> context,
        IBehavior<OrderCheckoutState, StockReserved> next)
    {
        var result = await completion.TryMarkPaidAsync(context.Saga.OrderId, context.CancellationToken);
        context.Saga.PublishDownstream = result is not null;
        context.Saga.UserEmail = result?.Email;
        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutState, StockReserved, TException> context,
        IBehavior<OrderCheckoutState, StockReserved> next)
        where TException : Exception
        => next.Faulted(context);
}

public class CancelOrderAfterStockFailureActivity(IOrderCheckoutCompletionService completion)
    : IStateMachineActivity<OrderCheckoutState, StockReservationFailed>
{
    public void Probe(ProbeContext context) => context.CreateScope("cancel-order-stock-failure");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(
        BehaviorContext<OrderCheckoutState, StockReservationFailed> context,
        IBehavior<OrderCheckoutState, StockReservationFailed> next)
    {
        await completion.CancelAfterStockFailureAsync(
            context.Saga.OrderId,
            context.Message.Reason,
            context.CancellationToken);
        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutState, StockReservationFailed, TException> context,
        IBehavior<OrderCheckoutState, StockReservationFailed> next)
        where TException : Exception
        => next.Faulted(context);
}
