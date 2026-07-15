using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Novacart.Api.Data;
using Novacart.Api.Messaging;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Messaging.Consumers;

/// <summary>MassTransit replacement for in-process EmailQueue on order confirmation.</summary>
public class SendOrderConfirmationConsumer(
    AppDbContext db,
    IEmailService email,
    ILogger<SendOrderConfirmationConsumer> logger) : IConsumer<OrderPaid>
{
    public async Task Consume(ConsumeContext<OrderPaid> context)
    {
        var msg = context.Message;

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == msg.OrderId);
        if (order is null)
            return;

        try
        {
            await email.SendOrderConfirmationAsync(msg.Email, order);
            logger.LogInformation("Order confirmation email sent for {OrderNumber}", order.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order confirmation for {OrderId}", msg.OrderId);
            throw;
        }
    }
}
