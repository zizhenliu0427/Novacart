using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Novacart.Api.Data;

namespace Novacart.Api.Infrastructure.Sharding;

public interface IOrderDbContextFactory
{
    AppDbContext CreateShardContext(int shardIndex);
}

public sealed class OrderDbContextFactory(
    IConfiguration configuration,
    IOptions<OrderShardingOptions> options) : IOrderDbContextFactory
{
    public AppDbContext CreateShardContext(int shardIndex)
    {
        var connectionString = ResolveShardConnection(shardIndex)
            ?? throw new InvalidOperationException(
                $"Connection string for commerce shard {shardIndex} is not configured.");

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(connectionString);
        return new AppDbContext(builder.Options);
    }

    private string? ResolveShardConnection(int shardIndex)
    {
        var shardKey = $"CommerceShard{shardIndex}";
        var shardCs = configuration.GetConnectionString(shardKey);
        if (!string.IsNullOrWhiteSpace(shardCs))
            return shardCs;

        if (shardIndex == 0 || !options.Value.Enabled)
            return configuration.GetConnectionString("DefaultConnection");

        return null;
    }
}
