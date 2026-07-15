using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Novacart.Api.Data;
using Novacart.Api.Messaging.Consumers;
using Novacart.Api.Messaging.Saga;

namespace Novacart.Api.Infrastructure;

public static class MassTransitExtensions
{
    public static IServiceCollection AddNovacartMassTransitForOrder(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<SendOrderConfirmationConsumer>();
            x.AddConsumer<SendEmailConsumer>();

            x.AddSagaStateMachine<OrderCheckoutStateMachine, OrderCheckoutState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<AppDbContext>();
                    r.UsePostgres();
                });

            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(GetRabbitConnection(configuration)));
                ApplyEndpointDefaults(cfg);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    public static IServiceCollection AddNovacartMassTransitForProduct(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ReserveStockConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(GetRabbitConnection(configuration)));
                ApplyEndpointDefaults(cfg);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    public static IServiceCollection AddNovacartMassTransitForCart(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ClearCartConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(GetRabbitConnection(configuration)));
                ApplyEndpointDefaults(cfg);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static void ApplyEndpointDefaults(IRabbitMqBusFactoryConfigurator cfg)
    {
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
    }

    private static string GetRabbitConnection(IConfiguration configuration) =>
        configuration.GetConnectionString("RabbitMQ")
        ?? configuration["RabbitMQ:Host"]
        ?? "amqp://guest:guest@localhost:5672";
}
