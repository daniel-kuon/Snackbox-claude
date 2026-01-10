using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdToInvoiceItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "product_id",
                table: "invoice_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_product_id",
                table: "invoice_items",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_items_products_product_id",
                table: "invoice_items",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoice_items_products_product_id",
                table: "invoice_items");

            migrationBuilder.DropIndex(
                name: "IX_invoice_items_product_id",
                table: "invoice_items");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "invoice_items");
        }
    }
}
