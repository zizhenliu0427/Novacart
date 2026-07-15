using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Novacart.Api.Clients;
using Novacart.Api.Services.Catalog;
using Refit;

namespace Novacart.Api.Infrastructure;

public static class CatalogClientExtensions
{
    public static IServiceCollection AddNovacartProductCatalogClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Services:product-api:http:0"]
            ?? configuration["ProductApi:BaseUrl"]
            ?? "http://product-api:8080";

        services.AddRefitClient<IProductCatalogApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
            .AddServiceDiscovery();

        return services;
    }

    public static IServiceCollection AddNovacartCatalogSupport(this IServiceCollection services, IConfiguration configuration)
    {
        var micro = configuration.GetSection(MicroservicesOptions.SectionName).Get<MicroservicesOptions>();
        if (micro?.IsolatedDatabases == true && micro.UseRefitCatalog)
        {
            services.AddNovacartProductCatalogClient(configuration);
            services.AddScoped<IProductCatalogStore, RefitProductCatalogStore>();
            return services;
        }

        services.AddScoped<IProductCatalogStore>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var options = config.GetSection(MicroservicesOptions.SectionName).Get<MicroservicesOptions>();
            var productCs = config.GetConnectionString("ProductRead")
                ?? config["ConnectionStrings:ProductDatabase"];
            if (options?.IsolatedDatabases == true && !string.IsNullOrEmpty(productCs))
                return ActivatorUtilities.CreateInstance<IsolatedProductCatalogStore>(sp);
            return ActivatorUtilities.CreateInstance<DbProductCatalogStore>(sp);
        });

        return services;
    }
}
