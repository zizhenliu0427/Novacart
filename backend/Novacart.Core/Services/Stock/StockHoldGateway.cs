using Novacart.Api.Models.Dtos.Stock;

namespace Novacart.Api.Services.Stock;

public record StockHoldGatewayResult(bool Success, string? Error = null);

public interface IStockHoldGateway
{
    Task<StockHoldGatewayResult> TryHoldForOrderAsync(
        Guid orderId,
        IReadOnlyList<StockHoldLine> lines,
        CancellationToken cancellationToken = default);

    Task ReleaseForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public class LocalStockHoldGateway(IStockHoldService holds) : IStockHoldGateway
{
    public async Task<StockHoldGatewayResult> TryHoldForOrderAsync(
        Guid orderId,
        IReadOnlyList<StockHoldLine> lines,
        CancellationToken cancellationToken = default)
    {
        var outcome = await holds.TryHoldForOrderAsync(orderId, lines, cancellationToken);
        return outcome switch
        {
            StockHoldOutcome.Held or StockHoldOutcome.AlreadyHeld => new StockHoldGatewayResult(true),
            StockHoldOutcome.InsufficientStock => new StockHoldGatewayResult(false, "Insufficient stock for one or more items."),
            StockHoldOutcome.LockNotAcquired => new StockHoldGatewayResult(false, "Inventory is busy; please retry."),
            _ => new StockHoldGatewayResult(false, "Unable to reserve stock."),
        };
    }

    public Task ReleaseForOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        holds.ReleaseForOrderAsync(orderId, cancellationToken);
}
