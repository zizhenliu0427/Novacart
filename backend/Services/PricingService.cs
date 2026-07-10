using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IPricingService
{
    /// <summary>
    /// Returns the effective price for a product given the active rules that apply to it.
    /// Picks the most-specific applicable rule (product > category > global) and applies
    /// percent / flat / fixed transformation. Never returns a negative price.
    /// </summary>
    decimal ResolveEffectivePrice(Product product, IReadOnlyCollection<PriceRule> activeRules);

    /// <summary>
    /// Returns the original (base) price and the resolved effective price together, so
    /// callers can show a compare-at / strikethrough price.
    /// </summary>
    ResolvedPrice Resolve(Product product, IReadOnlyCollection<PriceRule> activeRules);
}

/// <summary>Original + effective price pair for display (compare-at support).</summary>
public readonly record struct ResolvedPrice(decimal OriginalPrice, decimal EffectivePrice)
{
    public bool HasDiscount => EffectivePrice < OriginalPrice;
}

/// <summary>
/// P2-5 (Dynamic pricing). Pure rule-evaluation logic — no DB access, no side effects.
/// Kept pure so it stays unit-testable and EF-LINQ-safe (callers materialise first).
/// </summary>
public class PricingService : IPricingService
{
    /// <summary>
    /// Scope specificity used for "most-specific wins" resolution.
    /// A product-scoped rule beats a category-scoped rule beats a global rule.
    /// </summary>
    private const int ProductScopePriority = 0;
    private const int CategoryScopePriority = 1;
    private const int GlobalScopePriority = 2;

    public decimal ResolveEffectivePrice(Product product, IReadOnlyCollection<PriceRule> activeRules)
        => Resolve(product, activeRules).EffectivePrice;

    public ResolvedPrice Resolve(Product product, IReadOnlyCollection<PriceRule> activeRules)
    {
        if (activeRules is null || activeRules.Count == 0)
            return new ResolvedPrice(product.Price, product.Price);

        var now = DateTime.UtcNow;
        var original = product.Price;

        // Filter to rules that actually apply right now, then pick the most specific.
        PriceRule? best = null;
        var bestPriority = int.MaxValue;

        foreach (var rule in activeRules)
        {
            if (!rule.IsActive) continue;
            if (rule.StartsAt.HasValue && now < rule.StartsAt.Value) continue;
            if (rule.EndsAt.HasValue && now > rule.EndsAt.Value) continue;

            // Determine scope + whether it matches this product.
            int priority;
            if (rule.ProductId.HasValue)
            {
                if (rule.ProductId.Value != product.Id) continue;
                priority = ProductScopePriority;
            }
            else if (rule.CategoryId.HasValue)
            {
                if (product.CategoryId != rule.CategoryId.Value) continue;
                priority = CategoryScopePriority;
            }
            else
            {
                priority = GlobalScopePriority;
            }

            if (priority < bestPriority)
            {
                best = rule;
                bestPriority = priority;
            }
        }

        if (best is null)
            return new ResolvedPrice(original, original);

        var effective = ApplyRule(original, best);
        return new ResolvedPrice(original, effective);
    }

    /// <summary>
    /// Apply a single rule's transformation. Clamps at zero so a discount can't
    /// produce a negative price.
    /// </summary>
    private static decimal ApplyRule(decimal basePrice, PriceRule rule)
    {
        var result = rule.RuleType switch
        {
            PriceRuleType.Percent => basePrice * (1m - ClampPercent(rule.Value) / 100m),
            PriceRuleType.Flat => basePrice - rule.Value,
            PriceRuleType.Fixed => rule.Value,
            _ => basePrice,
        };

        return result < 0 ? 0 : result;
    }

    /// <summary>Clamp percentage to 0–100 so a rule can't invert the price sign.</summary>
    private static decimal ClampPercent(decimal value) =>
        value < 0 ? 0 : value > 100 ? 100 : value;
}
