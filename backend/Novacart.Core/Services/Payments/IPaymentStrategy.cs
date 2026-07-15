using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Payments;

namespace Novacart.Api.Services.Payments;

public interface IPaymentStrategy
{
    string Code { get; }

    Task<PaymentSessionResult> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl);
}
