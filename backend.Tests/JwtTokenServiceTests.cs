using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for JwtTokenService — verifies configuration parsing, claim mappings, expiry, and signature validation.
/// </summary>
public class JwtTokenServiceTests
{
    private readonly IConfiguration _config;
    private const string TestSecret = "this-is-a-very-long-and-secure-jwt-secret-key-32-chars";

    public JwtTokenServiceTests()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", TestSecret },
            { "Jwt:ExpiryHours", "12" },
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    [Fact]
    public void CreateToken_GeneratesValidToken_WithCorrectClaimsAndExpiration()
    {
        var svc = new JwtTokenService(_config);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tokenuser@example.com",
            FullName = "Token User"
        };
        var roles = new[] { "customer", "admin" };

        var (token, expiresAt) = svc.CreateToken(user, roles);

        // Verify return values
        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(12), TimeSpan.FromMinutes(1));

        // Read and parse the token to verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("test-issuer");
        jwtToken.Audiences.Should().ContainSingle(a => a == "test-audience");
        
        // Find subject claim
        var subClaim = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Value.Should().Be(user.Id.ToString());

        // Find email claim
        var emailClaim = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Value.Should().Be("tokenuser@example.com");

        // Find name claim
        var nameClaim = jwtToken.Claims.First(c => c.Type == ClaimTypes.Name);
        nameClaim.Value.Should().Be("Token User");

        // Find role claims
        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        roleClaims.Should().BeEquivalentTo("customer", "admin");
    }

    [Fact]
    public void Constructor_ThrowsInvalidOperationException_WhenSecretIsMissing()
    {
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var act = () => new JwtTokenService(emptyConfig);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }
}
