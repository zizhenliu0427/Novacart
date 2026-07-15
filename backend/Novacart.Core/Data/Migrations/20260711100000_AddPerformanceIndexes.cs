using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novacart.Api.Data.Migrations
{
    /// <summary>
    /// P3-3: Performance indexes for hot-query paths identified during the
    /// Alibaba Development Standards audit. See docs/database-standards.md.
    /// </summary>
    public partial class AddPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. orders(UserId, CurrentStatus) — user order list filtered by status
            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId_CurrentStatus",
                table: "orders",
                columns: new[] { "UserId", "CurrentStatus" });

            // 2. orders(CreatedAt) — analytics date-range aggregation
            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedAt",
                table: "orders",
                column: "CreatedAt");

            // 3. order_items(OrderId) — order detail loading
            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId",
                table: "order_items",
                column: "OrderId");

            // 4. order_items(ProductId) — best-seller aggregation
            migrationBuilder.CreateIndex(
                name: "IX_order_items_ProductId",
                table: "order_items",
                column: "ProductId");

            // 5. products(IsActive, CategoryId) — catalogue list filtered by category
            migrationBuilder.CreateIndex(
                name: "IX_products_IsActive_CategoryId",
                table: "products",
                columns: new[] { "IsActive", "CategoryId" });

            // 6. products(Slug) UNIQUE — slug-based lookups
            migrationBuilder.CreateIndex(
                name: "IX_products_Slug",
                table: "products",
                column: "Slug",
                unique: true);

            // 7. wishlist_items(UserId, ProductId) UNIQUE — prevent duplicates
            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_UserId_ProductId",
                table: "wishlist_items",
                columns: new[] { "UserId", "ProductId" },
                unique: true);

            // 8. cart_items(CartId, ProductId) UNIQUE — prevent duplicates
            migrationBuilder.CreateIndex(
                name: "IX_cart_items_CartId_ProductId",
                table: "cart_items",
                columns: new[] { "CartId", "ProductId" },
                unique: true);

            // 9. price_rules(IsActive, StartsAt, EndsAt) — active rule window queries
            migrationBuilder.CreateIndex(
                name: "IX_price_rules_IsActive_StartsAt_EndsAt",
                table: "price_rules",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("IX_orders_UserId_CurrentStatus", "orders");
            migrationBuilder.DropIndex("IX_orders_CreatedAt", "orders");
            migrationBuilder.DropIndex("IX_order_items_OrderId", "order_items");
            migrationBuilder.DropIndex("IX_order_items_ProductId", "order_items");
            migrationBuilder.DropIndex("IX_products_IsActive_CategoryId", "products");
            migrationBuilder.DropIndex("IX_products_Slug", "products");
            migrationBuilder.DropIndex("IX_wishlist_items_UserId_ProductId", "wishlist_items");
            migrationBuilder.DropIndex("IX_cart_items_CartId_ProductId", "cart_items");
            migrationBuilder.DropIndex("IX_price_rules_IsActive_StartsAt_EndsAt", "price_rules");
        }
    }
}
