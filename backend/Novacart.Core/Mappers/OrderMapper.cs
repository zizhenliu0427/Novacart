using Novacart.Api.Models.Dtos.Orders;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Mappers;

/// <summary>
/// P3-3 Mapper layer: static mappers for Order → DTO conversions.
/// Completes the Controller → Service → Mapper → Entity layering (README #13).
/// </summary>
public static class OrderMapper
{
    /// <summary>
    /// Map an <see cref="Order"/> to an <see cref="OrderDto"/> without line items.
    /// Suitable for list views where items aren't loaded.
    /// </summary>
    public static OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id              = order.Id,
            OrderNumber     = order.OrderNumber,
            Subtotal        = order.Subtotal,
            ShippingCost    = order.ShippingCost,
            Tax             = order.Tax,
            Total           = order.Total,
            Currency        = order.Currency,
            CurrentStatus   = order.CurrentStatus,
            ShippingName    = order.ShippingName,
            ShippingLine1   = order.ShippingLine1,
            ShippingLine2   = order.ShippingLine2,
            ShippingCity    = order.ShippingCity,
            ShippingState   = order.ShippingState,
            ShippingPostcode = order.ShippingPostcode,
            ShippingCountry = order.ShippingCountry,
            CreatedAt       = order.CreatedAt,
        };
    }

    /// <summary>
    /// Map an <see cref="Order"/> with its loaded <see cref="OrderItem"/>s
    /// to an <see cref="OrderDto"/> including line-item DTOs.
    /// </summary>
    public static OrderDto ToDtoWithItems(Order order)
    {
        var dto = ToDto(order);
        dto.Items = order.Items.Select(ToItemDto).ToList();
        return dto;
    }

    public static OrderItemDto ToItemDto(OrderItem item)
    {
        return new OrderItemDto
        {
            Id          = item.Id,
            ProductId   = item.ProductId,
            ProductName = item.ProductNameSnapshot,
            ProductSlug = item.Product?.Slug,
            Price       = item.PriceAtPurchase,
            Quantity    = item.Quantity,
        };
    }
}
