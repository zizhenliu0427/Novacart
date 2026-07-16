using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Novacart.Api.Infrastructure.Threading;

namespace Novacart.Api.Services.Payments;

/// <summary>Stripe webhook continuation after idempotent DB persist (PE-8 hot path).</summary>
public record StripeWebhookWorkItem(string EventId, string EventType, string Json);

public interface IStripeWebhookWorkQueue
{
    bool IsEnabled { get; }

    ValueTask EnqueueAsync(StripeWebhookWorkItem item, CancellationToken cancellationToken = default);
}

public sealed class DisabledStripeWebhookWorkQueue : IStripeWebhookWorkQueue
{
    public static readonly DisabledStripeWebhookWorkQueue Instance = new();

    public bool IsEnabled => false;

    public ValueTask EnqueueAsync(StripeWebhookWorkItem item, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Stripe webhook offload is not enabled.");
}

/// <summary>Bounded channel isolating webhook continuation from ASP.NET request threads.</summary>
public sealed class StripeWebhookWorkQueue : IStripeWebhookWorkQueue
{
    public const string MeterName = "Novacart.Webhook";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Enqueued =
        Meter.CreateCounter<long>("stripe.webhook.enqueued", description: "Webhook continuations queued");
    private static readonly Counter<long> Processed =
        Meter.CreateCounter<long>("stripe.webhook.processed", description: "Webhook continuations completed");
    private static readonly Counter<long> Failed =
        Meter.CreateCounter<long>("stripe.webhook.failed", description: "Webhook continuation failures");

    private readonly Channel<StripeWebhookWorkItem> _channel;
    private int _depth;

    public StripeWebhookWorkQueue(Microsoft.Extensions.Options.IOptions<ThreadPoolTuningOptions> options)
    {
        var capacity = Math.Max(16, options.Value.WebhookQueueCapacity);
        _channel = Channel.CreateBounded<StripeWebhookWorkItem>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });

        Meter.CreateObservableGauge(
            "stripe.webhook.queue_depth",
            () => new Measurement<int>[] { new(Volatile.Read(ref _depth)) },
            description: "In-flight + pending webhook continuations");
    }

    public bool IsEnabled => true;

    public async ValueTask EnqueueAsync(StripeWebhookWorkItem item, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _depth);
        try
        {
            await _channel.Writer.WriteAsync(item, cancellationToken);
            Enqueued.Add(1);
        }
        catch
        {
            Interlocked.Decrement(ref _depth);
            throw;
        }
    }

    public IAsyncEnumerable<StripeWebhookWorkItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    internal void RecordProcessed() => Processed.Add(1);

    internal void RecordFailed() => Failed.Add(1);

    internal void RecordDequeued() => Interlocked.Decrement(ref _depth);
}
