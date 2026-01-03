using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBarcodeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_barcode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "barcode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "description",
                table: "products");

            migrationBuilder.DropColumn(
                name: "price",
                table: "products");

            migrationBuilder.AddColumn<DateTime>(
                name: "best_before_in_stock",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "best_before_on_shelf",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_barcodes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_barcodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_barcodes_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "product_barcodes",
                columns: new[] { "id", "barcode", "created_at", "is_active", "product_id", "quantity" },
                values: new object[,]
                {
                    { 1, "1234567890123", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1, 1 },
                    { 2, "1234567890123-BOX", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1, 12 },
                    { 3, "1234567890124", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, 1 },
                    { 4, "1234567890124-PACK", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, 5 },
                    { 5, "1234567890125", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, 1 },
                    { 6, "1234567890126", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 4, 1 }
                });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "best_before_in_stock", "best_before_on_shelf" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "best_before_in_stock", "best_before_on_shelf" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "best_before_in_stock", "best_before_on_shelf" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "best_before_in_stock", "best_before_on_shelf" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_barcode",
                table: "product_barcodes",
                column: "barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_product_id",
                table: "product_barcodes",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "best_before_in_stock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "best_before_on_shelf",
                table: "products");

            migrationBuilder.AddColumn<string>(
                name: "barcode",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "products",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "barcode", "description", "price" },
                values: new object[] { "1234567890123", "Classic salted potato chips", 1.50m });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "barcode", "description", "price" },
                values: new object[] { "1234567890124", "Milk chocolate bar", 2.00m });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "barcode", "description", "price" },
                values: new object[] { "1234567890125", "Sugar-free energy drink", 2.50m });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "barcode", "description", "price" },
                values: new object[] { "1234567890126", "Chocolate chip cookies", 1.75m });

            migrationBuilder.CreateIndex(
                name: "IX_products_barcode",
                table: "products",
                column: "barcode",
                unique: true);
        }
    }
}
