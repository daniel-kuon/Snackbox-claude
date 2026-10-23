using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTabletAndProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "uses_tablet_selection",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "products",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_id",
                table: "barcode_scans",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.product_id);
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_barcode_scans_product_id",
                table: "barcode_scans",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_barcode_scans_products_product_id",
                table: "barcode_scans",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_barcode_scans_products_product_id",
                table: "barcode_scans");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropIndex(
                name: "IX_barcode_scans_product_id",
                table: "barcode_scans");

            migrationBuilder.DropColumn(
                name: "uses_tablet_selection",
                table: "users");

            migrationBuilder.DropColumn(
                name: "price",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "barcode_scans");
        }
    }
}
