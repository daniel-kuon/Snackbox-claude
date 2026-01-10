using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class Invoices2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_users_created_by_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "invoices");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_users_created_by_id",
                table: "invoices",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_users_created_by_id",
                table: "invoices");

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_users_created_by_id",
                table: "invoices",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
