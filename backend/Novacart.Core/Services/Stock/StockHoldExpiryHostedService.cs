using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Novacart.Api.Services.Stock;

/// <summary>Releases checkout holds past TTL (PE-4 reservation).</summary>
public class StockHoldExpiryHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<StockHoldExpiryHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var holds = scope.ServiceProvider.GetRequiredService<IStockHoldService>();
                var expired = await holds.ExpireStaleHoldsAsync(stoppingToken);
                if (expired > 0)
                    logger.LogDebug("StockHoldExpiry: expired {Count} holds", expired);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "StockHoldExpiry worker failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
