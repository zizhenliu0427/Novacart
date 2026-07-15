using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Cart;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Currency;
using Novacart.Api.Services;

namespace Novacart.Api.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Swap real Postgres DB for InMemory DB to guarantee isolation
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestDb");
                });

                // Swap real Redis Cache with NullRedisCacheService to avoid connection errors in isolated test runners
                var redisDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IRedisCacheService));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                services.AddSingleton<IRedisCacheService, NullRedisCacheService>();
            });
        });
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<HealthStatusDto>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCart_ReturnsEmptyCartForGuest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AdminEndpoint_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithCustomerToken_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var customerUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            FullName = "Customer User"
        };
        var (token, _) = tokenService.CreateToken(customerUser, new[] { RoleNames.Customer });

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/admin/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminToken_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FullName = "Admin User"
        };
        var (token, _) = tokenService.CreateToken(adminUser, new[] { RoleNames.Admin });

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/admin/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCurrencyRates_ReturnsAudBaseRates()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                foreach (var d in services.Where(s => s.ServiceType == typeof(ICurrencyService)).ToList())
                {
                    services.Remove(d);
                }
                services.AddSingleton<ICurrencyService>(new StubCurrencyServiceForIntegration());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/currency/rates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ExchangeRatesDto>();
        dto.Should().NotBeNull();
        dto!.Base.Should().Be("AUD");
        dto.Rates.Should().ContainKey("USD");
        dto.Rates.Should().ContainKey("GBP");
        dto.Rates.Should().ContainKey("NZD");
    }

    private sealed class StubCurrencyServiceForIntegration : ICurrencyService
    {
        public Task<ExchangeRatesDto> GetRatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ExchangeRatesDto
            {
                Base = "AUD",
                Date = "2026-07-14",
                Source = "test",
                Rates = new Dictionary<string, decimal>
                {
                    ["USD"] = 0.69m,
                    ["CNY"] = 4.71m,
                    ["JPY"] = 112.6m,
                    ["SGD"] = 0.90m,
                    ["GBP"] = 0.51m,
                    ["NZD"] = 1.09m,
                },
            });
    }

    private class HealthStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
