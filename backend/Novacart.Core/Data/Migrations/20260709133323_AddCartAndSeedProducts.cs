using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Novacart.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCartAndSeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cart_items_carts_CartId",
                        column: x => x.CartId,
                        principalTable: "carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cart_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "DisplayOrder", "Name", "ParentId", "Slug" },
                values: new object[,]
                {
                    { 1, 1, "Electronics", null, "electronics" },
                    { 2, 2, "Apparel", null, "apparel" },
                    { 3, 3, "Home & Living", null, "home-living" },
                    { 4, 4, "Accessories", null, "accessories" },
                    { 5, 5, "Books", null, "books" }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Currency", "Description", "IsActive", "Metadata", "Name", "Price", "Slug", "StockQuantity", "Tags", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "Premium over-ear headphones with 30-hour battery and adaptive noise cancellation for immersive listening.", true, "{\"brand\":\"SoundPro\",\"connectivity\":\"Bluetooth 5.3\",\"battery_hours\":30,\"weight_g\":250,\"color\":\"Midnight Black\",\"frequency_response\":\"20Hz–20kHz\",\"driver_size_mm\":40}", "Wireless Noise-Cancelling Headphones", 199.99m, "wireless-noise-cancelling-headphones", 45, new[] { "audio", "wireless", "noise-cancelling", "bluetooth" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000002"), 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "QHD IPS panel with 144Hz refresh rate, 1ms response time, and USB-C power delivery for creatives and gamers.", true, "{\"brand\":\"ViewMax\",\"resolution\":\"3440x1440\",\"refresh_rate_hz\":144,\"panel_type\":\"IPS\",\"response_time_ms\":1,\"ports\":\"2x HDMI, 1x DisplayPort, 2x USB-C\",\"size_inches\":34}", "34\" Ultrawide Curved Monitor", 649.00m, "ultrawide-curved-monitor-34", 12, new[] { "monitor", "ultrawide", "gaming", "usb-c" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000003"), 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "Compact TKL layout with hot-swap switches, per-key RGB, and aluminium top plate. Cherry MX Red switches included.", true, "{\"brand\":\"KeyForge\",\"layout\":\"TKL (80%)\",\"switch_type\":\"Cherry MX Red\",\"backlight\":\"Per-key RGB\",\"connectivity\":\"USB-C detachable\",\"material\":\"Aluminium top plate\",\"hot_swap\":true}", "Tenkeyless Mechanical Keyboard", 149.95m, "mechanical-keyboard-tkl", 30, new[] { "keyboard", "mechanical", "rgb", "gaming", "compact" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000004"), 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "100% Australian merino wool — naturally temperature-regulating, itch-free, and machine washable. A wardrobe staple.", true, "{\"material\":\"100% Australian Merino Wool\",\"sizes\":[\"XS\",\"S\",\"M\",\"L\",\"XL\",\"XXL\"],\"colors\":[\"Oatmeal\",\"Navy\",\"Forest Green\",\"Charcoal\"],\"fit\":\"Regular\",\"care\":\"Machine wash cold, lay flat to dry\",\"origin\":\"Australia\"}", "Merino Wool Crew-Neck Sweater", 89.00m, "merino-wool-crew-neck-sweater", 80, new[] { "wool", "merino", "knitwear", "sustainable", "unisex" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000005"), 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "Stretch-cotton chinos with a clean slim silhouette — smart enough for the office, comfortable enough for the weekend.", true, "{\"material\":\"97% Cotton, 3% Elastane\",\"sizes\":[\"28\",\"30\",\"32\",\"34\",\"36\",\"38\"],\"inseam_options\":[\"30\\\"\",\"32\\\"\",\"34\\\"\"],\"colors\":[\"Stone\",\"Navy\",\"Olive\",\"Charcoal\"],\"fit\":\"Slim\",\"care\":\"Machine wash 30°C\"}", "Slim-Fit Chino Pants", 69.99m, "slim-fit-chino-pants", 120, new[] { "pants", "chino", "slim-fit", "smart-casual" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000006"), 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "Three artisan soy-wax candles in a gift-ready box — Cedar & Amber, Linen & Sea Salt, and Bergamot & White Tea.", true, "{\"wax_type\":\"100% natural soy\",\"burn_time_hours\":35,\"scents\":[\"Cedar & Amber\",\"Linen & Sea Salt\",\"Bergamot & White Tea\"],\"container\":\"Glass tumbler\",\"weight_g\":200,\"wick\":\"Cotton braided\",\"quantity_in_pack\":3}", "Hand-Poured Soy Candle Set (3-pack)", 49.95m, "hand-poured-soy-candle-set", 60, new[] { "candle", "soy-wax", "gift", "scented", "handmade" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000007"), 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "Handcrafted from sustainably sourced Australian black walnut. Juice groove, hanging hole, and oiled finish included.", true, "{\"material\":\"Solid Australian Black Walnut\",\"dimensions_cm\":\"40 x 25 x 2\",\"finish\":\"Food-safe mineral oil\",\"features\":[\"Juice groove\",\"Hanging hole\"],\"care\":\"Hand wash only, re-oil monthly\",\"origin\":\"Australia\"}", "Solid Walnut Serving Board", 75.00m, "walnut-serving-board", 25, new[] { "wood", "walnut", "kitchen", "serving", "handmade", "sustainable" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000008"), 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "Pre-washed French linen that gets softer with every wash. Breathable and naturally thermoregulating for year-round comfort.", true, "{\"material\":\"100% French Linen\",\"size\":\"Queen (210x210cm)\",\"colors\":[\"White\",\"Sage\",\"Dusk Blue\",\"Natural\"],\"thread_count\":\"N/A (woven linen)\",\"care\":\"Machine wash 40°C, tumble dry low\",\"pillowcases_included\":2}", "Stonewashed Linen Duvet Cover — Queen", 189.00m, "linen-duvet-cover-queen", 18, new[] { "linen", "bedding", "duvet", "sustainable", "french-linen" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000009"), 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "Hand-stitched full-grain cowhide wallet with 6 card slots, a bill compartment, and RFID-blocking lining.", true, "{\"material\":\"Full-grain cowhide leather\",\"card_slots\":6,\"has_bill_compartment\":true,\"rfid_blocking\":true,\"colors\":[\"Tan\",\"Dark Brown\",\"Black\"],\"dimensions_cm\":\"9.5 x 11 (open)\",\"weight_g\":65}", "Full-Grain Leather Bi-Fold Wallet", 64.95m, "full-grain-leather-wallet", 55, new[] { "leather", "wallet", "accessories", "rfid", "handmade" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000010"), 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "12oz natural canvas tote with reinforced stitching, internal zip pocket, and a padded laptop sleeve up to 14\".", true, "{\"material\":\"12oz natural cotton canvas\",\"laptop_sleeve\":\"Up to 14\\\"\",\"internal_pockets\":2,\"external_pockets\":1,\"shoulder_strap_drop_cm\":28,\"capacity_litres\":20,\"colors\":[\"Natural\",\"Black\",\"Olive\"]}", "Heavy-Canvas Tote Bag", 39.00m, "canvas-tote-bag-natural", 90, new[] { "tote", "canvas", "sustainable", "laptop", "everyday" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000011"), 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "The classic guide to software craftsmanship — updated for modern development with new topics, tips, and exercises.", true, "{\"author\":\"David Thomas, Andrew Hunt\",\"publisher\":\"Pragmatic Bookshelf\",\"edition\":\"20th Anniversary\",\"pages\":352,\"isbn\":\"978-0135957059\",\"format\":[\"Paperback\",\"eBook\"],\"language\":\"English\",\"year\":2019}", "The Pragmatic Programmer (20th Anniversary Ed.)", 59.95m, "the-pragmatic-programmer-20th", 40, new[] { "programming", "software", "career", "bestseller" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000012"), 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AUD", "James Clear's #1 bestseller on building good habits and breaking bad ones — backed by science and packed with practical strategies.", true, "{\"author\":\"James Clear\",\"publisher\":\"Random House Business\",\"pages\":320,\"isbn\":\"978-1847941831\",\"format\":[\"Paperback\",\"Hardcover\",\"eBook\",\"Audiobook\"],\"language\":\"English\",\"year\":2018}", "Atomic Habits", 29.99m, "atomic-habits", 100, new[] { "self-help", "habits", "productivity", "bestseller" }, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_CartId",
                table: "cart_items",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_ProductId",
                table: "cart_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "idx_carts_session_id",
                table: "carts",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "idx_carts_user_id",
                table: "carts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cart_items");

            migrationBuilder.DropTable(
                name: "carts");

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
