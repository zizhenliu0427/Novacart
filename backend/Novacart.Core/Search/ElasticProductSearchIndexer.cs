using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Search;

public sealed class ElasticProductSearchIndexer : IProductSearchIndexer
{
    private readonly ElasticsearchClient _client;
    private readonly AppDbContext _db;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ElasticProductSearchIndexer> _logger;

    public ElasticProductSearchIndexer(
        ElasticsearchClient client,
        AppDbContext db,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticProductSearchIndexer> logger)
    {
        _client = client;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.Enabled;

    public async Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _client.Indices.ExistsAsync(_options.IndexName, cancellationToken);
        if (exists.Exists)
            return;

        var response = await _client.Indices.CreateAsync(_options.IndexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(0))
            .Mappings(m => m.Properties<ProductSearchDocument>(props => props
                .Keyword(k => k.Id)
                .Keyword(k => k.Slug)
                .Text(t => t.Name, td => td
                    .Fields(f => f.Keyword(kw => kw.Suffix("keyword"))))
                .Text(t => t.Description)
                .DoubleNumber(n => n.Price)
                .Keyword(k => k.Currency)
                .IntegerNumber(n => n.StockQuantity)
                .IntegerNumber(n => n.CategoryId)
                .Keyword(k => k.CategoryName)
                .Keyword(k => k.Tags)
                .Text(t => t.MetadataText)
                .Keyword(k => k.ImageUrl)
                .Boolean(b => b.IsActive)
                .Date(d => d.CreatedAt)
                .Date(d => d.UpdatedAt))), cancellationToken);

        if (!response.IsValidResponse)
            throw new InvalidOperationException($"Failed to create Elasticsearch index '{_options.IndexName}': {response.DebugInformation}");
    }

    public async Task IndexProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            await RemoveProductAsync(productId, cancellationToken);
            return;
        }

        if (!product.IsActive)
        {
            await RemoveProductAsync(productId, cancellationToken);
            return;
        }

        var document = ProductSearchDocumentMapper.ToDocument(product, product.Category?.Name);
        var response = await _client.IndexAsync(document, i => i
            .Index(_options.IndexName)
            .Id(productId.ToString()), cancellationToken);

        if (!response.IsValidResponse)
            _logger.LogWarning("Elasticsearch index failed for product {ProductId}: {Debug}", productId, response.DebugInformation);
    }

    public async Task RemoveProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync(
            _options.IndexName,
            Id.From(productId.ToString()),
            cancellationToken);

        if (!response.IsValidResponse && response.Result != Result.NotFound)
            _logger.LogWarning("Elasticsearch delete failed for product {ProductId}: {Debug}", productId, response.DebugInformation);
    }

    public async Task ReindexAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureIndexAsync(cancellationToken);

        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            var document = ProductSearchDocumentMapper.ToDocument(product, product.Category?.Name);
            var response = await _client.IndexAsync(document, i => i
                .Index(_options.IndexName)
                .Id(product.Id.ToString()), cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning(
                    "Elasticsearch index failed during reindex for product {ProductId}: {Debug}",
                    product.Id,
                    response.DebugInformation);
            }
        }

        if (products.Count > 0)
            _logger.LogInformation("Elasticsearch reindexed {Count} active products into {Index}", products.Count, _options.IndexName);
    }
}
