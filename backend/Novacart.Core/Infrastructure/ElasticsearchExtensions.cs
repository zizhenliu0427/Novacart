using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Novacart.Api.Search;

public static class ElasticsearchExtensions
{
    public static IServiceCollection AddNovacartElasticsearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));
        var options = configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
                      ?? new ElasticsearchOptions();

        if (!options.Enabled)
        {
            services.AddSingleton<IProductSearchService>(NullProductSearchService.Instance);
            services.AddSingleton<IProductSearchIndexer>(NullProductSearchIndexer.Instance);
            return services;
        }

        services.AddSingleton(_ =>
        {
            var settings = new ElasticsearchClientSettings(new Uri(options.Url))
                .DefaultIndex(options.IndexName)
                .RequestTimeout(TimeSpan.FromSeconds(Math.Max(1, options.RequestTimeoutSeconds)));
            return new ElasticsearchClient(settings);
        });

        services.AddSingleton<IProductSearchService, ElasticProductSearchService>();
        services.AddScoped<IProductSearchIndexer, ElasticProductSearchIndexer>();
        services.AddHostedService<ProductSearchBootstrapHostedService>();
        return services;
    }
}
