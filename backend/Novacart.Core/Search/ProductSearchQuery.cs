namespace Novacart.Api.Search;

public sealed record ProductSearchQuery(
    string Keyword,
    int? CategoryId,
    int[]? CategoryIds,
    string? Sort,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Tag,
    int Page,
    int PageSize);

public sealed record ProductSearchResult(
    IReadOnlyList<Guid> ProductIds,
    int TotalCount,
    bool UsedElasticsearch);
