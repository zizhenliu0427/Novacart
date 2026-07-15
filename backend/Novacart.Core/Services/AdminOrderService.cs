using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Novacart.Api.Data;
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
    private readonly AppDbContext _db;
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<AdminOrderService> _logger;

    public AdminOrderService(
        AppDbContext db,
        IEmailQueue emailQueue,
        ILogger<AdminOrderService> logger)
    {
        _db = db;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    public async Task<PagedResult<AdminOrderSummaryDto>> GetAllAsync(
        string? q, string? status, int page, int pageSize)
    {
        var query = _db.Orders
            .Include(o => o.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(term) ||
                o.User.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.CurrentStatus == status);

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AdminOrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserId = o.UserId,
                CustomerEmail = o.User.Email,
                Total = o.Total,
                Currency = o.Currency,
                CurrentStatus = o.CurrentStatus,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt,
            })
            .ToListAsync();

        return new PagedResult<AdminOrderSummaryDto>
        {
            Items = orders,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AdminOrderDto> GetByIdAsync(Guid id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw AppException.NotFound("Order");

        return MapDetail(order);
    }

    public async Task<AdminOrderDto> UpdateStatusAsync(
        Guid id, UpdateOrderStatusRequest request, Guid? actorUserId)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw AppException.NotFound("Order");

        var toStatus = NormalizeStatus(request.ToStatus);

        if (!IsTransitionAllowed(order.CurrentStatus, toStatus))
            throw AppException.Unprocessable(
                $"Cannot transition order from '{order.CurrentStatus}' to '{toStatus}'.");

        var fromStatus = order.CurrentStatus;
        order.CurrentStatus = toStatus;
        order.UpdatedAt = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync();

        // Queue status update email (non-blocking — sent by the background worker)
        try
        {
            if (order.User != null && !string.IsNullOrEmpty(order.User.Email))
            {
                await _emailQueue.EnqueueAsync(new EmailMessage
                {
                    Kind = EmailKind.OrderStatusUpdate,
                    Recipient = order.User.Email,
                    OrderNumber = order.OrderNumber,
                    OrderTotal = order.Total,
                    NewStatus = toStatus,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to enqueue order status update email for order {order.OrderNumber}.");
        }

        return MapDetail(order);
    }

    // ── Status transition state machine ─────────────────────────

    /// <summary>
    /// Allowed forward transitions. Based on the ER workflow:
    /// pending → paid → processing → shipped → completed, plus cancelled (from pending/paid).
    /// </summary>
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

    /// <summary>Normalize the incoming status string against the known set.</summary>
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
        CustomerEmail = o.User?.Email ?? string.Empty,
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
