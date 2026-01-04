#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 2,
                column: "type",
                value: 2);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 4,
                column: "type",
                value: 2);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 6,
                column: "type",
                value: 2);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 8,
                column: "type",
                value: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 2,
                column: "type",
                value: 1);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 4,
                column: "type",
                value: 1);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 6,
                column: "type",
                value: 1);

            migrationBuilder.UpdateData(
                table: "shelving_actions",
                keyColumn: "id",
                keyValue: 8,
                column: "type",
                value: 1);
        }
    }
}
