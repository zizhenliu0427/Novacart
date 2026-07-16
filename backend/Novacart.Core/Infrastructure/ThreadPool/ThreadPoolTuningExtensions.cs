using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Novacart.Api.Services.Payments;

namespace Novacart.Api.Infrastructure.Threading;

public static class ThreadPoolTuningExtensions
{
    /// <summary>Apply CLR min-thread floors as early as host configuration allows.</summary>
    public static IHostApplicationBuilder ConfigureNovacartThreadPool(this IHostApplicationBuilder builder)
    {
        _ = ThreadPoolRuntimeMetrics.MeterName;

        builder.Services.Configure<ThreadPoolTuningOptions>(
            builder.Configuration.GetSection(ThreadPoolTuningOptions.SectionName));

        ThreadPoolTuningApplicator.Apply(builder.Configuration);

        return builder;
    }

    public static IServiceCollection AddNovacartStripeWebhookHotPath(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ThreadPoolTuningOptions>(
            configuration.GetSection(ThreadPoolTuningOptions.SectionName));

        var options = configuration.GetSection(ThreadPoolTuningOptions.SectionName)
            .Get<ThreadPoolTuningOptions>() ?? new ThreadPoolTuningOptions();

        if (!options.Enabled || !options.OffloadStripeWebhooks)
        {
            services.AddSingleton<IStripeWebhookWorkQueue, DisabledStripeWebhookWorkQueue>();
            return services;
        }

        services.AddSingleton<StripeWebhookWorkQueue>();
        services.AddSingleton<IStripeWebhookWorkQueue>(sp => sp.GetRequiredService<StripeWebhookWorkQueue>());
        services.AddHostedService<StripeWebhookBackgroundWorker>();
        return services;
    }
}
