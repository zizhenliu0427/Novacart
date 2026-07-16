using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Novacart.Api.Data;
using Novacart.Api.Infrastructure.Sharding;

var dryRun = !args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
var deleteLegacy = args.Contains("--delete-legacy", StringComparer.OrdinalIgnoreCase);

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var legacyCs = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection (legacy/routing commerce DB).");

var shardingSection = config.GetSection(OrderShardingOptions.SectionName);
var sharding = shardingSection.Get<OrderShardingOptions>()
    ?? new OrderShardingOptions();

if (!sharding.Enabled || sharding.ShardCount <= 1)
    throw new InvalidOperationException("Set OrderSharding__Enabled=true and OrderSharding__ShardCount>=2.");

var legacyOptions = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(legacyCs).Options;
await using var legacyDb = new AppDbContext(legacyOptions);

var options = Microsoft.Extensions.Options.Options.Create(sharding);
var resolver = new OrderShardResolver(options);
var factory = new OrderDbContextFactory(config, options);
var routes = new OrderShardRouteStore(legacyDb, options);

var backfill = new OrderShardBackfillService(legacyDb, factory, routes, resolver, options);
var result = await backfill.RunAsync(dryRun, deleteLegacy);

Console.WriteLine($"DryRun={result.DryRun} DeleteLegacy={result.DeleteLegacy}");
Console.WriteLine($"LegacyOrders={result.LegacyOrderCount} Migrated={result.MigratedOrPlanned} Skipped={result.SkippedExistingRoute}");

foreach (var error in result.Errors)
    Console.WriteLine($"ERROR: {error}");

return result.Errors.Count > 0 ? 1 : 0;
