using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Tests;

/// <summary>
/// Shared test helpers: InMemory DB factory and seed data builders.
/// </summary>
public static class TestDbFactory
{
    /// <summary>
    /// Create a fresh InMemory AppDbContext with a unique database name per test.
    /// </summary>
    public static AppDbContext Create(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated(); // triggers HasData seeds (roles, categories, products)
        return db;
    }

    /// <summary>
    /// Create a test user and return its ID. Does NOT hash the password
    /// (hashing is AuthService's job — unit tests for service logic don't go through it).
    /// </summary>
    public static async Task<Guid> SeedTestUserAsync(AppDbContext db, string email = "test@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Test User",
            PasswordHash = "not-a-real-hash",
        };
        db.Users.Add(user);

        // Assign 'customer' role (seeded as Id = 1).
        db.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = 1 });
        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>
    /// Return the first seeded active product.
    /// </summary>
    public static async Task<Product> GetFirstProductAsync(AppDbContext db)
        => await db.Products.FirstAsync(p => p.IsActive);
}
