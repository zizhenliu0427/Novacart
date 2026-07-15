namespace Novacart.Api.Models.Dtos.Stock;

public record StockHoldLine(Guid ProductId, int Quantity);

public record StockHoldRequest(Guid OrderId, IReadOnlyList<StockHoldLine> Lines);

public record StockHoldResponse(bool Success, string? Error = null);

public record StockReleaseRequest(Guid OrderId);
