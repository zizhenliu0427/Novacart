using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IRefreshTokenService
{
    /// <summary>Issue and persist a new refresh token for the user. Returns the raw token (shown once).</summary>
    Task<(string RawToken, DateTime ExpiresAt)> GenerateAsync(Guid userId);

    /// <summary>
    /// Validate the raw token, revoke it, and issue a replacement (rotation).
    /// Returns the new raw token + the user id. Throws <see cref="AppException"/> on
    /// invalid/expired/revoked tokens, or when reuse of a revoked token is detected.
    /// </summary>
    Task<(string NewRawToken, DateTime ExpiresAt, Guid UserId, IEnumerable<string> Roles)> RotateAsync(string rawToken);

    /// <summary>Revoke all active refresh tokens for a user (used on logout / password change).</summary>
    Task RevokeAllAsync(Guid userId);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _db;
    private readonly int _refreshTokenDays;

    public RefreshTokenService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _refreshTokenDays = int.TryParse(config["Jwt:RefreshTokenDays"], out var d) ? d : 7;
    }

    public async Task<(string RawToken, DateTime ExpiresAt)> GenerateAsync(Guid userId)
    {
        var rawToken = GenerateOpaqueToken();
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
        };
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
        return (rawToken, token.ExpiresAt);
    }

    public async Task<(string NewRawToken, DateTime ExpiresAt, Guid UserId, IEnumerable<string> Roles)> RotateAsync(string rawToken)
    {
        var hash = Hash(rawToken);
        var existing = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u!.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (existing is null)
            throw AppException.Unauthorized("Invalid refresh token.");

        // Reuse detection: a revoked token being presented again means it may have been
        // stolen. Revoke the entire family (all tokens for this user) as a precaution.
        if (existing.RevokedAt != null)
        {
            await RevokeAllAsync(existing.UserId);
            throw AppException.Unauthorized("Refresh token reuse detected. All sessions revoked.");
        }

        if (existing.IsExpired)
            throw AppException.Unauthorized("Refresh token has expired.");

        // Rotate: revoke the old token and issue a new one, linking them.
        var newRaw = GenerateOpaqueToken();
        var replacement = new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = Hash(newRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
            ReplacedByTokenHash = null,
        };

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByTokenHash = replacement.TokenHash;

        _db.RefreshTokens.Add(replacement);
        await _db.SaveChangesAsync();

        var roles = existing.User?.UserRoles.Select(ur => ur.Role.Name).ToList()
                    ?? new List<string>();
        return (newRaw, replacement.ExpiresAt, existing.UserId, roles);
    }

    public async Task RevokeAllAsync(Guid userId)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in active)
            token.RevokedAt = DateTime.UtcNow;

        if (active.Count > 0)
            await _db.SaveChangesAsync();
    }

    /// <summary>Generate a URL-safe 256-bit opaque token.</summary>
    private static string GenerateOpaqueToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    /// <summary>SHA-256 hash (refresh tokens are high-entropy, so plain hashing suffices unlike passwords).</summary>
    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
