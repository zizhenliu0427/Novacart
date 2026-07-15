using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novacart.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase5DatabasePerService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "processed_stock_orders",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_stock_orders", x => x.OrderId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_stock_orders");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "orders");
        }
    }
}
