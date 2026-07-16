namespace Novacart.Api.Models.Entities;

/// <summary>Maps an order to its commerce shard (PE-7 routing index).</summary>
public class OrderShardRoute
{
    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public int ShardIndex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
