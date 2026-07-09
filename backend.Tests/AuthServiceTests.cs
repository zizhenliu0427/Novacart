using Xunit;
using FluentAssertions;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Auth;
using Microsoft.EntityFrameworkCore;

namespace Novacart.Api.Tests;

/// <summary>
/// Simple mock of IJwtTokenService to return deterministic dummy token values.
/// </summary>
public class FakeJwtTokenService : IJwtTokenService
{
    public (string Token, DateTime ExpiresAt) CreateToken(User user, IEnumerable<string> roles)
    {
        return ("dummy-jwt-token-value", DateTime.UtcNow.AddHours(24));
    }
}

/// <summary>
/// Unit tests for AuthService — covers registration, login, deactivation check, duplicate email rejection.
/// </summary>
public class AuthServiceTests
{
    private readonly FakeJwtTokenService _fakeJwt = new();

    [Fact]
    public async Task RegisterAsync_CreatesUser_AndReturnsAuthResponse()
    {
        using var db = TestDbFactory.Create();
        var svc = new AuthService(db, _fakeJwt);

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            FullName = "New User",
            Password = "SecurePassword123"
        };

        var response = await svc.RegisterAsync(request);

        // Verify response
        response.Should().NotBeNull();
        response.Token.Should().Be("dummy-jwt-token-value");
        response.User.Email.Should().Be("newuser@example.com");
        response.User.FullName.Should().Be("New User");
        response.User.Roles.Should().ContainSingle(r => r == RoleNames.Customer);

        // Verify DB entry
        var dbUser = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == "newuser@example.com");

        dbUser.Should().NotBeNull();
        dbUser!.FullName.Should().Be("New User");
        BCrypt.Net.BCrypt.Verify("SecurePassword123", dbUser.PasswordHash).Should().BeTrue();
        dbUser.UserRoles.Should().ContainSingle(ur => ur.RoleId == RoleNames.CustomerId);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsAuthException_WhenEmailAlreadyExists()
    {
        using var db = TestDbFactory.Create();
        var svc = new AuthService(db, _fakeJwt);
        await TestDbFactory.SeedTestUserAsync(db, "existing@example.com");

        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            FullName = "Another Name",
            Password = "Password"
        };

        var act = () => svc.RegisterAsync(request);

        await act.Should().ThrowAsync<AuthException>().Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
    {
        using var db = TestDbFactory.Create();
        var svc = new AuthService(db, _fakeJwt);

        // Seed user with a hashed password
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "loginuser@example.com",
            FullName = "Login User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("valid-password")
        };
        user.UserRoles.Add(new UserRole { RoleId = RoleNames.CustomerId });
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "loginuser@example.com",
            Password = "valid-password"
        };

        var response = await svc.LoginAsync(request);

        response.Should().NotBeNull();
        response.Token.Should().Be("dummy-jwt-token-value");
        response.User.Email.Should().Be("loginuser@example.com");
    }

    [Fact]
    public async Task LoginAsync_ThrowsAuthException_WhenPasswordIsIncorrect()
    {
        using var db = TestDbFactory.Create();
        var svc = new AuthService(db, _fakeJwt);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "loginuser@example.com",
            FullName = "Login User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password")
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "loginuser@example.com",
            Password = "incorrect-password"
        };

        var act = () => svc.LoginAsync(request);

        await act.Should().ThrowAsync<AuthException>().Where(e => e.StatusCode == 401);
    }

    [Fact]
    public async Task LoginAsync_ThrowsAuthException_WhenUserDoesNotExist()
    {
        using var db = TestDbFactory.Create();
        var svc = new AuthService(db, _fakeJwt);

        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "any-password"
        };

        var act = () => svc.LoginAsync(request);

        await act.Should().ThrowAsync<AuthException>().Where(e => e.StatusCode == 401);
    }

    [Fact]
    public async Task LoginAsync_ThrowsAuthException_WhenUserIsInactive()
    {
        using var db = TestDbFactory.Create();
        var svc = new AuthService(db, _fakeJwt);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive@example.com",
            FullName = "Inactive User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            IsActive = false
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "password"
        };

        var act = () => svc.LoginAsync(request);

        await act.Should().ThrowAsync<AuthException>().Where(e => e.StatusCode == 403);
    }
}
