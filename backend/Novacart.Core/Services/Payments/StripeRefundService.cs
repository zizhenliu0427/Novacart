using Microsoft.Extensions.Logging;
using Novacart.Api.Models.Entities;
using Stripe;
using Stripe.Checkout;

namespace Novacart.Api.Services.Payments;

public record StripeRefundResult(bool Success, string? RefundId, string? ErrorMessage);

public interface IStripeRefundService
{
    Task<StripeRefundResult> TryRefundAsync(Payment payment, CancellationToken cancellationToken = default);
}

/// <summary>Issues Stripe refunds for captured Checkout Sessions (saga compensation).</summary>
public class StripeRefundService(ILogger<StripeRefundService> logger) : IStripeRefundService
{
    public async Task<StripeRefundResult> TryRefundAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payment.ProviderTransactionId))
            return new StripeRefundResult(false, null, "Missing Stripe session id.");

        try
        {
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(
                payment.ProviderTransactionId,
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(session.PaymentIntentId))
                return new StripeRefundResult(false, null, "Checkout session has no payment intent.");

            var refundService = new RefundService();
            var refund = await refundService.CreateAsync(
                new RefundCreateOptions { PaymentIntent = session.PaymentIntentId },
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Stripe refund {RefundId} created for order {OrderId}",
                refund.Id,
                payment.OrderId);

            return new StripeRefundResult(true, refund.Id, null);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe refund failed for order {OrderId}", payment.OrderId);
            return new StripeRefundResult(false, null, ex.Message);
        }
    }
}
