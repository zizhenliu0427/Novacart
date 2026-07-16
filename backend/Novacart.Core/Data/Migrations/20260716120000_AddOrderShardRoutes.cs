using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novacart.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShardRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_shard_routes",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShardIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_shard_routes", x => x.OrderId);
                });

            migrationBuilder.CreateIndex(
                name: "idx_order_shard_routes_shard_index",
                table: "order_shard_routes",
                column: "ShardIndex");

            migrationBuilder.CreateIndex(
                name: "idx_order_shard_routes_user_id",
                table: "order_shard_routes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_shard_routes");
        }
    }
}
