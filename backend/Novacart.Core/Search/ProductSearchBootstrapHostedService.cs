using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Novacart.Api.Search;

/// <summary>Ensures the products index exists and optionally reindexes on Product API startup.</summary>
public sealed class ProductSearchBootstrapHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ProductSearchBootstrapHostedService> _logger;

    public ProductSearchBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ElasticsearchOptions> options,
        ILogger<ProductSearchBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_options.ReindexOnStartup)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var indexer = scope.ServiceProvider.GetRequiredService<IProductSearchIndexer>();
            await indexer.EnsureIndexAsync(cancellationToken);
            await indexer.ReindexAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch bootstrap reindex failed — storefront search will fall back to Postgres until ES is healthy.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
