namespace Novacart.Api.Infrastructure.Sharding;

public class OrderShardingOptions
{
    public const string SectionName = "OrderSharding";

    /// <summary>When false, all order reads/writes use <c>DefaultConnection</c> (Postgres-only).</summary>
    public bool Enabled { get; set; }

    /// <summary>Number of commerce shard databases (pilot: 2).</summary>
    public int ShardCount { get; set; } = 2;

    /// <summary>
    /// Connection string key for the routing table (<c>order_shard_routes</c>).
    /// Defaults to <c>DefaultConnection</c> (legacy <c>novacart_commerce</c>).
    /// </summary>
    public string? RoutingConnection { get; set; }
}
