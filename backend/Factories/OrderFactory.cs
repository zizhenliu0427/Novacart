using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Factories;

/// <summary>
/// Creates <see cref="Order"/> aggregates (order + line items + address snapshot)
/// from a loaded cart. Extracted from <c>PaymentService</c> to satisfy the
/// Factory Pattern requirement (README #13).
/// </summary>
public interface IOrderFactory
{
    /// <summary>
    /// Build an <see cref="Order"/> with its <see cref="OrderItem"/>s and
    /// shipping-address snapshot ready for persistence.
    /// Accepts active pricing rules so that dynamic discounts are reflected
    /// in <c>PriceAtPurchase</c> and order totals.
    /// </summary>
    Order CreateFromCart(Cart cart, User user, UserAddress address, IReadOnlyCollection<PriceRule> activeRules);
}

public class OrderFactory : IOrderFactory
{
    private readonly IPricingService _pricing;

    public OrderFactory(IPricingService pricing) => _pricing = pricing;

    public Order CreateFromCart(Cart cart, User user, UserAddress address, IReadOnlyCollection<PriceRule> activeRules)
    {
        var subtotal = cart.Items.Sum(ci => _pricing.ResolveEffectivePrice(ci.Product, activeRules) * ci.Quantity);
        var shipping = subtotal >= 100.00m ? 0.00m : 10.00m;
        var tax = Math.Round((subtotal + shipping) * 0.10m, 2);
        var total = subtotal + shipping + tax;

        var orderNumber = $"NC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var orderItems = cart.Items.Select(ci => new OrderItem
        {
            ProductId = ci.ProductId,
            ProductNameSnapshot = ci.Product.Name,
            PriceAtPurchase = _pricing.ResolveEffectivePrice(ci.Product, activeRules),
            Quantity = ci.Quantity
        }).ToList();

        return new Order
        {
            UserId = user.Id,
            OrderNumber = orderNumber,
            Subtotal = subtotal,
            ShippingCost = shipping,
            Tax = tax,
            Total = total,
            Currency = "AUD",
            CurrentStatus = OrderStatuses.Pending,
            ShippingName = user.FullName,
            ShippingLine1 = address.Line1,
            ShippingLine2 = address.Line2,
            ShippingCity = address.City,
            ShippingState = address.State,
            ShippingPostcode = address.Postcode,
            ShippingCountry = address.Country,
            Items = orderItems
        };
    }
}
