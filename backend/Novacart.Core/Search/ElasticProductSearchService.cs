using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Novacart.Api.Search;

public sealed class ElasticProductSearchService : IProductSearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ElasticProductSearchService> _logger;

    public ElasticProductSearchService(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticProductSearchService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.Enabled;

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.PingAsync(cancellationToken);
            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Elasticsearch ping failed");
            return false;
        }
    }

    public async Task<ProductSearchResult> SearchAsync(
        ProductSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var from = Math.Max(0, (query.Page - 1) * query.PageSize);
        var keyword = query.Keyword.Trim();

        var response = await _client.SearchAsync<ProductSearchDocument>(s =>
        {
            s.Index(_options.IndexName);
            s.From(from);
            s.Size(query.PageSize);
            s.Query(q => q.Bool(b =>
            {
                b.Must(m => m.MultiMatch(mm => mm
                    .Query(keyword)
                    .Fields(new[]
                    {
                        "name^3",
                        "description^2",
                        "tags^2",
                        "metadataText",
                        "categoryName",
                    })));

                b.Filter(f => f.Term(t => t
                    .Field(p => p.IsActive)
                    .Value(true)));

                if (query.CategoryIds is { Length: > 0 })
                {
                    var categoryValues = query.CategoryIds
                        .Select(id => FieldValue.Long(id))
                        .ToArray();
                    b.Filter(f => f.Terms(t => t
                        .Field(p => p.CategoryId)
                        .Term(new TermsQueryField(categoryValues))));
                }
                else if (query.CategoryId.HasValue)
                {
                    b.Filter(f => f.Term(t => t
                        .Field(p => p.CategoryId)
                        .Value(query.CategoryId.Value)));
                }

                if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
                {
                    b.Filter(f => f.Range(r => r
                        .NumberRange(nr =>
                        {
                            nr.Field(p => p.Price);
                            if (query.MinPrice.HasValue)
                                nr.Gte((double)query.MinPrice.Value);
                            if (query.MaxPrice.HasValue)
                                nr.Lte((double)query.MaxPrice.Value);
                        })));
                }

                if (!string.IsNullOrWhiteSpace(query.Tag))
                {
                    b.Filter(f => f.Term(t => t
                        .Field(p => p.Tags)
                        .Value(query.Tag.Trim())));
                }
            }));

            switch (query.Sort)
            {
                case "price_asc":
                    s.Sort(so => so.Field(f => f.Price, new FieldSort { Order = SortOrder.Asc }));
                    break;
                case "price_desc":
                    s.Sort(so => so.Field(f => f.Price, new FieldSort { Order = SortOrder.Desc }));
                    break;
                case "name_asc":
                    s.Sort(so => so.Field("name.keyword", new FieldSort { Order = SortOrder.Asc }));
                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(keyword))
                        s.Sort(so => so.Score(new ScoreSort { Order = SortOrder.Desc }));
                    else
                        s.Sort(so => so.Field(f => f.CreatedAt, new FieldSort { Order = SortOrder.Desc }));
                    break;
            }
        }, cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Elasticsearch search failed: {Debug}", response.DebugInformation);
            throw new InvalidOperationException("Elasticsearch search failed.");
        }

        var ids = response.Hits
            .Select(h => Guid.TryParse(h.Id, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var total = GetTotalCount(response, ids.Count);
        return new ProductSearchResult(ids, total, UsedElasticsearch: true);
    }

    private static int GetTotalCount(SearchResponse<ProductSearchDocument> response, int fallback)
    {
        if (response.HitsMetadata?.Total is not { } union)
            return fallback;

        return union.Match(
            static hits => (int)hits.Value,
            static count => (int)count);
    }
}
