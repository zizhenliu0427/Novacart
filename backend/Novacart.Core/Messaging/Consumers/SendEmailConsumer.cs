using MassTransit;
using Microsoft.Extensions.Logging;
using Novacart.Api.Messaging;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Messaging.Consumers;

/// <summary>Handles admin status-update emails (and other queued mail) via MassTransit.</summary>
public class SendEmailConsumer(IEmailService email, ILogger<SendEmailConsumer> logger) : IConsumer<SendEmailRequested>
{
    public async Task Consume(ConsumeContext<SendEmailRequested> context)
    {
        var message = context.Message;
        var orderSnapshot = new Order
        {
            OrderNumber = message.OrderNumber ?? "—",
            Total = message.OrderTotal ?? 0m,
        };

        try
        {
            switch (message.Kind)
            {
                case EmailKind.OrderConfirmation:
                    await email.SendOrderConfirmationAsync(message.Recipient, orderSnapshot);
                    break;
                case EmailKind.OrderStatusUpdate:
                    await email.SendOrderStatusUpdateAsync(
                        message.Recipient,
                        orderSnapshot,
                        message.NewStatus ?? "updated");
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {Kind} email to {Recipient}", message.Kind, message.Recipient);
            throw;
        }
    }
}
