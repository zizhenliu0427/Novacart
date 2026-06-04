using Microsoft.EntityFrameworkCore;

namespace Novacart.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets will be added here as we create models
    // public DbSet<User> Users => Set<User>();
    // public DbSet<Product> Products => Set<Product>();
    // public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Model configurations will go here
    }
}
