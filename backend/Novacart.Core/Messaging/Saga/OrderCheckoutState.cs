using MassTransit;

namespace Novacart.Api.Messaging.Saga;

/// <summary>Persisted checkout saga instance (correlated by <see cref="OrderId"/>).</summary>
public class OrderCheckoutState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string? UserEmail { get; set; }

    /// <summary>Set during <see cref="StockReserved"/> handling when downstream events should fire.</summary>
    public bool PublishDownstream { get; set; }

    public byte[]? RowVersion { get; set; }
}
