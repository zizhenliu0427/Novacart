using Microsoft.EntityFrameworkCore;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ───────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique().HasDatabaseName("idx_users_email");
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        });

        // ── Role ───────────────────────────────────────────────
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.Property(r => r.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(r => r.Name).IsUnique().HasDatabaseName("idx_roles_name");
        });

        // ── UserRole (join, composite key) ─────────────────────
        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Category ───────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.Property(c => c.Name).HasMaxLength(150).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(150).IsRequired();
            e.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("idx_categories_slug");
            e.HasOne(c => c.Parent)
                .WithMany()
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Product ────────────────────────────────────────────
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.Property(p => p.Slug).HasMaxLength(200).IsRequired();
            e.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("idx_products_slug");
            e.Property(p => p.Name).HasMaxLength(300).IsRequired();
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Property(p => p.Currency).HasMaxLength(3);
            e.Property(p => p.Metadata).HasColumnType("jsonb");
            e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Order ──────────────────────────────────────────────
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.Property(o => o.OrderNumber).HasMaxLength(40).IsRequired();
            e.HasIndex(o => o.OrderNumber).IsUnique().HasDatabaseName("idx_orders_order_number");
            e.Property(o => o.Currency).HasMaxLength(3);
            e.Property(o => o.CurrentStatus).HasMaxLength(30);
            e.Property(o => o.Subtotal).HasPrecision(18, 2);
            e.Property(o => o.ShippingCost).HasPrecision(18, 2);
            e.Property(o => o.Tax).HasPrecision(18, 2);
            e.Property(o => o.Total).HasPrecision(18, 2);
            e.HasIndex(o => new { o.UserId, o.CurrentStatus, o.CreatedAt })
                .HasDatabaseName("idx_orders_user_status");
            e.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── OrderItem ──────────────────────────────────────────
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.Property(i => i.ProductNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(i => i.PriceAtPurchase).HasPrecision(18, 2);
            e.HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Seed data ──────────────────────────────────────────
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = RoleNames.CustomerId, Name = RoleNames.Customer, Description = "Standard shopper" },
            new Role { Id = RoleNames.AdminId, Name = RoleNames.Admin, Description = "Store administrator" },
            new Role { Id = RoleNames.SysAdminId, Name = RoleNames.SysAdmin, Description = "System administrator" }
        );
    }
}
