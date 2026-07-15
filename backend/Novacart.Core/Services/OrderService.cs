using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Orders;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Mappers; // for PagedResult

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
    private readonly AppDbContext _db;
    private readonly IRedisCacheService _cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public OrderService(AppDbContext db, IRedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(Guid userId, int page, int pageSize)
    {
        var cacheKey = $"orders:user:{userId}:p{page}:s{pageSize}";

        // Try cache first
        var cached = await _cache.GetAsync<PagedResult<OrderDto>>(cacheKey);
        if (cached is not null) return cached;

        var query = _db.Orders
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

        var result = new PagedResult<OrderDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        await _cache.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid userId, Guid orderId)
    {
        var cacheKey = $"orders:detail:{orderId}";

        var cached = await _cache.GetAsync<OrderDto>(cacheKey);
        if (cached is not null && cached.Id == orderId)
        {
            // Verify ownership even on cache hit
            var ownerCheck = await _db.Orders.AnyAsync(o => o.Id == orderId && o.UserId == userId);
            if (!ownerCheck) throw AppException.NotFound("Order");
            return cached;
        }

        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId)
            ?? throw AppException.NotFound("Order");

        var dto = OrderMapper.ToDtoWithItems(order);

        await _cache.SetAsync(cacheKey, dto, CacheTtl);
        return dto;
    }

    public async Task InvalidateUserOrderCacheAsync(Guid userId)
    {
        await _cache.RemoveByPrefixAsync($"orders:user:{userId}:");
    }
}
