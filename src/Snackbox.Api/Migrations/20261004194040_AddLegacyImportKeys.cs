using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyImportKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "legacy_user_id",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "legacy_to_pay_id",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "legacy_code_id",
                table: "barcodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "legacy_posten_id",
                table: "barcode_scans",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_legacy_user_id",
                table: "users",
                column: "legacy_user_id",
                unique: true,
                filter: "legacy_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_legacy_to_pay_id",
                table: "payments",
                column: "legacy_to_pay_id",
                unique: true,
                filter: "legacy_to_pay_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_barcodes_legacy_code_id",
                table: "barcodes",
                column: "legacy_code_id",
                unique: true,
                filter: "legacy_code_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_barcode_scans_legacy_posten_id",
                table: "barcode_scans",
                column: "legacy_posten_id",
                unique: true,
                filter: "legacy_posten_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_legacy_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_payments_legacy_to_pay_id",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_barcodes_legacy_code_id",
                table: "barcodes");

            migrationBuilder.DropIndex(
                name: "IX_barcode_scans_legacy_posten_id",
                table: "barcode_scans");

            migrationBuilder.DropColumn(
                name: "legacy_user_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "legacy_to_pay_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "legacy_code_id",
                table: "barcodes");

            migrationBuilder.DropColumn(
                name: "legacy_posten_id",
                table: "barcode_scans");
        }
    }
}
