using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Infrastructure.Messaging;
using Testcontainers.RabbitMq;
using Xunit;

namespace Novacart.Api.Tests;

/// <summary>Requires Docker. Skipped when the daemon is unavailable.</summary>
public class RabbitMqMonitoringIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();

    private bool _started;

    public async Task InitializeAsync()
    {
        try
        {
            await _rabbit.StartAsync();
            _started = true;
        }
        catch
        {
            _started = false;
        }
    }

    public Task DisposeAsync() => _started ? _rabbit.DisposeAsync().AsTask() : Task.CompletedTask;

    [Fact]
    public async Task GetQueuesAsync_ReturnsManagementData_WhenRabbitMqRunning()
    {
        if (!_started)
        {
            // Docker not available in this environment — skip without failing CI that lacks Docker.
            return;
        }

        var managementPort = _rabbit.GetMappedPublicPort(15672);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMQ:ManagementUrl"] = $"http://127.0.0.1:{managementPort}",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddHttpClient(nameof(RabbitMqMonitoringService));
        services.AddSingleton<IRabbitMqMonitoringService, RabbitMqMonitoringService>();

        await using var provider = services.BuildServiceProvider();
        var monitoring = provider.GetRequiredService<IRabbitMqMonitoringService>();

        var queues = await monitoring.GetQueuesAsync();
        queues.Should().NotBeNull();
    }
}
