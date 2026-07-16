using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Infrastructure.Sharding;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Orders;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Mappers;

namespace Novacart.Api.Services;

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetOrdersAsync(Guid userId, int page, int pageSize);
    Task<OrderDto> GetOrderByIdAsync(Guid userId, Guid orderId);

    /// <summary>Invalidate all cached orders for a user (call after order creation/status change).</summary>
    Task InvalidateUserOrderCacheAsync(Guid userId);
}

public class OrderService : IOrderService
{
    private readonly IShardedOrderDb _shardedDb;
    private readonly IRedisCacheService _cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public OrderService(AppDbContext db, IRedisCacheService cache)
        : this(new SingleDbShardedOrderDb(db), cache)
    {
    }

    public OrderService(IShardedOrderDb shardedDb, IRedisCacheService cache)
    {
        _shardedDb = shardedDb;
        _cache = cache;
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(Guid userId, int page, int pageSize)
    {
        var cacheKey = $"orders:user:{userId}:p{page}:s{pageSize}";

        var cached = await _cache.GetAsync<PagedResult<OrderDto>>(cacheKey);
        if (cached is not null) return cached;

        var result = await _shardedDb.ExecuteForUserAsync(userId, async db =>
        {
            var query = db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Subtotal = o.Subtotal,
                    ShippingCost = o.ShippingCost,
                    Tax = o.Tax,
                    Total = o.Total,
                    Currency = o.Currency,
                    CurrentStatus = o.CurrentStatus,
                    ShippingName = o.ShippingName,
                    ShippingLine1 = o.ShippingLine1,
                    ShippingLine2 = o.ShippingLine2,
                    ShippingCity = o.ShippingCity,
                    ShippingState = o.ShippingState,
                    ShippingPostcode = o.ShippingPostcode,
                    ShippingCountry = o.ShippingCountry,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<OrderDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        });

        await _cache.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid userId, Guid orderId)
    {
        var cacheKey = $"orders:detail:{orderId}";

        var cached = await _cache.GetAsync<OrderDto>(cacheKey);
        if (cached is not null && cached.Id == orderId)
        {
            var ownerCheck = await _shardedDb.ExecuteForOrderIdAsync(orderId, db =>
                db.Orders.AnyAsync(o => o.Id == orderId && o.UserId == userId));
            if (!ownerCheck) throw AppException.NotFound("Order");
            return cached;
        }

        var dto = await _shardedDb.ExecuteForOrderIdAsync(orderId, async db =>
        {
            var order = await db.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            return order is null ? null : OrderMapper.ToDtoWithItems(order);
        }) ?? throw AppException.NotFound("Order");

        await _cache.SetAsync(cacheKey, dto, CacheTtl);
        return dto;
    }

    public Task InvalidateUserOrderCacheAsync(Guid userId) =>
        _cache.RemoveByPrefixAsync($"orders:user:{userId}:");
}
