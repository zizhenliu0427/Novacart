using Novacart.Api.Data;

namespace Novacart.Api.Services;

// ── DTOs (P2-9) ────────────────────────────────────────────
public record SalesSummaryDto(decimal TotalRevenue, int TotalOrders, int TotalUnitsSold, decimal AverageOrderValue);
public record SalesPointDto(DateOnly Date, decimal Revenue, int Orders);
public record BestSellerDto(Guid ProductId, string Name, int UnitsSold, decimal Revenue);

/// <summary>P2-9 (Analytics dashboard). See HANDOFF §7 P2-9.</summary>
public interface IAnalyticsService
{
    Task<SalesSummaryDto> GetSummaryAsync();
    Task<IReadOnlyList<SalesPointDto>> GetSalesOverTimeAsync(int days);
    Task<IReadOnlyList<BestSellerDto>> GetBestSellersAsync(int top);
}

/// <summary>
/// SCAFFOLD STUB — throws 501 until implemented. Fill in with aggregate queries over
/// paid Orders / OrderItems (SUM(total), COUNT, GROUP BY date, top products by units).
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    public AnalyticsService(AppDbContext db) => _db = db;

    public Task<SalesSummaryDto> GetSummaryAsync() =>
        throw AppException.NotImplemented("P2-9: analytics summary not implemented yet.");

    public Task<IReadOnlyList<SalesPointDto>> GetSalesOverTimeAsync(int days) =>
        throw AppException.NotImplemented("P2-9: sales-over-time not implemented yet.");

    public Task<IReadOnlyList<BestSellerDto>> GetBestSellersAsync(int top) =>
        throw AppException.NotImplemented("P2-9: best-sellers not implemented yet.");
}
