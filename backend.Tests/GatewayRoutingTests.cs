using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Novacart.Api.Tests;

/// <summary>Smoke test that YARP routes cover all public API prefixes (PE-1 gateway).</summary>
public class GatewayRoutingTests
{
    private static readonly string[] RequiredRoutePrefixes =
    [
        "/api/auth/",
        "/api/users/",
        "/api/products/",
        "/api/cart/",
        "/api/checkout/",
        "/api/orders/",
        "/api/support/",
        "/api/admin/system/",
        "/api/health",
    ];

    private static string ResolveGatewayDirectory()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 12 && dir is not null; i++)
        {
            foreach (var relative in new[] { "Novacart.Gateway", Path.Combine("backend", "Novacart.Gateway") })
            {
                var candidate = Path.Combine(dir, relative, "appsettings.json");
                if (File.Exists(candidate))
                    return Path.Combine(dir, relative);
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new FileNotFoundException("Could not locate Novacart.Gateway/appsettings.json");
    }

    [Fact]
    public void ReverseProxy_Config_IncludesAllServiceRoutes()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(ResolveGatewayDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var routes = config.GetSection("ReverseProxy:Routes").GetChildren().ToList();
        routes.Should().NotBeEmpty();

        var paths = routes
            .Select(r => r["Match:Path"])
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p!.Replace("{**catch-all}", ""))
            .ToList();

        foreach (var prefix in RequiredRoutePrefixes)
        {
            paths.Should().Contain(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase),
                because: $"gateway should route {prefix}");
        }
    }
}
