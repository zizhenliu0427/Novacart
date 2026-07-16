using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Novacart.Api.Infrastructure;
using Novacart.Api.Infrastructure.Threading;

namespace Novacart.Microservice.Hosting;

public static class MicroserviceBootstrap
{
    public static WebApplication BuildAuthService(string[] args) =>
        Build(args, b =>
        {
            b.Services.AddNovacartAuthDatabase(b.Configuration);
            b.Services.AddNovacartAuth();
        });

    public static WebApplication BuildProductService(string[] args) =>
        Build(args, b =>
        {
            b.Services.AddNovacartProductDatabase(b.Configuration);
            b.Services.AddNovacartProduct(b.Configuration);
            MassTransitExtensions.AddNovacartMassTransitForProduct(b.Services, b.Configuration);
        });

    public static WebApplication BuildCartService(string[] args) =>
        Build(args, b =>
        {
            b.Services.AddNovacartCartDatabase(b.Configuration);
            b.Services.AddNovacartCart(b.Configuration);
            MassTransitExtensions.AddNovacartMassTransitForCart(b.Services, b.Configuration);
        });

    public static WebApplication BuildOrderService(string[] args) =>
        Build(args, b =>
        {
            b.Services.AddNovacartCommerceDatabase(b.Configuration);
            b.Services.AddNovacartOrderForMicroservice(b.Configuration);
            MassTransitExtensions.AddNovacartMassTransitForOrder(b.Services, b.Configuration);
        });

    private static WebApplication Build(string[] args, Action<WebApplicationBuilder> configure)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.ConfigureNovacartThreadPool();
        builder.AddServiceDefaults();
        builder.Services.AddNovacartInfrastructure(builder.Configuration);
        configure(builder);
        var app = builder.Build();
        ConfigurePipeline(app);
        app.MapDefaultEndpoints();
        return app;
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseResponseCompression();
        app.UseResponseCaching();
        app.UseCors("AllowFrontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapNovacartHealth();
    }
}
