using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Orders;
using Novacart.Api.Models.Dtos.Products; // for PagedResult

namespace Novacart.Api.Services;

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetOrdersAsync(Guid userId, int page, int pageSize);
    Task<OrderDto> GetOrderByIdAsync(Guid userId, Guid orderId);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db) => _db = db;

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(Guid userId, int page, int pageSize)
    {
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
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid userId, Guid orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId)
            ?? throw AppException.NotFound("Order");

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Subtotal = order.Subtotal,
            ShippingCost = order.ShippingCost,
            Tax = order.Tax,
            Total = order.Total,
            Currency = order.Currency,
            CurrentStatus = order.CurrentStatus,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.ProductNameSnapshot,
                ProductSlug = oi.Product != null ? oi.Product.Slug : null,
                Price = oi.PriceAtPurchase,
                Quantity = oi.Quantity
            }).ToList()
        };
    }
}
