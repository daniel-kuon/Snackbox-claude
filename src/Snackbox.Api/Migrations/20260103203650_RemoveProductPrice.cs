#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "product_barcodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "product_barcodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 1,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 2,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 3,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 4,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 5,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 6,
                column: "is_active",
                value: true);
        }
    }
}
