namespace Novacart.Api.Search;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public bool Enabled { get; set; }

    public string Url { get; set; } = "http://localhost:9200";

    public string IndexName { get; set; } = "novacart-products";

    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>When true, reindex all active products on Product API startup.</summary>
    public bool ReindexOnStartup { get; set; } = true;
}
