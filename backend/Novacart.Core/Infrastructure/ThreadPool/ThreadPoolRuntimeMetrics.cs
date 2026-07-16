using System.Diagnostics.Metrics;

namespace Novacart.Api.Infrastructure.Threading;

/// <summary>PE-8 observability: CLR thread-pool pressure (complements dotnet-counters / OTel runtime).</summary>
public static class ThreadPoolRuntimeMetrics
{
    public const string MeterName = "Novacart.Runtime";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    static ThreadPoolRuntimeMetrics()
    {
        Meter.CreateObservableGauge(
            "threadpool.worker.available",
            () => MeasureWorkerAvailable(),
            description: "Available worker threads in the CLR pool");

        Meter.CreateObservableGauge(
            "threadpool.worker.min",
            () => MeasureWorkerMin(),
            description: "Configured minimum worker threads");

        Meter.CreateObservableGauge(
            "threadpool.io.available",
            () => MeasureIoAvailable(),
            description: "Available I/O completion port threads");

        Meter.CreateObservableGauge(
            "threadpool.pending.work",
            () => MeasurePendingWork(),
            description: "Approximate pending work items (ThreadPool.PendingWorkItemCount when available)");
    }

    private static IEnumerable<Measurement<int>> MeasureWorkerAvailable()
    {
        global::System.Threading.ThreadPool.GetAvailableThreads(out var worker, out _);
        return [new Measurement<int>(worker)];
    }

    private static IEnumerable<Measurement<int>> MeasureWorkerMin()
    {
        global::System.Threading.ThreadPool.GetMinThreads(out var worker, out _);
        return [new Measurement<int>(worker)];
    }

    private static IEnumerable<Measurement<int>> MeasureIoAvailable()
    {
        global::System.Threading.ThreadPool.GetAvailableThreads(out _, out var io);
        return [new Measurement<int>(io)];
    }

    private static IEnumerable<Measurement<long>> MeasurePendingWork()
    {
        var pending = global::System.Threading.ThreadPool.PendingWorkItemCount;
        return [new Measurement<long>(pending)];
    }
}
