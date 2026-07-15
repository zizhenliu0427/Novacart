namespace Novacart.Api.Models.Entities;

/// <summary>
/// P2-7: audit trail of an order's status transitions. Each row is one move in the
/// state machine (pending → paid → processing → shipped → completed / cancelled).
/// </summary>
public class OrderStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    /// <summary>The status the order moved <em>to</em> in this transition.</summary>
    public string ToStatus { get; set; } = string.Empty;

    /// <summary>The status the order moved <em>from</em> (null for the initial entry).</summary>
    public string? FromStatus { get; set; }

    /// <summary>Who performed the transition (null for system/webhook-driven moves).</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>Optional admin note accompanying the transition.</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
