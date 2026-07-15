using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novacart.Api.Infrastructure.Messaging;

public record RabbitMqQueueInfo(string Name, int Messages, int MessagesReady, bool IsErrorQueue);

public interface IRabbitMqMonitoringService
{
    Task<IReadOnlyList<RabbitMqQueueInfo>> GetQueuesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RabbitMqQueueInfo>> GetErrorQueuesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Inspects RabbitMQ management API for queue depth (including MassTransit <c>_error</c> queues).</summary>
public class RabbitMqMonitoringService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<RabbitMqMonitoringService> logger) : IRabbitMqMonitoringService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<RabbitMqQueueInfo>> GetQueuesAsync(CancellationToken cancellationToken = default)
    {
        var managementUrl = configuration["RabbitMQ:ManagementUrl"];
        if (string.IsNullOrWhiteSpace(managementUrl))
            return Array.Empty<RabbitMqQueueInfo>();

        try
        {
            var client = httpClientFactory.CreateClient(nameof(RabbitMqMonitoringService));
            var user = configuration["RabbitMQ:Username"] ?? "guest";
            var pass = configuration["RabbitMQ:Password"] ?? "guest";
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await client.GetAsync($"{managementUrl.TrimEnd('/')}/api/queues", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("RabbitMQ management API returned {StatusCode}", response.StatusCode);
                return Array.Empty<RabbitMqQueueInfo>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var raw = JsonSerializer.Deserialize<List<RabbitMqQueueDto>>(json, JsonOptions) ?? [];

            return raw
                .Select(q => new RabbitMqQueueInfo(
                    q.Name ?? string.Empty,
                    q.Messages ?? 0,
                    q.MessagesReady ?? 0,
                    (q.Name ?? string.Empty).Contains("_error", StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(q => q.Messages)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to query RabbitMQ management API");
            return Array.Empty<RabbitMqQueueInfo>();
        }
    }

    public async Task<IReadOnlyList<RabbitMqQueueInfo>> GetErrorQueuesAsync(CancellationToken cancellationToken = default)
    {
        var queues = await GetQueuesAsync(cancellationToken);
        return queues.Where(q => q.IsErrorQueue && q.Messages > 0).ToList();
    }

    private sealed class RabbitMqQueueDto
    {
        public string? Name { get; set; }

        [JsonPropertyName("messages")]
        public int? Messages { get; set; }

        [JsonPropertyName("messages_ready")]
        public int? MessagesReady { get; set; }
    }
}

/// <summary>Logs warnings when MassTransit error queues contain poison messages.</summary>
public class DeadLetterQueueAlertHostedService(
    IRabbitMqMonitoringService monitoring,
    IConfiguration configuration,
    ILogger<DeadLetterQueueAlertHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(configuration["RabbitMQ:ManagementUrl"]))
            return;

        var intervalSeconds = configuration.GetValue("RabbitMQ:DlqPollSeconds", 60);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(15, intervalSeconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var errorQueues = await monitoring.GetErrorQueuesAsync(stoppingToken);
            foreach (var queue in errorQueues)
            {
                logger.LogWarning(
                    "DLQ alert: queue {QueueName} has {MessageCount} message(s) ready for review",
                    queue.Name,
                    queue.Messages);
            }
        }
    }
}

public static class RabbitMqMonitoringExtensions
{
    public static IServiceCollection AddNovacartRabbitMqMonitoring(this IServiceCollection services)
    {
        services.AddHttpClient(nameof(RabbitMqMonitoringService));
        services.AddSingleton<IRabbitMqMonitoringService, RabbitMqMonitoringService>();
        services.AddHostedService<DeadLetterQueueAlertHostedService>();
        return services;
    }
}
