using Microsoft.EntityFrameworkCore.Migrations;
using Snackbox.Api.Models;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class SplitBarcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_users_created_by_id",
                table: "invoices");

            migrationBuilder.RenameColumn(
                name: "created_by_id",
                table: "invoices",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_invoices_created_by_id",
                table: "invoices",
                newName: "IX_invoices_created_by_user_id");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "barcodes",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            //language=sql
            migrationBuilder.Sql(
                                 $"""
                                 UPDATE barcodes
                                                   SET "Discriminator" = CASE 
                                                     WHEN is_login_only THEN '{nameof(LoginBarcode)}' 
                                                     ELSE '{nameof(PurchaseBarcode)}' 
                                                   END;
                                 """
                                );

            migrationBuilder.DropColumn(
                                        name: "is_login_only",
                                        table: "barcodes");


            migrationBuilder.AddForeignKey(
                name: "FK_invoices_users_created_by_user_id",
                table: "invoices",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_users_created_by_user_id",
                table: "invoices");
            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "invoices",
                newName: "created_by_id");

            migrationBuilder.RenameIndex(
                name: "IX_invoices_created_by_user_id",
                table: "invoices",
                newName: "IX_invoices_created_by_id");

            migrationBuilder.AddColumn<bool>(
                name: "is_login_only",
                table: "barcodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            //language=sql
            migrationBuilder.Sql($"""
                                  UPDATE barcodes
                                                    SET "Discriminator" = CASE 
                                                      WHEN is_login_only THEN '{nameof(LoginBarcode)}' 
                                                      ELSE '{nameof(PurchaseBarcode)}' 
                                                    END;
                                  """);

            migrationBuilder.DropColumn(
                                        name: "Discriminator",
                                        table: "barcodes");



            migrationBuilder.AddForeignKey(
                name: "FK_invoices_users_created_by_id",
                table: "invoices",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
