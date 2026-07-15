namespace Novacart.Api.Models.Entities;

/// <summary>
/// A persisted refresh token supporting rotation, multi-device sessions, and
/// reuse detection. Stores only a hash of the token — the raw value is returned
/// to the client once and never persisted.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>SHA-256 hash of the raw token. The raw token is never stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the token is rotated out or explicitly revoked. Null = active.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Hash of the token that replaced this one (for audit / reuse detection).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => RevokedAt == null && !IsExpired;
}
