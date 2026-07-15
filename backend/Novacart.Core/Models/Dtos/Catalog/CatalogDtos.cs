using Novacart.Api.Models.Entities;

namespace Novacart.Api.Models.Dtos.Catalog;

public record CatalogProductSnapshot(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    int StockQuantity,
    int? CategoryId,
    bool IsActive);

public record CatalogPricingQuery(
    IReadOnlyList<Guid> ProductIds,
    IReadOnlyList<int> CategoryIds);

public record CatalogPriceRuleSnapshot(
    Guid Id,
    PriceRuleType RuleType,
    decimal Value,
    Guid? ProductId,
    int? CategoryId,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt);
