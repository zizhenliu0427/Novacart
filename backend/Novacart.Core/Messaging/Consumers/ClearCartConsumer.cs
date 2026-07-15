using MassTransit;
using Microsoft.Extensions.Logging;
using Novacart.Api.Messaging;
using Novacart.Api.Services;

namespace Novacart.Api.Messaging.Consumers;

public class ClearCartConsumer(
    ICartService cart,
    ILogger<ClearCartConsumer> logger) : IConsumer<ClearCartForOrder>
{
    public async Task Consume(ConsumeContext<ClearCartForOrder> context)
    {
        var msg = context.Message;
        await cart.ClearCartAsync(msg.UserId);
        logger.LogInformation("Cart cleared for user {UserId} after order {OrderId}", msg.UserId, msg.OrderId);
    }
}
