using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

// ── DTOs (P2-9) ────────────────────────────────────────────
public record SalesSummaryDto(decimal TotalRevenue, int TotalOrders, int TotalUnitsSold, decimal AverageOrderValue);
public record SalesPointDto(DateOnly Date, decimal Revenue, int Orders);
public record BestSellerDto(Guid ProductId, string Name, int UnitsSold, decimal Revenue);
public record LowStockProductDto(Guid ProductId, string Name, int StockQuantity);

/// <summary>P2-9 (Analytics dashboard). See HANDOFF §7 P2-9.</summary>
public interface IAnalyticsService
{
    Task<SalesSummaryDto> GetSummaryAsync();
    Task<IReadOnlyList<SalesPointDto>> GetSalesOverTimeAsync(int days);
    Task<IReadOnlyList<BestSellerDto>> GetBestSellersAsync(int top);
    Task<IReadOnlyList<LowStockProductDto>> GetLowStockProductsAsync(int threshold = 10);
}

/// <summary>
/// Real implementation of IAnalyticsService fetching aggregate queries over
/// paid/processing/shipped/completed orders. Excludes pending and cancelled.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    public AnalyticsService(AppDbContext db) => _db = db;

    public async Task<SalesSummaryDto> GetSummaryAsync()
    {
        var summaryQuery = _db.Orders
            .Where(o => o.CurrentStatus != OrderStatuses.Pending && o.CurrentStatus != OrderStatuses.Cancelled);

        var totalRevenue = await summaryQuery.SumAsync(o => o.Total);
        var totalOrders = await summaryQuery.CountAsync();
        
        var totalUnitsSold = await _db.Orders
            .Where(o => o.CurrentStatus != OrderStatuses.Pending && o.CurrentStatus != OrderStatuses.Cancelled)
            .SelectMany(o => o.Items)
            .SumAsync(oi => oi.Quantity);

        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

        return new SalesSummaryDto(totalRevenue, totalOrders, totalUnitsSold, averageOrderValue);
    }

    public async Task<IReadOnlyList<SalesPointDto>> GetSalesOverTimeAsync(int days)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);
        var rawPoints = await _db.Orders
            .Where(o => o.CurrentStatus != OrderStatuses.Pending && o.CurrentStatus != OrderStatuses.Cancelled && o.CreatedAt >= cutoff)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.Total),
                Orders = g.Count()
            })
            .OrderBy(p => p.Date)
            .ToListAsync();

        var points = rawPoints.Select(p => new SalesPointDto(
            DateOnly.FromDateTime(p.Date),
            p.Revenue,
            p.Orders
        )).ToList();

        // Fill gaps in dates
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allDates = Enumerable.Range(0, days)
            .Select(i => today.AddDays(-i))
            .OrderBy(d => d)
            .ToList();

        var pointMap = points.ToDictionary(p => p.Date);

        return allDates.Select(d => pointMap.TryGetValue(d, out var val)
            ? val
            : new SalesPointDto(d, 0m, 0)
        ).ToList();
    }

    public async Task<IReadOnlyList<BestSellerDto>> GetBestSellersAsync(int top)
    {
        var bestSellers = await _db.Orders
            .Where(o => o.CurrentStatus != OrderStatuses.Pending && o.CurrentStatus != OrderStatuses.Cancelled)
            .SelectMany(o => o.Items)
            .GroupBy(oi => new { oi.ProductId, oi.ProductNameSnapshot })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.ProductNameSnapshot,
                UnitsSold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Quantity * oi.PriceAtPurchase)
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(top)
            .ToListAsync();

        return bestSellers.Select(b => new BestSellerDto(
            b.ProductId,
            b.Name,
            b.UnitsSold,
            b.Revenue
        )).ToList();
    }

    public async Task<IReadOnlyList<LowStockProductDto>> GetLowStockProductsAsync(int threshold = 10)
    {
        return await _db.Products
            .Where(p => p.IsActive && p.StockQuantity <= threshold)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockProductDto(p.Id, p.Name, p.StockQuantity))
            .ToListAsync();
    }
}
