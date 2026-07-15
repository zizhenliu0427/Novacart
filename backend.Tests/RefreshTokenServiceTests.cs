using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for RefreshTokenService — covers generation, rotation, reuse detection,
/// expiry, and revoke-all (logout).
/// </summary>
public class RefreshTokenServiceTests
{
    /// <summary>Minimal dictionary-backed config (avoids extra NuGet deps).</summary>
    private sealed class DictConfig : IConfiguration
    {
        private readonly Dictionary<string, string?> _values;
        public DictConfig(Dictionary<string, string?> values) => _values = values;
        public string? this[string key] { get => _values.GetValueOrDefault(key); set => _values[key] = value; }
        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => NullChangeToken.Instance;
        public IConfigurationSection GetSection(string key) =>
            throw new NotImplementedException();
        public IEnumerable<KeyValuePair<string, string?>> GetChildKeys(
            IEnumerable<KeyValuePair<string, string?>> earlierKeys, string? parentPath) => _values;
    }

    /// <summary>A change token that never fires (for the no-op reload token).</summary>
    private sealed class NullChangeToken : IChangeToken
    {
        public static readonly NullChangeToken Instance = new();
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) =>
            NullDisposable.Instance;
        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    private static IConfiguration Config() => new DictConfig(new() { ["Jwt:RefreshTokenDays"] = "7" });

    [Fact]
    public async Task GenerateAsync_PersistsHashedToken_AndReturnsRaw()
    {
        using var db = TestDbFactory.Create();
        var svc = new RefreshTokenService(db, Config());
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var (rawToken, expiresAt) = await svc.GenerateAsync(userId);

        rawToken.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
        // The raw token must never be stored — only its hash.
        var stored = db.RefreshTokens.Single();
        stored.TokenHash.Should().NotBe(rawToken);
        stored.UserId.Should().Be(userId);
        stored.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RotateAsync_IssuesNewToken_AndRevokesOld()
    {
        using var db = TestDbFactory.Create();
        var svc = new RefreshTokenService(db, Config());
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var (rawToken, _) = await svc.GenerateAsync(userId);
        var (newRaw, _, rotatedUserId, roles) = await svc.RotateAsync(rawToken);

        newRaw.Should().NotBe(rawToken);
        rotatedUserId.Should().Be(userId);
        roles.Should().Contain(RoleNames.Customer);

        var old = db.RefreshTokens.First(t => t.TokenHash != HashFor(newRaw));
        old.RevokedAt.Should().NotBeNull();
        old.ReplacedByTokenHash.Should().NotBeNull();
    }

    [Fact]
    public async Task RotateAsync_RejectsRevokedToken_AndRevokesAllSessions()
    {
        // Reuse detection: presenting an already-rotated token again must revoke everything.
        using var db = TestDbFactory.Create();
        var svc = new RefreshTokenService(db, Config());
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var (rawToken, _) = await svc.GenerateAsync(userId);
        await svc.RotateAsync(rawToken); // first rotation succeeds

        // Presenting the old token again should be detected as reuse.
        var act = () => svc.RotateAsync(rawToken);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 401);
        var all = db.RefreshTokens.Where(t => t.UserId == userId).ToList();
        all.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull(),
            "reuse detection revokes the entire family");
    }

    [Fact]
    public async Task RotateAsync_RejectsUnknownToken()
    {
        using var db = TestDbFactory.Create();
        var svc = new RefreshTokenService(db, Config());

        var act = () => svc.RotateAsync("nonexistent-token");

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 401);
    }

    [Fact]
    public async Task RevokeAllAsync_RevokesAllActiveTokens_ForUser()
    {
        using var db = TestDbFactory.Create();
        var svc = new RefreshTokenService(db, Config());
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        await svc.GenerateAsync(userId);
        await svc.GenerateAsync(userId);

        await svc.RevokeAllAsync(userId);

        var active = db.RefreshTokens.Where(t => t.UserId == userId).ToList();
        active.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
    }

    private static string HashFor(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token)));
}
