using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Novacart.Api.Clients;
using Novacart.Api.Services.Stock;
using Refit;

namespace Novacart.Api.Infrastructure;

public static class StockClientExtensions
{
    public static IServiceCollection AddNovacartProductStockClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Services:product-api:http:0"]
            ?? configuration["ProductApi:BaseUrl"]
            ?? "http://product-api:8080";

        services.AddRefitClient<IProductStockApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
            .AddServiceDiscovery();

        return services;
    }

    public static IServiceCollection AddNovacartStockHoldGateway(this IServiceCollection services, IConfiguration configuration)
    {
        var micro = configuration.GetSection(MicroservicesOptions.SectionName).Get<MicroservicesOptions>();
        if (micro?.IsolatedDatabases == true)
        {
            services.AddNovacartProductStockClient(configuration);
            services.AddScoped<IStockHoldGateway, RefitStockHoldGateway>();
            return services;
        }

        services.AddScoped<IStockHoldGateway, LocalStockHoldGateway>();
        return services;
    }
}
