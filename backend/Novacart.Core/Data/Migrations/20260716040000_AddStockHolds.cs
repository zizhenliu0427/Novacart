using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novacart.Api.Data.Migrations;

/// <inheritdoc />
public partial class AddStockHolds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "stock_holds",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stock_holds", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "idx_stock_holds_order_id",
            table: "stock_holds",
            column: "order_id");

        migrationBuilder.CreateIndex(
            name: "idx_stock_holds_product_active",
            table: "stock_holds",
            columns: new[] { "product_id", "status", "expires_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "stock_holds");
    }
}
