using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Novacart.Api.Services;

/// <summary>
/// Background service that drains the <see cref="IEmailQueue"/> and sends each
/// message via <see cref="IEmailService"/>. Runs in its own DI scope so it resolves
/// a fresh scoped <c>EmailService</c> per iteration.
/// </summary>
public class EmailBackgroundWorker : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailBackgroundWorker> _logger;

    public EmailBackgroundWorker(
        EmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email background worker started.");

        // Create a fake Order from the snapshot fields so we can reuse the existing
        // IEmailService template methods without changing their signatures.
        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var orderSnapshot = new Models.Entities.Order
                {
                    OrderNumber = message.OrderNumber ?? "—",
                    Total = message.OrderTotal ?? 0m,
                };

                switch (message.Kind)
                {
                    case EmailKind.OrderConfirmation:
                        await email.SendOrderConfirmationAsync(message.Recipient, orderSnapshot);
                        break;
                    case EmailKind.OrderStatusUpdate:
                        await email.SendOrderStatusUpdateAsync(message.Recipient, orderSnapshot, message.NewStatus ?? "updated");
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown — stop processing.
                throw;
            }
            catch (Exception ex)
            {
                // A failed send must not kill the worker. The EmailService already
                // falls back to console logging; this guards against unexpected faults.
                _logger.LogError(ex, "Background email send failed for {Kind} to {Recipient}.", message.Kind, message.Recipient);
            }
        }

        _logger.LogInformation("Email background worker stopped.");
    }
}
