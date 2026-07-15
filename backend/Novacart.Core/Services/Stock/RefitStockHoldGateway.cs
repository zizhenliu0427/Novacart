using Novacart.Api.Clients;
using Novacart.Api.Models.Dtos.Stock;
using Novacart.Api.Services.Stock;

namespace Novacart.Api.Services.Stock;

public class RefitStockHoldGateway(IProductStockApi stockApi) : IStockHoldGateway
{
    public async Task<StockHoldGatewayResult> TryHoldForOrderAsync(
        Guid orderId,
        IReadOnlyList<StockHoldLine> lines,
        CancellationToken cancellationToken = default)
    {
        var response = await stockApi.HoldAsync(new StockHoldRequest(orderId, lines), cancellationToken);
        return new StockHoldGatewayResult(response.Success, response.Error);
    }

    public Task ReleaseForOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        stockApi.ReleaseAsync(new StockReleaseRequest(orderId), cancellationToken);
}
