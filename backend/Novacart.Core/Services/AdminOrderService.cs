using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Novacart.Api.Data;
using Novacart.Api.Infrastructure.Sharding;
using Novacart.Api.Models.Dtos.Orders;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IAdminOrderService
{
    Task<PagedResult<AdminOrderSummaryDto>> GetAllAsync(
        string? q, string? status, int page, int pageSize);
    Task<AdminOrderDto> GetByIdAsync(Guid id);
    Task<AdminOrderDto> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, Guid? actorUserId);
}

/// <summary>P2-7 / P2-8: admin order management + status workflow.</summary>
public sealed class AdminOrderService : IAdminOrderService
{
    private readonly IShardedOrderDb _shardedDb;
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<AdminOrderService> _logger;

    public AdminOrderService(
        AppDbContext db,
        IEmailQueue emailQueue,
        ILogger<AdminOrderService> logger)
        : this(new SingleDbShardedOrderDb(db), emailQueue, logger)
    {
    }

    public AdminOrderService(
        IShardedOrderDb shardedDb,
        IEmailQueue emailQueue,
        ILogger<AdminOrderService> logger)
    {
        _shardedDb = shardedDb;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    public async Task<PagedResult<AdminOrderSummaryDto>> GetAllAsync(
        string? q, string? status, int page, int pageSize)
    {
        if (!_shardedDb.Enabled)
        {
            return await _shardedDb.ExecuteDefaultAsync(async db =>
            {
                var query = BuildFilteredQuery(db, q, status);
                var total = await query.CountAsync();
                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<AdminOrderSummaryDto>
                {
                    Items = items,
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize,
                };
            });
        }

        var shardResults = await _shardedDb.QueryAllShardContextsAsync(
            db => BuildFilteredQuery(db, q, status).ToListAsync(),
            includeLegacyDefault: true);

        var merged = shardResults
            .SelectMany(x => x)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        return PaginateInMemory(merged, page, pageSize);
    }

    public async Task<AdminOrderDto> GetByIdAsync(Guid id)
    {
        var dto = await _shardedDb.ExecuteForOrderIdAsync(id, async db =>
        {
            var order = await db.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order is null ? null : MapDetail(order);
        });

        return dto ?? throw AppException.NotFound("Order");
    }

    public async Task<AdminOrderDto> UpdateStatusAsync(
        Guid id, UpdateOrderStatusRequest request, Guid? actorUserId)
    {
        var dto = await _shardedDb.ExecuteForOrderIdAsync(id, async db =>
        {
            var order = await db.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
                return null;

            var toStatus = NormalizeStatus(request.ToStatus);

            if (!IsTransitionAllowed(order.CurrentStatus, toStatus))
                throw AppException.Unprocessable(
                    $"Cannot transition order from '{order.CurrentStatus}' to '{toStatus}'.");

            var fromStatus = order.CurrentStatus;
            order.CurrentStatus = toStatus;
            order.UpdatedAt = DateTime.UtcNow;

            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ActorUserId = actorUserId,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync();

            try
            {
                var recipient = !string.IsNullOrWhiteSpace(order.CustomerEmail)
                    ? order.CustomerEmail
                    : order.User?.Email;

                if (!string.IsNullOrEmpty(recipient))
                {
                    await _emailQueue.EnqueueAsync(new EmailMessage
                    {
                        Kind = EmailKind.OrderStatusUpdate,
                        Recipient = recipient,
                        OrderNumber = order.OrderNumber,
                        OrderTotal = order.Total,
                        NewStatus = toStatus,
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue order status update email for order {OrderNumber}.", order.OrderNumber);
            }

            return MapDetail(order);
        });

        return dto ?? throw AppException.NotFound("Order");
    }

    private static PagedResult<AdminOrderSummaryDto> PaginateInMemory(
        List<AdminOrderSummaryDto> items,
        int page,
        int pageSize)
    {
        var total = items.Count;
        var pageItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<AdminOrderSummaryDto>
        {
            Items = pageItems,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    private static IQueryable<AdminOrderSummaryDto> BuildFilteredQuery(
        AppDbContext db,
        string? q,
        string? status)
    {
        var query = db.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(term) ||
                o.CustomerEmail.ToLower().Contains(term) ||
                (o.User != null && o.User.Email.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.CurrentStatus == status);

        return query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new AdminOrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserId = o.UserId,
                CustomerEmail = !string.IsNullOrEmpty(o.CustomerEmail)
                    ? o.CustomerEmail
                    : (o.User != null ? o.User.Email : string.Empty),
                Total = o.Total,
                Currency = o.Currency,
                CurrentStatus = o.CurrentStatus,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt,
            });
    }

    private static readonly Dictionary<string, string[]> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        [OrderStatuses.Pending] = new[] { OrderStatuses.Paid, OrderStatuses.Cancelled },
        [OrderStatuses.Paid] = new[] { OrderStatuses.Processing, OrderStatuses.Cancelled },
        [OrderStatuses.Processing] = new[] { OrderStatuses.Shipped },
        [OrderStatuses.Shipped] = new[] { OrderStatuses.Completed },
        [OrderStatuses.Completed] = Array.Empty<string>(),
        [OrderStatuses.Cancelled] = Array.Empty<string>(),
    };

    private static bool IsTransitionAllowed(string from, string to)
    {
        if (!AllowedTransitions.TryGetValue(from, out var targets))
            return false;
        return targets.Contains(to, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeStatus(string raw)
    {
        var normalized = raw?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            OrderStatuses.Pending => OrderStatuses.Pending,
            OrderStatuses.Paid => OrderStatuses.Paid,
            OrderStatuses.Processing => OrderStatuses.Processing,
            OrderStatuses.Shipped => OrderStatuses.Shipped,
            OrderStatuses.Completed => OrderStatuses.Completed,
            OrderStatuses.Cancelled => OrderStatuses.Cancelled,
            _ => throw AppException.Unprocessable(
                $"Unknown order status '{raw}'. Valid: pending, paid, processing, shipped, completed, cancelled."),
        };
    }

    private static AdminOrderDto MapDetail(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        UserId = o.UserId,
        CustomerEmail = !string.IsNullOrWhiteSpace(o.CustomerEmail)
            ? o.CustomerEmail
            : (o.User?.Email ?? string.Empty),
        CustomerName = o.User?.FullName ?? string.Empty,
        Subtotal = o.Subtotal,
        ShippingCost = o.ShippingCost,
        Tax = o.Tax,
        Total = o.Total,
        Currency = o.Currency,
        CurrentStatus = o.CurrentStatus,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        ShippingName = o.ShippingName,
        ShippingLine1 = o.ShippingLine1,
        ShippingLine2 = o.ShippingLine2,
        ShippingCity = o.ShippingCity,
        ShippingState = o.ShippingState,
        ShippingPostcode = o.ShippingPostcode,
        ShippingCountry = o.ShippingCountry,
        Items = o.Items.Select(oi => new OrderItemDto
        {
            Id = oi.Id,
            ProductId = oi.ProductId,
            ProductName = oi.ProductNameSnapshot,
            ProductSlug = oi.Product?.Slug,
            Price = oi.PriceAtPurchase,
            Quantity = oi.Quantity,
        }).ToList(),
    };
}
