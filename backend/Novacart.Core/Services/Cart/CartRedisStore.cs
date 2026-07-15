using Microsoft.Extensions.Options;
using Novacart.Api.Infrastructure;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services.CartRedis;

public class CartRedisStore(
    IRedisCacheService cache,
    IOptions<CartRedisOptions> options) : ICartRedisStore
{
    public bool Enabled => options.Value.Enabled;

    public Task<CartRedisSnapshot?> GetUserCartAsync(Guid userId, CancellationToken cancellationToken = default) =>
        cache.GetAsync<CartRedisSnapshot>(CartRedisKeys.ForUser(userId));

    public Task<CartRedisSnapshot?> GetGuestCartAsync(string sessionId, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(sessionId)
            ? Task.FromResult<CartRedisSnapshot?>(null)
            : cache.GetAsync<CartRedisSnapshot>(CartRedisKeys.ForSession(sessionId));

    public Task SetUserCartAsync(CartRedisSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (!Enabled || snapshot.UserId is null)
            return Task.CompletedTask;

        return cache.SetAsync(
            CartRedisKeys.ForUser(snapshot.UserId.Value),
            snapshot,
            TimeSpan.FromDays(Math.Max(1, options.Value.UserTtlDays)));
    }

    public Task SetGuestCartAsync(CartRedisSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(snapshot.SessionId))
            return Task.CompletedTask;

        return cache.SetAsync(
            CartRedisKeys.ForSession(snapshot.SessionId),
            snapshot,
            TimeSpan.FromDays(Math.Max(1, options.Value.GuestTtlDays)));
    }

    public Task RemoveUserCartAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Enabled ? cache.RemoveAsync(CartRedisKeys.ForUser(userId)) : Task.CompletedTask;

    public Task RemoveGuestCartAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Enabled && !string.IsNullOrWhiteSpace(sessionId)
            ? cache.RemoveAsync(CartRedisKeys.ForSession(sessionId))
            : Task.CompletedTask;

    public static CartRedisSnapshot ToSnapshot(Cart cart) =>
        new()
        {
            CartId = cart.Id,
            UserId = cart.UserId,
            SessionId = cart.SessionId,
            UpdatedAt = cart.UpdatedAt,
            Items = cart.Items
                .Select(i => new CartItemSnapshot(i.Id, i.ProductId, i.Quantity))
                .ToList(),
        };

    public static Cart ToEntity(CartRedisSnapshot snapshot)
    {
        var cart = new Cart
        {
            Id = snapshot.CartId,
            UserId = snapshot.UserId,
            SessionId = snapshot.SessionId,
            UpdatedAt = snapshot.UpdatedAt,
        };

        foreach (var item in snapshot.Items)
        {
            cart.Items.Add(new CartItem
            {
                Id = item.Id,
                CartId = snapshot.CartId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }

        return cart;
    }
}
