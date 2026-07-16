using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novacart.Api.Infrastructure.Threading;

namespace Novacart.Api.Services.Payments;

/// <summary>Drains <see cref="StripeWebhookWorkQueue"/> on dedicated long-running tasks (PE-8).</summary>
public sealed class StripeWebhookBackgroundWorker : BackgroundService
{
    private readonly StripeWebhookWorkQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StripeWebhookBackgroundWorker> _logger;
    private readonly int _workerCount;

    public StripeWebhookBackgroundWorker(
        StripeWebhookWorkQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<ThreadPoolTuningOptions> options,
        ILogger<StripeWebhookBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerCount = Math.Clamp(options.Value.WebhookWorkerCount, 1, 16);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stripe webhook hot-path worker started with {Count} consumers.", _workerCount);

        var workers = Enumerable.Range(0, _workerCount)
            .Select(i => ConsumeLoopAsync(i, stoppingToken))
            .ToArray();

        await Task.WhenAll(workers);
    }

    private async Task ConsumeLoopAsync(int workerId, CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var payment = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                await payment.ContinuePersistedStripeWebhookAsync(item, stoppingToken);
                _queue.RecordProcessed();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _queue.RecordFailed();
                _logger.LogError(ex, "Stripe webhook continuation failed for {EventId} (worker {WorkerId}).", item.EventId, workerId);
            }
            finally
            {
                _queue.RecordDequeued();
            }
        }
    }
}
