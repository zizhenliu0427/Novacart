using Xunit;
using FluentAssertions;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Pricing;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for PriceRuleService — admin CRUD for pricing rules (P2-5).
/// </summary>
public class PriceRuleServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsRulesOrderedByNewest()
    {
        using var db = TestDbFactory.Create();
        var svc = new PriceRuleService(db);

        await svc.CreateAsync(new CreatePriceRuleRequest
        {
            RuleType = PriceRuleType.Percent,
            Value = 10m,
            IsActive = true,
        });
        await svc.CreateAsync(new CreatePriceRuleRequest
        {
            RuleType = PriceRuleType.Flat,
            Value = 5m,
            IsActive = true,
        });

        var rules = await svc.GetAllAsync();

        rules.Should().HaveCount(2);
        // Newest first
        rules[0].RuleType.Should().Be(PriceRuleType.Flat);
        rules[1].RuleType.Should().Be(PriceRuleType.Percent);
    }

    [Fact]
    public async Task CreateAsync_PersistsGlobalPercentRule()
    {
        using var db = TestDbFactory.Create();
        var svc = new PriceRuleService(db);

        var created = await svc.CreateAsync(new CreatePriceRuleRequest
        {
            RuleType = PriceRuleType.Percent,
            Value = 25m,
            IsActive = true,
        });

        created.Id.Should().NotBeEmpty();
        created.RuleType.Should().Be(PriceRuleType.Percent);
        created.Value.Should().Be(25m);
        db.PriceRules.Should().ContainSingle(r => r.Id == created.Id);
    }

    [Fact]
    public async Task CreateAsync_RejectsBothProductAndCategoryScope()
    {
        using var db = TestDbFactory.Create();
        var product = await TestDbFactory.GetFirstProductAsync(db);
        var svc = new PriceRuleService(db);

        var act = () => svc.CreateAsync(new CreatePriceRuleRequest
        {
            ProductId = product.Id,
            CategoryId = product.CategoryId,
            RuleType = PriceRuleType.Percent,
            Value = 10m,
        });

        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task CreateAsync_RejectsPercentOver100()
    {
        using var db = TestDbFactory.Create();
        var svc = new PriceRuleService(db);

        var act = () => svc.CreateAsync(new CreatePriceRuleRequest
        {
            RuleType = PriceRuleType.Percent,
            Value = 150m,
        });

        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task CreateAsync_ThrowsNotFound_WhenProductMissing()
    {
        using var db = TestDbFactory.Create();
        var svc = new PriceRuleService(db);

        var act = () => svc.CreateAsync(new CreatePriceRuleRequest
        {
            ProductId = Guid.NewGuid(),
            RuleType = PriceRuleType.Percent,
            Value = 10m,
        });

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task CreateAsync_AcceptsProductScopedRule()
    {
        using var db = TestDbFactory.Create();
        var product = await TestDbFactory.GetFirstProductAsync(db);
        var svc = new PriceRuleService(db);

        var created = await svc.CreateAsync(new CreatePriceRuleRequest
        {
            ProductId = product.Id,
            RuleType = PriceRuleType.Flat,
            Value = 20m,
        });

        created.ProductId.Should().Be(product.Id);
        created.ProductName.Should().Be(product.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRule()
    {
        using var db = TestDbFactory.Create();
        var svc = new PriceRuleService(db);

        var created = await svc.CreateAsync(new CreatePriceRuleRequest
        {
            RuleType = PriceRuleType.Percent,
            Value = 10m,
        });

        await svc.DeleteAsync(created.Id);

        (await svc.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFound_WhenRuleMissing()
    {
        using var db = TestDbFactory.Create();
        var svc = new PriceRuleService(db);

        var act = () => svc.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }
}
