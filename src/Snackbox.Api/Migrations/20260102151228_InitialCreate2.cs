using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.UpdateData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 1,
                column: "code",
                value: "4061461764012");

            migrationBuilder.UpdateData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 5,
                column: "code",
                value: "4260473313809");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 1,
                column: "code",
                value: "USER2-5EUR");

            migrationBuilder.UpdateData(
                table: "barcodes",
                keyColumn: "id",
                keyValue: 5,
                column: "code",
                value: "ADMIN-LOGIN");
        }
    }
}
