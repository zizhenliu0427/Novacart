using Xunit;
using FluentAssertions;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for PricingService — the pure rule-evaluation engine (P2-5).
/// Covers percent/flat/fixed, scope priority, time windows, and clamping.
/// </summary>
public class PricingServiceTests
{
    private readonly PricingService _pricing = new();

    private static Product Product(decimal price, Guid? id = null, int? categoryId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Price = price,
        CategoryId = categoryId,
    };

    [Fact]
    public void Resolve_ReturnsBasePrice_WhenNoRules()
    {
        var result = _pricing.Resolve(Product(100m), Array.Empty<PriceRule>());

        result.OriginalPrice.Should().Be(100m);
        result.EffectivePrice.Should().Be(100m);
        result.HasDiscount.Should().BeFalse();
    }

    [Fact]
    public void Resolve_AppliesPercentRule_Global()
    {
        var rules = new[]
        {
            new PriceRule { RuleType = PriceRuleType.Percent, Value = 20m, IsActive = true },
        };

        var result = _pricing.Resolve(Product(100m), rules);

        result.EffectivePrice.Should().Be(80m);
        result.HasDiscount.Should().BeTrue();
    }

    [Fact]
    public void Resolve_AppliesFlatRule()
    {
        var rules = new[]
        {
            new PriceRule { RuleType = PriceRuleType.Flat, Value = 15m, IsActive = true },
        };

        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(85m);
    }

    [Fact]
    public void Resolve_AppliesFixedRule()
    {
        var rules = new[]
        {
            new PriceRule { RuleType = PriceRuleType.Fixed, Value = 49.99m, IsActive = true },
        };

        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(49.99m);
    }

    [Fact]
    public void Resolve_FlatDiscountClampsAtZero()
    {
        var rules = new[]
        {
            new PriceRule { RuleType = PriceRuleType.Flat, Value = 150m, IsActive = true },
        };

        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(0m);
    }

    [Fact]
    public void Resolve_PercentClampsTo100()
    {
        var rules = new[]
        {
            new PriceRule { RuleType = PriceRuleType.Percent, Value = 150m, IsActive = true },
        };

        // 150% should be clamped to 100%, so price = 0.
        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(0m);
    }

    [Fact]
    public void Resolve_ProductScopeBeatsCategoryAndGlobal()
    {
        var productId = Guid.NewGuid();
        var rules = new[]
        {
            new PriceRule { RuleType = PriceRuleType.Percent, Value = 10m, IsActive = true }, // global
            new PriceRule { CategoryId = 1, RuleType = PriceRuleType.Percent, Value = 20m, IsActive = true },
            new PriceRule { ProductId = productId, RuleType = PriceRuleType.Percent, Value = 30m, IsActive = true },
        };

        _pricing.ResolveEffectivePrice(Product(100m, productId, 1), rules).Should().Be(70m);
    }

    [Fact]
    public void Resolve_CategoryScopeBeatsGlobal()
    {
        var rules = new[]
        {
            new PriceRule { RuleType = PriceRuleType.Percent, Value = 10m, IsActive = true }, // global
            new PriceRule { CategoryId = 1, RuleType = PriceRuleType.Percent, Value = 25m, IsActive = true },
        };

        _pricing.ResolveEffectivePrice(Product(100m, categoryId: 1), rules).Should().Be(75m);
    }

    [Fact]
    public void Resolve_IgnoresInactiveRules()
    {
        var rules = new[]
        {
            new PriceRule { RuleType = PriceRuleType.Percent, Value = 50m, IsActive = false },
        };

        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(100m);
    }

    [Fact]
    public void Resolve_IgnoresExpiredRules()
    {
        var rules = new[]
        {
            new PriceRule
            {
                RuleType = PriceRuleType.Percent,
                Value = 50m,
                IsActive = true,
                EndsAt = DateTime.UtcNow.AddDays(-1),
            },
        };

        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(100m);
    }

    [Fact]
    public void Resolve_IgnoresFutureRules()
    {
        var rules = new[]
        {
            new PriceRule
            {
                RuleType = PriceRuleType.Percent,
                Value = 50m,
                IsActive = true,
                StartsAt = DateTime.UtcNow.AddDays(1),
            },
        };

        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(100m);
    }

    [Fact]
    public void Resolve_RespectsActiveTimeWindow()
    {
        var rules = new[]
        {
            new PriceRule
            {
                RuleType = PriceRuleType.Percent,
                Value = 50m,
                IsActive = true,
                StartsAt = DateTime.UtcNow.AddDays(-1),
                EndsAt = DateTime.UtcNow.AddDays(1),
            },
        };

        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(50m);
    }

    [Fact]
    public void Resolve_ProductScopedRuleDoesNotApplyToOtherProducts()
    {
        var rules = new[]
        {
            new PriceRule { ProductId = Guid.NewGuid(), RuleType = PriceRuleType.Percent, Value = 50m, IsActive = true },
        };

        _pricing.ResolveEffectivePrice(Product(100m), rules).Should().Be(100m);
    }

    [Fact]
    public void Resolve_CategoryScopedRuleOnlyAppliesToMatchingCategory()
    {
        var rules = new[]
        {
            new PriceRule { CategoryId = 5, RuleType = PriceRuleType.Percent, Value = 50m, IsActive = true },
        };

        _pricing.ResolveEffectivePrice(Product(100m, categoryId: 1), rules).Should().Be(100m);
    }
}
