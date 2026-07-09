using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user, IEnumerable<string> roles);
}

/// <summary>
/// Issues HS256 JWTs signed with the <c>Jwt:Secret</c> from configuration.
/// Claims: sub (user id), email, name, and one role claim per role.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly int _expiryHours;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration config)
    {
        _secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _expiryHours = int.TryParse(config["Jwt:ExpiryHours"], out var h) ? h : 24;
        _issuer = config["Jwt:Issuer"] ?? "novacart";
        _audience = config["Jwt:Audience"] ?? "novacart";
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user, IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddHours(_expiryHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.FullName),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
