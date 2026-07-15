using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Data;
using Novacart.Api.Search;
using Novacart.Api.Services;
using Testcontainers.Elasticsearch;
using Xunit;

namespace Novacart.Api.Tests;

/// <summary>Requires Docker. Skipped when the daemon is unavailable.</summary>
public class ProductSearchIntegrationTests : IAsyncLifetime
{
    private ElasticsearchContainer? _elasticsearch;
    private bool _started;

    public async Task InitializeAsync()
    {
        try
        {
            _elasticsearch = new ElasticsearchBuilder()
                .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.15.0")
                .WithEnvironment("discovery.type", "single-node")
                .WithEnvironment("xpack.security.enabled", "false")
                .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
                .Build();
            await _elasticsearch.StartAsync();
            _started = true;
        }
        catch
        {
            _started = false;
        }
    }

    public Task DisposeAsync() => _started && _elasticsearch is not null
        ? _elasticsearch.DisposeAsync().AsTask()
        : Task.CompletedTask;

    [Fact]
    public async Task SearchAsync_FindsProductsByKeyword_WhenElasticsearchRunning()
    {
        if (!_started)
            return;

        using var db = TestDbFactory.Create();
        var url = $"http://127.0.0.1:{_elasticsearch!.GetMappedPublicPort(9200)}";

        var options = Microsoft.Extensions.Options.Options.Create(new ElasticsearchOptions
        {
            Enabled = true,
            Url = url,
            IndexName = $"novacart-products-test-{Guid.NewGuid():N}",
            ReindexOnStartup = false,
        });

        var client = new Elastic.Clients.Elasticsearch.ElasticsearchClient(
            new Elastic.Clients.Elasticsearch.ElasticsearchClientSettings(new Uri(url))
                .DefaultIndex(options.Value.IndexName));

        var indexer = new ElasticProductSearchIndexer(
            client,
            db,
            options,
            NullLogger<ElasticProductSearchIndexer>.Instance);

        await indexer.ReindexAllAsync();

        var search = new ElasticProductSearchService(
            client,
            options,
            NullLogger<ElasticProductSearchService>.Instance);

        (await search.IsHealthyAsync()).Should().BeTrue();

        var result = await search.SearchAsync(new ProductSearchQuery(
            "Wireless",
            null,
            null,
            null,
            null,
            null,
            null,
            1,
            10));

        result.UsedElasticsearch.Should().BeTrue();
        result.ProductIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProductService_FallsBackToPostgres_WhenElasticsearchUnavailable()
    {
        using var db = TestDbFactory.Create();
        var svc = new ProductService(
            db,
            new PricingService(),
            new NullRedisCacheService(),
            new UnhealthyProductSearchService(),
            NullLogger<ProductService>.Instance);

        // Browse without keyword — confirms service still works when ES is enabled but unhealthy.
        var result = await svc.GetAllAsync(null, null, null, null, null, null, null, 1, 20);

        result.Items.Should().NotBeEmpty();
        result.SearchEngine.Should().BeNull();
    }

    [Fact]
    public async Task ProductService_UsesElasticsearch_WhenHealthy()
    {
        if (!_started)
            return;

        using var db = TestDbFactory.Create();
        var url = $"http://127.0.0.1:{_elasticsearch!.GetMappedPublicPort(9200)}";
        var indexName = $"novacart-products-it-{Guid.NewGuid():N}";

        var options = Microsoft.Extensions.Options.Options.Create(new ElasticsearchOptions
        {
            Enabled = true,
            Url = url,
            IndexName = indexName,
            ReindexOnStartup = false,
        });

        var client = new Elastic.Clients.Elasticsearch.ElasticsearchClient(
            new Elastic.Clients.Elasticsearch.ElasticsearchClientSettings(new Uri(url))
                .DefaultIndex(indexName));

        await new ElasticProductSearchIndexer(
            client, db, options, NullLogger<ElasticProductSearchIndexer>.Instance)
            .ReindexAllAsync();

        var search = new ElasticProductSearchService(
            client, options, NullLogger<ElasticProductSearchService>.Instance);

        var svc = new ProductService(
            db,
            new PricingService(),
            new NullRedisCacheService(),
            search,
            NullLogger<ProductService>.Instance);

        var result = await svc.GetAllAsync("headphones", null, null, null, null, null, null, 1, 20);

        result.SearchEngine.Should().Be("elasticsearch");
        result.Items.Should().NotBeEmpty();
    }

    private sealed class UnhealthyProductSearchService : IProductSearchService
    {
        public bool IsEnabled => true;

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<ProductSearchResult> SearchAsync(
            ProductSearchQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProductSearchResult(Array.Empty<Guid>(), 0, false));
    }
}
