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
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhook> PaymentWebhooks => Set<PaymentWebhook>();

    // ── P2 scaffold (see HANDOFF §7 / §13) ──
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

            // InMemory provider doesn't support string[] mapping natively.
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                e.Property(p => p.Tags)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
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
            e.Property(o => o.Total).HasPrecision(18,2);

            e.Property(o => o.ShippingName).HasMaxLength(150).IsRequired();
            e.Property(o => o.ShippingLine1).HasMaxLength(200).IsRequired();
            e.Property(o => o.ShippingLine2).HasMaxLength(200);
            e.Property(o => o.ShippingCity).HasMaxLength(100).IsRequired();
            e.Property(o => o.ShippingState).HasMaxLength(100).IsRequired();
            e.Property(o => o.ShippingPostcode).HasMaxLength(20).IsRequired();
            e.Property(o => o.ShippingCountry).HasMaxLength(100).IsRequired();
            e.HasIndex(o => new { o.UserId, o.CurrentStatus, o.CreatedAt })
                .HasDatabaseName("idx_orders_user_status");
            e.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── OrderStatusHistory (P2-7 audit trail) ─────────────
        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.ToTable("order_status_history");
            e.Property(h => h.ToStatus).HasMaxLength(30).IsRequired();
            e.Property(h => h.FromStatus).HasMaxLength(30);
            e.Property(h => h.Notes).HasMaxLength(500);
            e.HasIndex(h => h.OrderId).HasDatabaseName("idx_order_status_history_order");
            e.HasOne(h => h.Order)
                .WithMany()
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
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

        // ── Cart ───────────────────────────────────────────────
        modelBuilder.Entity<Cart>(e =>
        {
            e.ToTable("carts");
            e.HasIndex(c => c.UserId).HasDatabaseName("idx_carts_user_id");
            e.HasIndex(c => c.SessionId).HasDatabaseName("idx_carts_session_id");
            e.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CartItem ───────────────────────────────────────────
        modelBuilder.Entity<CartItem>(e =>
        {
            e.ToTable("cart_items");
            e.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PaymentMethod ──────────────────────────────────────
        modelBuilder.Entity<PaymentMethod>(e =>
        {
            e.ToTable("payment_methods");
            e.Property(pm => pm.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(pm => pm.Code).IsUnique().HasDatabaseName("idx_payment_methods_code");
            e.Property(pm => pm.DisplayName).HasMaxLength(150).IsRequired();
            e.Property(pm => pm.Config).HasColumnType("jsonb");
        });

        // ── Payment ────────────────────────────────────────────
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.Property(p => p.ProviderTransactionId).HasMaxLength(255).IsRequired();
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.Currency).HasMaxLength(3);
            e.Property(p => p.Status).HasMaxLength(50);
            e.Property(p => p.RawResponse).HasColumnType("jsonb");
            e.HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.PaymentMethod)
                .WithMany()
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PaymentWebhook ─────────────────────────────────────
        modelBuilder.Entity<PaymentWebhook>(e =>
        {
            e.ToTable("payment_webhooks");
            e.Property(w => w.EventId).HasMaxLength(255).IsRequired();
            e.HasIndex(w => w.EventId).IsUnique().HasDatabaseName("idx_payment_webhooks_event_id");
            e.Property(w => w.EventType).HasMaxLength(150).IsRequired();
            e.Property(w => w.Payload).HasColumnType("jsonb");
            e.Property(w => w.ErrorMessage).HasColumnType("text");
            e.HasOne(w => w.PaymentMethod)
                .WithMany()
                .HasForeignKey(w => w.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── P2 scaffold entities (bodies/logic TODO — see HANDOFF §7) ──
        // ── WishlistItem (P2-3) ────────────────────────────────
        modelBuilder.Entity<WishlistItem>(e =>
        {
            e.ToTable("wishlist_items");
            e.HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique().HasDatabaseName("idx_wishlist_user_product");
            e.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PriceRule (P2-5) ───────────────────────────────────
        modelBuilder.Entity<PriceRule>(e =>
        {
            e.ToTable("price_rules");
            e.Property(r => r.RuleType).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Value).HasPrecision(18, 2);
            e.HasIndex(r => r.ProductId).HasDatabaseName("idx_price_rules_product_id");
            e.HasIndex(r => r.CategoryId).HasDatabaseName("idx_price_rules_category_id");
            e.HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Category)
                .WithMany()
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── UserAddress (P2-7) ─────────────────────────────────
        modelBuilder.Entity<UserAddress>(e =>
        {
            e.ToTable("user_addresses");
            e.Property(a => a.Label).HasMaxLength(50);
            e.Property(a => a.Line1).HasMaxLength(200).IsRequired();
            e.Property(a => a.Line2).HasMaxLength(200);
            e.Property(a => a.City).HasMaxLength(100);
            e.Property(a => a.State).HasMaxLength(100);
            e.Property(a => a.Postcode).HasMaxLength(20);
            e.Property(a => a.Country).HasMaxLength(100);
            e.HasIndex(a => a.UserId).HasDatabaseName("idx_user_addresses_user_id");
            e.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RefreshToken (JWT refresh, rotation + reuse detection) ──
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("idx_refresh_tokens_token_hash");
            e.HasIndex(t => t.UserId).HasDatabaseName("idx_refresh_tokens_user_id");
            e.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
            e.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed data ──────────────────────────────────────────
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = RoleNames.CustomerId, Name = RoleNames.Customer, Description = "Standard shopper" },
            new Role { Id = RoleNames.AdminId, Name = RoleNames.Admin, Description = "Store administrator" },
            new Role { Id = RoleNames.SysAdminId, Name = RoleNames.SysAdmin, Description = "System administrator" }
        );

        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { Id = 1, Code = "stripe", DisplayName = "Credit / Debit Card", IsActive = true }
        );

        // Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics",  Slug = "electronics",  DisplayOrder = 1 },
            new Category { Id = 2, Name = "Apparel",      Slug = "apparel",      DisplayOrder = 2 },
            new Category { Id = 3, Name = "Home & Living",Slug = "home-living",  DisplayOrder = 3 },
            new Category { Id = 4, Name = "Accessories",  Slug = "accessories",  DisplayOrder = 4 },
            new Category { Id = 5, Name = "Books",        Slug = "books",        DisplayOrder = 5 }
        );

        // Products — deliberately span 3+ types to prove general-marketplace / type-specific attributes
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Product>().HasData(
            // ── Electronics ────────────────────────────────────
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000001"),
                Slug = "wireless-noise-cancelling-headphones",
                Name = "Wireless Noise-Cancelling Headphones",
                Description = "Premium over-ear headphones with 30-hour battery and adaptive noise cancellation for immersive listening.",
                Price = 199.99m, Currency = "AUD", StockQuantity = 45,
                CategoryId = 1, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "audio", "wireless", "noise-cancelling", "bluetooth" },
                Metadata = """{"brand":"SoundPro","connectivity":"Bluetooth 5.3","battery_hours":30,"weight_g":250,"color":"Midnight Black","frequency_response":"20Hz–20kHz","driver_size_mm":40}""",
                ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&auto=format&fit=crop&q=60",
            },
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000002"),
                Slug = "ultrawide-curved-monitor-34",
                Name = "34\" Ultrawide Curved Monitor",
                Description = "QHD IPS panel with 144Hz refresh rate, 1ms response time, and USB-C power delivery for creatives and gamers.",
                Price = 649.00m, Currency = "AUD", StockQuantity = 12,
                CategoryId = 1, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "monitor", "ultrawide", "gaming", "usb-c" },
                Metadata = """{"brand":"ViewMax","resolution":"3440x1440","refresh_rate_hz":144,"panel_type":"IPS","response_time_ms":1,"ports":"2x HDMI, 1x DisplayPort, 2x USB-C","size_inches":34}""",
                ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=500&auto=format&fit=crop&q=60",
            },
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000003"),
                Slug = "mechanical-keyboard-tkl",
                Name = "Tenkeyless Mechanical Keyboard",
                Description = "Compact TKL layout with hot-swap switches, per-key RGB, and aluminium top plate. Cherry MX Red switches included.",
                Price = 149.95m, Currency = "AUD", StockQuantity = 30,
                CategoryId = 1, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "keyboard", "mechanical", "rgb", "gaming", "compact" },
                Metadata = """{"brand":"KeyForge","layout":"TKL (80%)","switch_type":"Cherry MX Red","backlight":"Per-key RGB","connectivity":"USB-C detachable","material":"Aluminium top plate","hot_swap":true}""",
                ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=500&auto=format&fit=crop&q=60",
            },

            // ── Apparel ────────────────────────────────────────
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000004"),
                Slug = "merino-wool-crew-neck-sweater",
                Name = "Merino Wool Crew-Neck Sweater",
                Description = "100% Australian merino wool — naturally temperature-regulating, itch-free, and machine washable. A wardrobe staple.",
                Price = 89.00m, Currency = "AUD", StockQuantity = 80,
                CategoryId = 2, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "wool", "merino", "knitwear", "sustainable", "unisex" },
                Metadata = """{"material":"100% Australian Merino Wool","sizes":["XS","S","M","L","XL","XXL"],"colors":["Oatmeal","Navy","Forest Green","Charcoal"],"fit":"Regular","care":"Machine wash cold, lay flat to dry","origin":"Australia"}""",
                ImageUrl = "https://images.unsplash.com/photo-1614975058789-41316d0e2e9c?w=500&auto=format&fit=crop&q=60",
            },
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000005"),
                Slug = "slim-fit-chino-pants",
                Name = "Slim-Fit Chino Pants",
                Description = "Stretch-cotton chinos with a clean slim silhouette — smart enough for the office, comfortable enough for the weekend.",
                Price = 69.99m, Currency = "AUD", StockQuantity = 120,
                CategoryId = 2, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "pants", "chino", "slim-fit", "smart-casual" },
                Metadata = """{"material":"97% Cotton, 3% Elastane","sizes":["28","30","32","34","36","38"],"inseam_options":["30\"","32\"","34\""],"colors":["Stone","Navy","Olive","Charcoal"],"fit":"Slim","care":"Machine wash 30°C"}""",
                ImageUrl = "https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?w=500&auto=format&fit=crop&q=60",
            },

            // ── Home & Living ──────────────────────────────────
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000006"),
                Slug = "hand-poured-soy-candle-set",
                Name = "Hand-Poured Soy Candle Set (3-pack)",
                Description = "Three artisan soy-wax candles in a gift-ready box — Cedar & Amber, Linen & Sea Salt, and Bergamot & White Tea.",
                Price = 49.95m, Currency = "AUD", StockQuantity = 60,
                CategoryId = 3, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "candle", "soy-wax", "gift", "scented", "handmade" },
                Metadata = """{"wax_type":"100% natural soy","burn_time_hours":35,"scents":["Cedar & Amber","Linen & Sea Salt","Bergamot & White Tea"],"container":"Glass tumbler","weight_g":200,"wick":"Cotton braided","quantity_in_pack":3}""",
                ImageUrl = "https://images.unsplash.com/photo-1603006905003-be475563bc59?w=500&auto=format&fit=crop&q=60",
            },
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000007"),
                Slug = "walnut-serving-board",
                Name = "Solid Walnut Serving Board",
                Description = "Handcrafted from sustainably sourced Australian black walnut. Juice groove, hanging hole, and oiled finish included.",
                Price = 75.00m, Currency = "AUD", StockQuantity = 25,
                CategoryId = 3, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "wood", "walnut", "kitchen", "serving", "handmade", "sustainable" },
                Metadata = """{"material":"Solid Australian Black Walnut","dimensions_cm":"40 x 25 x 2","finish":"Food-safe mineral oil","features":["Juice groove","Hanging hole"],"care":"Hand wash only, re-oil monthly","origin":"Australia"}""",
                ImageUrl = "https://images.unsplash.com/photo-1587314168485-3236d6710814?w=500&auto=format&fit=crop&q=60",
            },
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000008"),
                Slug = "linen-duvet-cover-queen",
                Name = "Stonewashed Linen Duvet Cover — Queen",
                Description = "Pre-washed French linen that gets softer with every wash. Breathable and naturally thermoregulating for year-round comfort.",
                Price = 189.00m, Currency = "AUD", StockQuantity = 18,
                CategoryId = 3, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "linen", "bedding", "duvet", "sustainable", "french-linen" },
                Metadata = """{"material":"100% French Linen","size":"Queen (210x210cm)","colors":["White","Sage","Dusk Blue","Natural"],"thread_count":"N/A (woven linen)","care":"Machine wash 40°C, tumble dry low","pillowcases_included":2}""",
                ImageUrl = "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?w=500&auto=format&fit=crop&q=60",
            },

            // ── Accessories ────────────────────────────────────
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000009"),
                Slug = "full-grain-leather-wallet",
                Name = "Full-Grain Leather Bi-Fold Wallet",
                Description = "Hand-stitched full-grain cowhide wallet with 6 card slots, a bill compartment, and RFID-blocking lining.",
                Price = 64.95m, Currency = "AUD", StockQuantity = 55,
                CategoryId = 4, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "leather", "wallet", "accessories", "rfid", "handmade" },
                Metadata = """{"material":"Full-grain cowhide leather","card_slots":6,"has_bill_compartment":true,"rfid_blocking":true,"colors":["Tan","Dark Brown","Black"],"dimensions_cm":"9.5 x 11 (open)","weight_g":65}""",
                ImageUrl = "https://images.unsplash.com/photo-1627124118303-624c8f5d224d?w=500&auto=format&fit=crop&q=60",
            },
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000010"),
                Slug = "canvas-tote-bag-natural",
                Name = "Heavy-Canvas Tote Bag",
                Description = "12oz natural canvas tote with reinforced stitching, internal zip pocket, and a padded laptop sleeve up to 14\".",
                Price = 39.00m, Currency = "AUD", StockQuantity = 90,
                CategoryId = 4, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "tote", "canvas", "sustainable", "laptop", "everyday" },
                Metadata = """{"material":"12oz natural cotton canvas","laptop_sleeve":"Up to 14\"","internal_pockets":2,"external_pockets":1,"shoulder_strap_drop_cm":28,"capacity_litres":20,"colors":["Natural","Black","Olive"]}""",
                ImageUrl = "https://images.unsplash.com/photo-1544816155-12df9643f363?w=500&auto=format&fit=crop&q=60",
            },

            // ── Books ──────────────────────────────────────────
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000011"),
                Slug = "the-pragmatic-programmer-20th",
                Name = "The Pragmatic Programmer (20th Anniversary Ed.)",
                Description = "The classic guide to software craftsmanship — updated for modern development with new topics, tips, and exercises.",
                Price = 59.95m, Currency = "AUD", StockQuantity = 40,
                CategoryId = 5, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "programming", "software", "career", "bestseller" },
                Metadata = """{"author":"David Thomas, Andrew Hunt","publisher":"Pragmatic Bookshelf","edition":"20th Anniversary","pages":352,"isbn":"978-0135957059","format":["Paperback","eBook"],"language":"English","year":2019}""",
                ImageUrl = "https://images.unsplash.com/photo-1629654297299-c8506221ca97?w=500&auto=format&fit=crop&q=60",
            },
            new Product
            {
                Id = new Guid("11111111-0000-0000-0000-000000000012"),
                Slug = "atomic-habits",
                Name = "Atomic Habits",
                Description = "James Clear's #1 bestseller on building good habits and breaking bad ones — backed by science and packed with practical strategies.",
                Price = 29.99m, Currency = "AUD", StockQuantity = 100,
                CategoryId = 5, IsActive = true, CreatedAt = now, UpdatedAt = now,
                Tags = new[] { "self-help", "habits", "productivity", "bestseller" },
                Metadata = """{"author":"James Clear","publisher":"Random House Business","pages":320,"isbn":"978-1847941831","format":["Paperback","Hardcover","eBook","Audiobook"],"language":"English","year":2018}""",
                ImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=500&auto=format&fit=crop&q=60",
            }
        );
    }
}
