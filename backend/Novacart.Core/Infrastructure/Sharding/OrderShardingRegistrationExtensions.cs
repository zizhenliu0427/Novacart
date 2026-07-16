using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Novacart.Api.Data;

namespace Novacart.Api.Infrastructure.Sharding;

public static class OrderShardingRegistrationExtensions
{
    public static IServiceCollection AddNovacartOrderSharding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OrderShardingOptions>(configuration.GetSection(OrderShardingOptions.SectionName));
        services.AddSingleton<IOrderShardResolver, OrderShardResolver>();
        services.AddSingleton<IOrderDbContextFactory, OrderDbContextFactory>();
        services.AddScoped<IOrderShardRouteStore, OrderShardRouteStore>();
        services.AddScoped<IShardedOrderDb, ShardedOrderDb>();
        return services;
    }
}
