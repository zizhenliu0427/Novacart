namespace Novacart.Api.Models.Entities;

/// <summary>How a <see cref="PriceRule"/> transforms a product's base price.</summary>
public enum PriceRuleType
{
    /// <summary>Percentage off (Value = 0–100).</summary>
    Percent,
    /// <summary>Flat amount off (Value = currency units).</summary>
    Flat,
    /// <summary>Absolute override price (Value = the new price).</summary>
    Fixed,
}

/// <summary>
/// P2-5 (Dynamic pricing): an admin-configured pricing rule applied at price-read
/// time via the <c>ProductService.ResolvePrice</c> seam. A rule can target a single
/// product, a whole category, or (both null) everything, and may be time-bounded.
/// See HANDOFF §7 P2-5.
/// </summary>
public class PriceRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Target product; null = not product-scoped.</summary>
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Target category; null = not category-scoped.</summary>
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public PriceRuleType RuleType { get; set; } = PriceRuleType.Percent;

    /// <summary>Meaning depends on <see cref="RuleType"/> (percent / flat / fixed).</summary>
    public decimal Value { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
