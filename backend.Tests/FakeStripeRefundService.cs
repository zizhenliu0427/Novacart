using Novacart.Api.Models.Entities;
using Novacart.Api.Services.Payments;

namespace Novacart.Api.Tests;

public class FakeStripeRefundService : IStripeRefundService
{
    public bool ShouldSucceed { get; set; } = true;
    public int RefundAttempts { get; private set; }
    public Payment? LastPayment { get; private set; }

    public Task<StripeRefundResult> TryRefundAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        RefundAttempts++;
        LastPayment = payment;
        return Task.FromResult(ShouldSucceed
            ? new StripeRefundResult(true, "re_test_refund", null)
            : new StripeRefundResult(false, null, "refund declined"));
    }
}
