using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Messaging;
using Novacart.Api.Messaging.Consumers;
using Novacart.Api.Services;
using Xunit;

namespace Novacart.Api.Tests;

public class MassTransitEmailQueueTests
{
    [Fact]
    public async Task EnqueueAsync_PublishesSendEmailRequested()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddConsumer<SendEmailConsumer>();
        });
        services.AddSingleton<IEmailService, FakeEmailService>();
        services.AddScoped<IEmailQueue, MassTransitEmailQueue>();

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var queue = provider.GetRequiredService<IEmailQueue>();
        await queue.EnqueueAsync(new EmailMessage
        {
            Kind = EmailKind.OrderStatusUpdate,
            Recipient = "buyer@example.com",
            OrderNumber = "NC-1001",
            OrderTotal = 42.50m,
            NewStatus = "shipped",
        });

        (await harness.Published.Any<SendEmailRequested>()).Should().BeTrue();
        var published = harness.Published.Select<SendEmailRequested>().First().Context.Message;
        published.Recipient.Should().Be("buyer@example.com");
        published.Kind.Should().Be(EmailKind.OrderStatusUpdate);
        published.NewStatus.Should().Be("shipped");
    }

    private sealed class FakeEmailService : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string body) => Task.CompletedTask;
        public Task SendOrderConfirmationAsync(string email, Models.Entities.Order order) => Task.CompletedTask;
        public Task SendOrderStatusUpdateAsync(string email, Models.Entities.Order order, string newStatus) => Task.CompletedTask;
    }
}
