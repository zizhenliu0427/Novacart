namespace Novacart.Api.Models.Entities;

/// <summary>Idempotent stock reservation record (Product database, Phase 5).</summary>
public class ProcessedStockOrder
{
    public Guid OrderId { get; set; }

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    public string Outcome { get; set; } = string.Empty;
}

public static class StockReservationOutcomes
{
    public const string Reserved = "reserved";
    public const string InsufficientStock = "insufficient";
}
