using Novacart.Api.Models.Dtos.Stock;
using Refit;

namespace Novacart.Api.Clients;

public interface IProductStockApi
{
    [Post("/api/internal/stock/hold")]
    Task<StockHoldResponse> HoldAsync([Body] StockHoldRequest request, CancellationToken cancellationToken = default);

    [Post("/api/internal/stock/release")]
    Task ReleaseAsync([Body] StockReleaseRequest request, CancellationToken cancellationToken = default);
}
