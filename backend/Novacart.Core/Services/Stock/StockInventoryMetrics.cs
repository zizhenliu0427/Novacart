using System.Diagnostics.Metrics;

namespace Novacart.Api.Services.Stock;

/// <summary>PE-4 observability: lock contention, holds, atomic decrement outcomes.</summary>
public static class StockInventoryMetrics
{
    public const string MeterName = "Novacart.Stock";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> HoldsCreated =
        Meter.CreateCounter<long>("stock.holds.created", description: "Active stock holds created at checkout");

    public static readonly Counter<long> HoldsReleased =
        Meter.CreateCounter<long>("stock.holds.released", description: "Stock holds released (cancel / webhook)");

    public static readonly Counter<long> HoldsExpired =
        Meter.CreateCounter<long>("stock.holds.expired", description: "Stock holds expired by TTL worker");

    public static readonly Counter<long> HoldsConfirmed =
        Meter.CreateCounter<long>("stock.holds.confirmed", description: "Stock holds confirmed after payment");

    public static readonly Counter<long> LockNotAcquired =
        Meter.CreateCounter<long>("stock.lock.not_acquired", description: "Distributed lock acquisition failures");

    public static readonly Histogram<double> LockWaitMs =
        Meter.CreateHistogram<double>("stock.lock.wait_ms", unit: "ms", description: "Time spent waiting for Redis lock");

    public static readonly Counter<long> AtomicDecrementSuccess =
        Meter.CreateCounter<long>("stock.decrement.success", description: "Conditional SQL stock decrements");

    public static readonly Counter<long> AtomicDecrementFailure =
        Meter.CreateCounter<long>("stock.decrement.failure", description: "Conditional SQL stock decrement failures");

    public static readonly Counter<long> ReservationInsufficient =
        Meter.CreateCounter<long>("stock.reservation.insufficient", description: "Insufficient stock at hold or confirm");
}
