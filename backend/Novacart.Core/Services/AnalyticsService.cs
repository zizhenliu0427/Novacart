using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Infrastructure.Sharding;
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
/// Aggregate analytics over paid/processing/shipped/completed orders.
/// When order sharding is enabled, fans out across all commerce shards + legacy DB.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly IShardedOrderDb _shardedDb;

    public AnalyticsService(AppDbContext db)
        : this(db, new SingleDbShardedOrderDb(db))
    {
    }

    public AnalyticsService(AppDbContext db, IShardedOrderDb shardedDb)
    {
        _db = db;
        _shardedDb = shardedDb;
    }

    public async Task<SalesSummaryDto> GetSummaryAsync()
    {
        if (!_shardedDb.Enabled)
            return await BuildSummaryAsync(_db, excludeRoutedDuplicates: false);

        var shardSummaries = await _shardedDb.QueryAllShardContextsAsync(
            db => BuildSummaryAsync(db, excludeRoutedDuplicates: false),
            includeLegacyDefault: false);

        var legacySummary = await BuildSummaryAsync(_db, excludeRoutedDuplicates: true);
        var all = shardSummaries.Append(legacySummary).ToList();

        var totalRevenue = all.Sum(s => s.TotalRevenue);
        var totalOrders = all.Sum(s => s.TotalOrders);
        var totalUnitsSold = all.Sum(s => s.TotalUnitsSold);
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

        return new SalesSummaryDto(totalRevenue, totalOrders, totalUnitsSold, averageOrderValue);
    }

    public async Task<IReadOnlyList<SalesPointDto>> GetSalesOverTimeAsync(int days)
    {
        if (!_shardedDb.Enabled)
            return FillDateGaps(await BuildSalesPointsAsync(_db, days, excludeRoutedDuplicates: false), days);

        var shardPoints = await _shardedDb.QueryAllShardContextsAsync(
            db => BuildSalesPointsAsync(db, days, excludeRoutedDuplicates: false),
            includeLegacyDefault: false);

        var legacyPoints = await BuildSalesPointsAsync(_db, days, excludeRoutedDuplicates: true);

        var merged = shardPoints
            .SelectMany(p => p)
            .Concat(legacyPoints)
            .GroupBy(p => p.Date)
            .Select(g => new SalesPointDto(
                g.Key,
                g.Sum(x => x.Revenue),
                g.Sum(x => x.Orders)))
            .ToList();

        return FillDateGaps(merged, days);
    }

    public async Task<IReadOnlyList<BestSellerDto>> GetBestSellersAsync(int top)
    {
        if (!_shardedDb.Enabled)
            return await BuildBestSellersAsync(_db, top, excludeRoutedDuplicates: false);

        var shardResults = await _shardedDb.QueryAllShardContextsAsync(
            db => BuildBestSellersAsync(db, int.MaxValue, excludeRoutedDuplicates: false),
            includeLegacyDefault: false);

        var legacyResults = await BuildBestSellersAsync(_db, int.MaxValue, excludeRoutedDuplicates: true);

        return shardResults
            .SelectMany(x => x)
            .Concat(legacyResults)
            .GroupBy(b => b.ProductId)
            .Select(g => new BestSellerDto(
                g.Key,
                g.First().Name,
                g.Sum(x => x.UnitsSold),
                g.Sum(x => x.Revenue)))
            .OrderByDescending(x => x.UnitsSold)
            .Take(top)
            .ToList();
    }

    public async Task<IReadOnlyList<LowStockProductDto>> GetLowStockProductsAsync(int threshold = 10)
    {
        return await _db.Products
            .Where(p => p.IsActive && p.StockQuantity <= threshold)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockProductDto(p.Id, p.Name, p.StockQuantity))
            .ToListAsync();
    }

    private static bool IsCountableSale(string status) =>
        status != OrderStatuses.Pending && status != OrderStatuses.Cancelled;

    private static async Task<IQueryable<Order>> FilterCountableOrdersAsync(
        AppDbContext db,
        bool excludeRoutedDuplicates)
    {
        var query = db.Orders.Where(o => IsCountableSale(o.CurrentStatus));
        if (!excludeRoutedDuplicates)
            return query;

        var routedIds = await db.OrderShardRoutes.Select(r => r.OrderId).ToListAsync();
        return routedIds.Count == 0
            ? query
            : query.Where(o => !routedIds.Contains(o.Id));
    }

    private static async Task<SalesSummaryDto> BuildSummaryAsync(AppDbContext db, bool excludeRoutedDuplicates)
    {
        var summaryQuery = await FilterCountableOrdersAsync(db, excludeRoutedDuplicates);

        var totalRevenue = await summaryQuery.SumAsync(o => o.Total);
        var totalOrders = await summaryQuery.CountAsync();

        var itemsQuery = await FilterCountableOrdersAsync(db, excludeRoutedDuplicates);
        var totalUnitsSold = await itemsQuery
            .SelectMany(o => o.Items)
            .SumAsync(oi => oi.Quantity);

        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;
        return new SalesSummaryDto(totalRevenue, totalOrders, totalUnitsSold, averageOrderValue);
    }

    private static async Task<List<SalesPointDto>> BuildSalesPointsAsync(
        AppDbContext db,
        int days,
        bool excludeRoutedDuplicates)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);
        var ordersQuery = await FilterCountableOrdersAsync(db, excludeRoutedDuplicates);

        return await ordersQuery
            .Where(o => o.CreatedAt >= cutoff)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new SalesPointDto(
                DateOnly.FromDateTime(g.Key),
                g.Sum(o => o.Total),
                g.Count()))
            .ToListAsync();
    }

    private static IReadOnlyList<SalesPointDto> FillDateGaps(IReadOnlyList<SalesPointDto> points, int days)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allDates = Enumerable.Range(0, days)
            .Select(i => today.AddDays(-i))
            .OrderBy(d => d)
            .ToList();

        var pointMap = points.ToDictionary(p => p.Date);

        return allDates.Select(d => pointMap.TryGetValue(d, out var val)
            ? val
            : new SalesPointDto(d, 0m, 0)).ToList();
    }

    private static async Task<List<BestSellerDto>> BuildBestSellersAsync(
        AppDbContext db,
        int top,
        bool excludeRoutedDuplicates)
    {
        var ordersQuery = await FilterCountableOrdersAsync(db, excludeRoutedDuplicates);

        var rows = await ordersQuery
            .SelectMany(o => o.Items)
            .GroupBy(oi => new { oi.ProductId, oi.ProductNameSnapshot })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.ProductNameSnapshot,
                UnitsSold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Quantity * oi.PriceAtPurchase),
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(top)
            .ToListAsync();

        return rows
            .Select(b => new BestSellerDto(b.ProductId, b.Name, b.UnitsSold, b.Revenue))
            .ToList();
    }
}
