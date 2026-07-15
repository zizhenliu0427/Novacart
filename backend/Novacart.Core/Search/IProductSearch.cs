namespace Novacart.Api.Search;

public interface IProductSearchService
{
    bool IsEnabled { get; }

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    Task<ProductSearchResult> SearchAsync(
        ProductSearchQuery query,
        CancellationToken cancellationToken = default);
}

public interface IProductSearchIndexer
{
    bool IsEnabled { get; }

    Task EnsureIndexAsync(CancellationToken cancellationToken = default);

    Task IndexProductAsync(Guid productId, CancellationToken cancellationToken = default);

    Task RemoveProductAsync(Guid productId, CancellationToken cancellationToken = default);

    Task ReindexAllAsync(CancellationToken cancellationToken = default);
}
