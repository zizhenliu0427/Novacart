using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Pricing;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IPriceRuleService
{
    Task<IReadOnlyList<PriceRuleDto>> GetAllAsync();
    Task<PriceRuleDto> CreateAsync(CreatePriceRuleRequest request);
    Task DeleteAsync(Guid id);
}

/// <summary>P2-5 admin CRUD for pricing rules.</summary>
public sealed class PriceRuleService : IPriceRuleService
{
    private readonly AppDbContext _db;

    public PriceRuleService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PriceRuleDto>> GetAllAsync()
    {
        return await _db.PriceRules
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PriceRuleDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product != null ? r.Product.Name : null,
                CategoryId = r.CategoryId,
                CategoryName = r.Category != null ? r.Category.Name : null,
                RuleType = r.RuleType,
                Value = r.Value,
                StartsAt = r.StartsAt,
                EndsAt = r.EndsAt,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<PriceRuleDto> CreateAsync(CreatePriceRuleRequest request)
    {
        await ValidateAsync(request);

        var rule = new PriceRule
        {
            ProductId = request.ProductId,
            CategoryId = request.CategoryId,
            RuleType = request.RuleType,
            Value = request.Value,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        _db.PriceRules.Add(rule);
        await SaveAsync();

        // Reload navigations for the response DTO.
        if (rule.ProductId.HasValue)
            await _db.Entry(rule).Reference(r => r.Product!).LoadAsync();
        if (rule.CategoryId.HasValue)
            await _db.Entry(rule).Reference(r => r.Category!).LoadAsync();

        return Map(rule);
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule = await _db.PriceRules.FindAsync(id)
            ?? throw AppException.NotFound("Price rule");

        _db.PriceRules.Remove(rule);
        await _db.SaveChangesAsync();
    }

    private async Task ValidateAsync(CreatePriceRuleRequest request)
    {
        // A rule must target at most one scope. Product takes precedence; if both are set
        // we reject rather than silently ignore one.
        if (request.ProductId.HasValue && request.CategoryId.HasValue)
            throw new AppException("A price rule cannot target both a product and a category. Choose one scope.");

        if (request.ProductId.HasValue &&
            !await _db.Products.AnyAsync(p => p.Id == request.ProductId.Value))
            throw AppException.NotFound("Product");

        if (request.CategoryId.HasValue &&
            !await _db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value))
            throw AppException.NotFound("Category");

        // Percent rules are bounded 0–100; flat/fixed just need >= 0 (already enforced by the
        // Range attribute, but double-check percent here so the service is safe to call directly).
        if (request.RuleType == PriceRuleType.Percent && (request.Value < 0 || request.Value > 100))
            throw new AppException("Percentage discount must be between 0 and 100.");

        if (request.StartsAt.HasValue && request.EndsAt.HasValue &&
            request.EndsAt.Value <= request.StartsAt.Value)
            throw new AppException("End date must be after the start date.");
    }

    private async Task SaveAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw AppException.Conflict("Could not save the price rule.");
        }
    }

    private static PriceRuleDto Map(PriceRule r) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        ProductName = r.Product?.Name,
        CategoryId = r.CategoryId,
        CategoryName = r.Category?.Name,
        RuleType = r.RuleType,
        Value = r.Value,
        StartsAt = r.StartsAt,
        EndsAt = r.EndsAt,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt,
    };
}
