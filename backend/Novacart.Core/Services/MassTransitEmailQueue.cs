using MassTransit;
using Novacart.Api.Messaging;

namespace Novacart.Api.Services;

/// <summary>
/// Publishes email commands to RabbitMQ instead of an in-process channel.
/// Used by the Order microservice (Phase 7).
/// </summary>
public class MassTransitEmailQueue(IPublishEndpoint publishEndpoint) : IEmailQueue
{
    public async ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        await publishEndpoint.Publish(
            new SendEmailRequested(
                message.Kind,
                message.Recipient,
                message.OrderNumber,
                message.OrderTotal,
                message.NewStatus),
            cancellationToken);
    }
}
