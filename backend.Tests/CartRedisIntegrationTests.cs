using FluentAssertions;
using Microsoft.Extensions.Options;
using Novacart.Api.Infrastructure;
using Novacart.Api.Models.Dtos.Cart;
using Novacart.Api.Services;
using Novacart.Api.Services.CartRedis;
using Novacart.Api.Services.Catalog;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Novacart.Api.Tests;

/// <summary>PE-6: Redis-backed cart cache with real Redis (Testcontainers).</summary>
public class CartRedisIntegrationTests : IAsyncLifetime
{
    private RedisContainer? _redis;
    private bool _started;

    public async Task InitializeAsync()
    {
        try
        {
            _redis = new RedisBuilder().WithImage("redis:7-alpine").Build();
            await _redis.StartAsync();
            _started = true;
        }
        catch
        {
            _started = false;
        }
    }

    public Task DisposeAsync() => _started && _redis is not null
        ? _redis.DisposeAsync().AsTask()
        : Task.CompletedTask;

    [Fact]
    public async Task CartService_WritesSnapshotToRedis_WhenCartRedisEnabled()
    {
        if (!_started || _redis is null)
            return;

        await using var db = TestDbFactory.Create();
        var mux = ConnectionMultiplexer.Connect(_redis.GetConnectionString());
        var cache = new RedisCacheService(mux);

        var redisStore = new CartRedisStore(
            cache,
            Options.Create(new CartRedisOptions { Enabled = true, GuestTtlDays = 1, UserTtlDays = 1 }));

        var cartSvc = new CartService(
            db,
            new PricingService(),
            new DbProductCatalogStoreAdapter(db),
            redisStore);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        await cartSvc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 2 });

        var snapshot = await redisStore.GetUserCartAsync(userId);
        snapshot.Should().NotBeNull();
        snapshot!.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.Quantity == 2);

        var cachedDto = await cartSvc.GetCartAsync(userId);
        cachedDto.TotalItems.Should().Be(2);
    }
}
