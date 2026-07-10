namespace Novacart.Api.Models.Dtos.Orders;

/// <summary>Admin-facing order DTO — includes customer email and line items.</summary>
public class AdminOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "AUD";
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();
}

/// <summary>Compact order row for the admin list view (no line items).</summary>
public class AdminOrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Currency { get; set; } = "AUD";
    public string CurrentStatus { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Request to advance an order's status.</summary>
public class UpdateOrderStatusRequest
{
    /// <summary>Target status — one of <see cref="OrderStatuses"/>.</summary>
    public string ToStatus { get; set; } = string.Empty;

    /// <summary>Optional admin note for the transition.</summary>
    public string? Notes { get; set; }
}
