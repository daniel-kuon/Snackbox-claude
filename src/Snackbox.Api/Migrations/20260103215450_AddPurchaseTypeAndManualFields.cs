using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseTypeAndManualFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "manual_amount",
                table: "purchases",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reference_purchase_id",
                table: "purchases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "purchases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "purchases",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "manual_amount", "reference_purchase_id", "type" },
                values: new object[] { null, null, 0 });

            migrationBuilder.UpdateData(
                table: "purchases",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "manual_amount", "reference_purchase_id", "type" },
                values: new object[] { null, null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_purchases_reference_purchase_id",
                table: "purchases",
                column: "reference_purchase_id");

            migrationBuilder.AddForeignKey(
                name: "FK_purchases_purchases_reference_purchase_id",
                table: "purchases",
                column: "reference_purchase_id",
                principalTable: "purchases",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchases_purchases_reference_purchase_id",
                table: "purchases");

            migrationBuilder.DropIndex(
                name: "IX_purchases_reference_purchase_id",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "manual_amount",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "reference_purchase_id",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "type",
                table: "purchases");
        }
    }
}
