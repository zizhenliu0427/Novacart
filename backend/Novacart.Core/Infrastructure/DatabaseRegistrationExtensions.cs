using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Novacart.Api.Data;

namespace Novacart.Api.Infrastructure;

public static class DatabaseRegistrationExtensions
{
    public static IServiceCollection AddNovacartCartDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        RegisterProductReadWhenIsolated(services, configuration);
        return services;
    }

    public static IServiceCollection AddNovacartCommerceDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        RegisterProductReadWhenIsolated(services, configuration);
        return services;
    }

    public static IServiceCollection AddNovacartAuthDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddNovacartProductDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }

    private static void RegisterProductReadWhenIsolated(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var micro = configuration.GetSection(MicroservicesOptions.SectionName).Get<MicroservicesOptions>();
        if (micro?.IsolatedDatabases != true)
            return;

        var productCs = configuration.GetConnectionString("ProductRead")
            ?? configuration["ConnectionStrings:ProductDatabase"];
        if (string.IsNullOrEmpty(productCs))
            return;

        services.AddDbContext<ProductReadDbContext>(options =>
            options.UseNpgsql(productCs));
    }
}
