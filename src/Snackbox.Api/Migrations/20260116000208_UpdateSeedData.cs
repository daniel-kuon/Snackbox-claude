using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "barcode_scans",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "barcode_scans",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "barcode_scans",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "payments",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "payments",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "product_barcodes",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "product_batches",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "product_batches",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "product_batches",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "product_batches",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "purchases",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "purchases",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "id", "best_before_in_stock", "best_before_on_shelf", "created_at", "name" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chips - Salt" },
                    { 2, null, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chocolate Bar" },
                    { 3, null, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Energy Drink" },
                    { 4, null, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cookies" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "created_at", "email", "is_admin", "password_hash", "username" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@snackbox.com", true, "$2a$11$7EW8wLqhqKQZH8J6rX5kQ.gBAGZERO3WEhOw84rLKyWNOe90gtIZi", "admin" },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "john.doe@company.com", false, "$2a$11$7EW8wLqhqKQZH8J6rX5kQ.nhwgnkeU3Z3ua5zEq5X5I6iMvqFFYkO", "john.doe" },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "jane.smith@company.com", false, null, "jane.smith" }
                });

            migrationBuilder.InsertData(
                table: "barcodes",
                columns: new[] { "id", "amount", "code", "created_at", "is_active", "is_login_only", "user_id" },
                values: new object[,]
                {
                    { 1, 5.00m, "4061461764012", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 2 },
                    { 2, 10.00m, "USER2-10EUR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 2 },
                    { 3, 5.00m, "USER3-5EUR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 3 },
                    { 4, 10.00m, "USER3-10EUR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 3 },
                    { 5, 0m, "4260473313809", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 1 },
                    { 6, 0m, "USER2-LOGIN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 2 },
                    { 7, 0m, "USER3-LOGIN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 3 }
                });

            migrationBuilder.InsertData(
                table: "payments",
                columns: new[] { "id", "admin_user_id", "amount", "invoice_id", "linked_deposit_id", "linked_withdrawal_id", "notes", "paid_at", "type", "user_id" },
                values: new object[,]
                {
                    { 1, null, 20.00m, null, null, null, "Initial payment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, 2 },
                    { 2, null, 15.00m, null, null, null, "Cash payment", new DateTime(2024, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), 0, 3 }
                });

            migrationBuilder.InsertData(
                table: "product_barcodes",
                columns: new[] { "id", "barcode", "created_at", "product_id", "quantity" },
                values: new object[,]
                {
                    { 1, "1234567890123", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1 },
                    { 2, "1234567890123-BOX", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 12 },
                    { 3, "1234567890124", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 1 },
                    { 4, "1234567890124-PACK", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 5 },
                    { 5, "1234567890125", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 1 },
                    { 6, "1234567890126", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 1 }
                });

            migrationBuilder.InsertData(
                table: "product_batches",
                columns: new[] { "id", "best_before_date", "created_at", "product_id" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, new DateTime(2025, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { 3, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { 4, new DateTime(2025, 5, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4 }
                });

            migrationBuilder.InsertData(
                table: "purchases",
                columns: new[] { "id", "completed_at", "created_at", "manual_amount", "reference_purchase_id", "type", "user_id" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 6, 0, 5, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 6, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, 2 },
                    { 2, new DateTime(2024, 1, 11, 0, 3, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, 3 }
                });

            migrationBuilder.InsertData(
                table: "barcode_scans",
                columns: new[] { "id", "amount", "barcode_id", "purchase_id", "scanned_at" },
                values: new object[,]
                {
                    { 1, 5.00m, 1, 1, new DateTime(2024, 1, 6, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 10.00m, 2, 1, new DateTime(2024, 1, 6, 0, 2, 0, 0, DateTimeKind.Utc) },
                    { 3, 5.00m, 3, 2, new DateTime(2024, 1, 11, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "shelving_actions",
                columns: new[] { "id", "action_at", "invoice_item_id", "product_batch_id", "quantity", "type" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 50, 0 },
                    { 2, new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 20, 2 },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, 30, 0 },
                    { 4, new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, 15, 2 },
                    { 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, 40, 0 },
                    { 6, new DateTime(2024, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, 25, 2 },
                    { 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, 35, 0 },
                    { 8, new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, 18, 2 }
                });
        }
    }
}
