using Stripe;
using Stripe.Checkout;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Payments;

namespace Novacart.Api.Services.Payments;

public class StripePaymentStrategy : IPaymentStrategy
{
    public string Code => "stripe";

    public async Task<PaymentSessionResult> CreateCheckoutSessionAsync(
        Order order, string successUrl, string cancelUrl)
    {
        var lineItems = new List<SessionLineItemOptions>();

        foreach (var item in order.Items)
        {
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    // Stripe unit amount is in cents
                    UnitAmount = (long)(item.PriceAtPurchase * 100),
                    Currency = order.Currency.ToLowerInvariant(),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductNameSnapshot,
                        Description = item.Product?.Description ?? "E-commerce purchase",
                    }
                },
                Quantity = item.Quantity
            });
        }

        // Handle shipping fee if positive
        if (order.ShippingCost > 0)
        {
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(order.ShippingCost * 100),
                    Currency = order.Currency.ToLowerInvariant(),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Shipping & Handling",
                        Description = "Standard flat-rate shipping"
                    }
                },
                Quantity = 1
            });
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "OrderId", order.Id.ToString() },
                { "UserId", order.UserId.ToString() }
            }
        };

        try
        {
            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return new PaymentSessionResult
            {
                ProviderTransactionId = session.Id,
                RedirectUrl = session.Url
            };
        }
        catch (StripeException ex)
        {
            throw new AppException($"Stripe payment gateway initialization failed: {ex.Message}", StatusCodes.Status502BadGateway);
        }
    }
}
