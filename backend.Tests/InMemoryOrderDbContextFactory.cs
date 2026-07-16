using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Novacart.Api.Data;
using Novacart.Api.Infrastructure.Sharding;

namespace Novacart.Api.Tests;

/// <summary>In-memory factory for sharded order integration tests.</summary>
public sealed class InMemoryOrderDbContextFactory(IReadOnlyList<string> shardDatabaseNames) : IOrderDbContextFactory
{
    public AppDbContext CreateShardContext(int shardIndex)
    {
        if (shardIndex < 0 || shardIndex >= shardDatabaseNames.Count)
            throw new ArgumentOutOfRangeException(nameof(shardIndex));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(shardDatabaseNames[shardIndex])
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
