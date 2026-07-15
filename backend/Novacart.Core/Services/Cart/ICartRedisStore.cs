namespace Novacart.Api.Services.CartRedis;

public interface ICartRedisStore
{
    bool Enabled { get; }

    Task<CartRedisSnapshot?> GetUserCartAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CartRedisSnapshot?> GetGuestCartAsync(string sessionId, CancellationToken cancellationToken = default);

    Task SetUserCartAsync(CartRedisSnapshot snapshot, CancellationToken cancellationToken = default);

    Task SetGuestCartAsync(CartRedisSnapshot snapshot, CancellationToken cancellationToken = default);

    Task RemoveUserCartAsync(Guid userId, CancellationToken cancellationToken = default);

    Task RemoveGuestCartAsync(string sessionId, CancellationToken cancellationToken = default);
}

/// <summary>No-op store when <see cref="Infrastructure.CartRedisOptions.Enabled"/> is false.</summary>
public sealed class DisabledCartRedisStore : ICartRedisStore
{
    public static readonly DisabledCartRedisStore Instance = new();

    private DisabledCartRedisStore() { }

    public bool Enabled => false;

    public Task<CartRedisSnapshot?> GetUserCartAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CartRedisSnapshot?>(null);

    public Task<CartRedisSnapshot?> GetGuestCartAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CartRedisSnapshot?>(null);

    public Task SetUserCartAsync(CartRedisSnapshot snapshot, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SetGuestCartAsync(CartRedisSnapshot snapshot, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveUserCartAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveGuestCartAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public static class CartRedisKeys
{
    public const string UserPrefix = "novacart:cart:user:";
    public const string SessionPrefix = "novacart:cart:session:";

    public static string ForUser(Guid userId) => $"{UserPrefix}{userId:N}";

    public static string ForSession(string sessionId) => $"{SessionPrefix}{sessionId}";
}
