using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Infrastructure.Sharding;

public interface IOrderShardRouteStore
{
    Task<OrderShardRoute?> FindAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task RegisterAsync(Guid orderId, Guid userId, int shardIndex, CancellationToken cancellationToken = default);
}

public sealed class OrderShardRouteStore(
    AppDbContext routingDb,
    IOptions<OrderShardingOptions> options) : IOrderShardRouteStore
{
    public async Task<OrderShardRoute?> FindAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return null;

        return await routingDb.OrderShardRoutes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
    }

    public async Task RegisterAsync(
        Guid orderId,
        Guid userId,
        int shardIndex,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return;

        var exists = await routingDb.OrderShardRoutes
            .AnyAsync(r => r.OrderId == orderId, cancellationToken);
        if (exists)
            return;

        routingDb.OrderShardRoutes.Add(new OrderShardRoute
        {
            OrderId = orderId,
            UserId = userId,
            ShardIndex = shardIndex,
            CreatedAt = DateTime.UtcNow,
        });

        await routingDb.SaveChangesAsync(cancellationToken);
    }
}
