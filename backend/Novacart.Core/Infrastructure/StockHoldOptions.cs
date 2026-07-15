namespace Novacart.Api.Infrastructure;

public class StockHoldOptions
{
    public const string SectionName = "StockHold";

    /// <summary>Hold TTL when checkout / PaymentIntent session is created (minutes).</summary>
    public int TtlMinutes { get; set; } = 15;

    public TimeSpan Ttl => TimeSpan.FromMinutes(TtlMinutes);
}
