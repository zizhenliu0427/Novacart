using Microsoft.EntityFrameworkCore;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Data;

/// <summary>Read-only product catalog connection (Phase 5 — Cart/Order → Product DB).</summary>
public class ProductReadDbContext : DbContext
{
    public ProductReadDbContext(DbContextOptions<ProductReadDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Property(p => p.Metadata).HasColumnType("jsonb");
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                e.Property(p => p.Tags)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
        });

        modelBuilder.Entity<Category>(e => e.ToTable("categories"));
        modelBuilder.Entity<PriceRule>(e =>
        {
            e.ToTable("price_rules");
            e.Property(r => r.RuleType).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Value).HasPrecision(18, 2);
        });
    }
}
