using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "invoice_item_id",
                table: "shelving_actions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invoice_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    additional_costs = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoices_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    invoice_id = table.Column<int>(type: "integer", nullable: false),
                    product_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    best_before_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    article_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_items_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 1,
                column: "invoice_item_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 2,
                column: "invoice_item_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 3,
                column: "invoice_item_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 4,
                column: "invoice_item_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 5,
                column: "invoice_item_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 6,
                column: "invoice_item_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 7,
                column: "invoice_item_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 8,
                column: "invoice_item_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_shelving_actions_invoice_item_id",
                table: "shelving_actions",
                column: "invoice_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_invoice_id",
                table: "invoice_items",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_created_by_id",
                table: "invoices",
                column: "created_by_id");

            migrationBuilder.AddForeignKey(
                name: "FK_shelving_actions_invoice_items_invoice_item_id",
                table: "shelving_actions",
                column: "invoice_item_id",
                principalTable: "invoice_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shelving_actions_invoice_items_invoice_item_id",
                table: "shelving_actions");

            migrationBuilder.DropTable(
                name: "invoice_items");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_shelving_actions_invoice_item_id",
                table: "shelving_actions");

            migrationBuilder.DropColumn(
                name: "invoice_item_id",
                table: "shelving_actions");
        }
    }
}
