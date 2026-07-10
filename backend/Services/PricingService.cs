using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

/// <summary>
/// P2-5 (Dynamic pricing). This is the future home for rule-based pricing. It mirrors the
/// existing <c>ProductService.ResolvePrice</c> seam. See HANDOFF §7 P2-5.
/// </summary>
public interface IPricingService
{
    /// <summary>
    /// Returns the effective price for a product given the active rules that apply to it.
    /// SCAFFOLD: pass-through (returns the base price) until P2-5 implements rule evaluation.
    /// </summary>
    decimal ResolveEffectivePrice(Product product, IReadOnlyCollection<PriceRule> activeRules);
}

/// <summary>
/// SCAFFOLD STUB — currently a safe pass-through so wiring it up never breaks pricing.
/// P2-5 replaces the body: pick the most-specific active, in-window rule and apply
/// percent / flat / fixed. Keep it pure so it stays testable and EF-LINQ-safe.
/// </summary>
public class PricingService : IPricingService
{
    public decimal ResolveEffectivePrice(Product product, IReadOnlyCollection<PriceRule> activeRules)
    {
        // TODO P2-5: evaluate activeRules (most-specific wins, respect StartsAt/EndsAt/IsActive),
        // apply RuleType (Percent/Flat/Fixed), clamp at >= 0, and return the discounted price.
        return product.Price;
    }
}
