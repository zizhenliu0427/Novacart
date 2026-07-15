namespace Novacart.Api.Infrastructure;

public class CartRedisOptions
{
    public const string SectionName = "CartRedis";

    public bool Enabled { get; set; } = true;

    /// <summary>Guest session cart TTL (abandoned cart eviction).</summary>
    public int GuestTtlDays { get; set; } = 30;

    /// <summary>Authenticated user cart TTL (refreshed on each write).</summary>
    public int UserTtlDays { get; set; } = 90;
}
