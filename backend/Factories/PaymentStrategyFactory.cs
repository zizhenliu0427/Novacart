using Microsoft.AspNetCore.Http;
using Novacart.Api.Services;
using Novacart.Api.Services.Payments;

namespace Novacart.Api.Factories;

/// <summary>
/// Resolves the correct <see cref="IPaymentStrategy"/> for a given gateway code.
/// Satisfies the Factory Pattern requirement (README #13) and prepares
/// the codebase for multi-provider payment support.
/// </summary>
public interface IPaymentStrategyFactory
{
    IPaymentStrategy Create(string providerCode);
}

public class PaymentStrategyFactory : IPaymentStrategyFactory
{
    private readonly IEnumerable<IPaymentStrategy> _strategies;

    public PaymentStrategyFactory(IEnumerable<IPaymentStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IPaymentStrategy Create(string providerCode)
    {
        return _strategies.FirstOrDefault(s =>
                   s.Code.Equals(providerCode, StringComparison.OrdinalIgnoreCase))
               ?? throw new AppException(
                   $"Payment gateway '{providerCode}' is not supported.",
                   StatusCodes.Status400BadRequest);
    }
}
