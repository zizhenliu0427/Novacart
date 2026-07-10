using Xunit;
using FluentAssertions;
using Novacart.Api.Services;
using Novacart.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Novacart.Api.Tests;

/// <summary>Unit tests for UserService — P2-2 (profile management).</summary>
public class UserServiceTests
{
    [Fact]
    public async Task GetProfileAsync_ReturnsUserWithRoles()
    {
        using var db = TestDbFactory.Create();
        var svc = new UserService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db, "profile@example.com");

        var profile = await svc.GetProfileAsync(userId);

        profile.Email.Should().Be("profile@example.com");
        profile.FullName.Should().Be("Test User");
        profile.Roles.Should().Contain("customer");
    }

    [Fact]
    public async Task GetProfileAsync_ThrowsNotFound_WhenUserMissing()
    {
        using var db = TestDbFactory.Create();
        var svc = new UserService(db);

        var act = () => svc.GetProfileAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesFullName()
    {
        using var db = TestDbFactory.Create();
        var svc = new UserService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var updated = await svc.UpdateProfileAsync(userId, new UpdateProfileRequest("New Name"));

        updated.FullName.Should().Be("New Name");
        var dbUser = await db.Users.FirstAsync(u => u.Id == userId);
        dbUser.FullName.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateProfileAsync_RejectsEmptyName()
    {
        using var db = TestDbFactory.Create();
        var svc = new UserService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var act = () => svc.UpdateProfileAsync(userId, new UpdateProfileRequest(""));

        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_TrimsWhitespace()
    {
        using var db = TestDbFactory.Create();
        var svc = new UserService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var updated = await svc.UpdateProfileAsync(userId, new UpdateProfileRequest("  Spaced Name  "));

        updated.FullName.Should().Be("Spaced Name");
    }
}
