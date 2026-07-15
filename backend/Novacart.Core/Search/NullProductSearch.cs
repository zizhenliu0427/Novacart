namespace Novacart.Api.Search;

/// <summary>No-op search when Elasticsearch is disabled.</summary>
public sealed class NullProductSearchService : IProductSearchService
{
    public static readonly NullProductSearchService Instance = new();

    public bool IsEnabled => false;

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<ProductSearchResult> SearchAsync(
        ProductSearchQuery query,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ProductSearchResult(Array.Empty<Guid>(), 0, false));
}

/// <summary>No-op indexer when Elasticsearch is disabled.</summary>
public sealed class NullProductSearchIndexer : IProductSearchIndexer
{
    public static readonly NullProductSearchIndexer Instance = new();

    public bool IsEnabled => false;

    public Task EnsureIndexAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task IndexProductAsync(Guid productId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveProductAsync(Guid productId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ReindexAllAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
