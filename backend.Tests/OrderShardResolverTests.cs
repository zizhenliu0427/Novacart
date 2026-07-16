using FluentAssertions;
using Novacart.Api.Infrastructure.Sharding;
using Xunit;

namespace Novacart.Api.Tests;

public class OrderShardResolverTests
{
    [Theory]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    [InlineData("22222222-2222-2222-2222-222222222222")]
    [InlineData("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")]
    public void GetShardIndex_IsDeterministic_ForSameUser(string userIdText)
    {
        var userId = Guid.Parse(userIdText);
        var options = Microsoft.Extensions.Options.Options.Create(new OrderShardingOptions
        {
            Enabled = true,
            ShardCount = 4,
        });

        var resolver = new OrderShardResolver(options);
        var first = resolver.GetShardIndex(userId);
        var second = resolver.GetShardIndex(userId);

        first.Should().BeInRange(0, 3);
        first.Should().Be(second);
    }

    [Fact]
    public void GetShardIndex_ReturnsZero_WhenShardingDisabled()
    {
        var resolver = new OrderShardResolver(Microsoft.Extensions.Options.Options.Create(new OrderShardingOptions
        {
            Enabled = false,
            ShardCount = 4,
        }));

        resolver.GetShardIndex(Guid.NewGuid()).Should().Be(0);
    }
}
