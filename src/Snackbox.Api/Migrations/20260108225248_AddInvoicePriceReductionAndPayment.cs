using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePriceReductionAndPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "invoice_id",
                table: "payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "paid_by_user_id",
                table: "invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "payment_id",
                table: "invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_reduction",
                table: "invoices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "payments",
                keyColumn: "id",
                keyValue: 1,
                column: "invoice_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "payments",
                keyColumn: "id",
                keyValue: 2,
                column: "invoice_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_paid_by_user_id",
                table: "invoices",
                column: "paid_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_payment_id",
                table: "invoices",
                column: "payment_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_payments_payment_id",
                table: "invoices",
                column: "payment_id",
                principalTable: "payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_users_paid_by_user_id",
                table: "invoices",
                column: "paid_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_payments_payment_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_invoices_users_paid_by_user_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_paid_by_user_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_payment_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "paid_by_user_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "payment_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "price_reduction",
                table: "invoices");
        }
    }
}
