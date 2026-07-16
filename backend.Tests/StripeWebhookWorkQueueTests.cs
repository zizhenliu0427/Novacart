using FluentAssertions;
using Microsoft.Extensions.Options;
using Novacart.Api.Infrastructure.Threading;
using Novacart.Api.Services.Payments;
using Xunit;

namespace Novacart.Api.Tests;

public class StripeWebhookWorkQueueTests
{
    [Fact]
    public async Task EnqueueAsync_DeliversItemToReader()
    {
        var queue = new StripeWebhookWorkQueue(Options.Create(new ThreadPoolTuningOptions
        {
            WebhookQueueCapacity = 8,
        }));

        var item = new StripeWebhookWorkItem("evt_1", "checkout.session.completed", "{}");

        await queue.EnqueueAsync(item);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        StripeWebhookWorkItem? received = null;

        await foreach (var next in queue.ReadAllAsync(cts.Token))
        {
            received = next;
            break;
        }

        received.Should().Be(item);
    }
}
