using Microsoft.Extensions.Options;

namespace Novacart.Api.Infrastructure.Sharding;

public interface IOrderShardResolver
{
    bool Enabled { get; }

    int ShardCount { get; }

    int GetShardIndex(Guid userId);
}

public sealed class OrderShardResolver(IOptions<OrderShardingOptions> options) : IOrderShardResolver
{
    private readonly OrderShardingOptions _options = options.Value;

    public bool Enabled => _options.Enabled && _options.ShardCount > 1;

    public int ShardCount => Math.Max(1, _options.ShardCount);

    public int GetShardIndex(Guid userId)
    {
        if (!Enabled)
            return 0;

        return (int)(StableHash(userId) % (uint)_options.ShardCount);
    }

    /// <summary>FNV-1a over Guid bytes — stable across processes and .NET versions.</summary>
    internal static uint StableHash(Guid userId)
    {
        var bytes = userId.ToByteArray();
        uint hash = 2166136261u;
        foreach (var b in bytes)
            hash = (hash ^ b) * 16777619u;

        return hash;
    }
}
