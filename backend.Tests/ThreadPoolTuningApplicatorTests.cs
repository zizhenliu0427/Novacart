using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Novacart.Api.Infrastructure.Threading;
using Xunit;

namespace Novacart.Api.Tests;

public class ThreadPoolTuningApplicatorTests
{
    [Fact]
    public void Apply_WhenDisabled_DoesNotChangeMinThreads()
    {
        ThreadPool.GetMinThreads(out var beforeWorker, out var beforeIo);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThreadPool:Enabled"] = "false",
                ["ThreadPool:MinWorkerThreads"] = "128",
            })
            .Build();

        var result = ThreadPoolTuningApplicator.Apply(config);

        result.Enabled.Should().BeFalse();
        ThreadPool.GetMinThreads(out var afterWorker, out var afterIo);
        afterWorker.Should().Be(beforeWorker);
        afterIo.Should().Be(beforeIo);
    }

    [Fact]
    public void Apply_WhenEnabled_RaisesMinWorkerThreads()
    {
        ThreadPool.GetMinThreads(out var beforeWorker, out var beforeIo);
        var target = Math.Min(beforeWorker + 8, 512);

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ThreadPool:Enabled"] = "true",
                    ["ThreadPool:MinWorkerThreads"] = target.ToString(),
                })
                .Build();

            var result = ThreadPoolTuningApplicator.Apply(config);

            result.Enabled.Should().BeTrue();
            ThreadPool.GetMinThreads(out var afterWorker, out _);
            afterWorker.Should().BeGreaterThanOrEqualTo(target);
        }
        finally
        {
            ThreadPool.SetMinThreads(beforeWorker, beforeIo);
        }
    }
}
