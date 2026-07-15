using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novacart.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCheckoutSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_checkout_sagas",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PublishDownstream = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_checkout_sagas", x => x.CorrelationId);
                });

            migrationBuilder.CreateIndex(
                name: "idx_order_checkout_sagas_order_id",
                table: "order_checkout_sagas",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_checkout_sagas");
        }
    }
}
