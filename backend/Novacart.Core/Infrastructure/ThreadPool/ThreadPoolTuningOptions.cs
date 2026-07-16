namespace Novacart.Api.Infrastructure.Threading;

/// <summary>PE-8: optional CLR thread-pool floor and webhook hot-path offload.</summary>
public class ThreadPoolTuningOptions
{
    public const string SectionName = "ThreadPool";

    /// <summary>When false, min-thread settings and webhook offload are no-ops.</summary>
    public bool Enabled { get; set; }

    /// <summary>Floor for worker threads (0 = leave OS default).</summary>
    public int MinWorkerThreads { get; set; }

    /// <summary>Floor for I/O completion port threads (0 = leave OS default).</summary>
    public int MinCompletionPortThreads { get; set; }

    /// <summary>Return Stripe 200 after idempotent persist; continue on dedicated queue.</summary>
    public bool OffloadStripeWebhooks { get; set; }

    /// <summary>Parallel webhook continuations (each uses its own DI scope).</summary>
    public int WebhookWorkerCount { get; set; } = 2;

    /// <summary>Bounded queue capacity before checkout webhook POST blocks.</summary>
    public int WebhookQueueCapacity { get; set; } = 256;
}
