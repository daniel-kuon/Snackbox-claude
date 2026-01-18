using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class InactiveUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 40,
                column: "description",
                value: "Made 2 scans within 3 seconds");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 41,
                column: "description",
                value: "Made 2 or more scans in a session");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 42,
                column: "description",
                value: "Made 3 or more scans in a session");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 43,
                column: "description",
                value: "Made 7 or more scans in a session");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 40,
                column: "description",
                value: "Made 2 purchases within 1 minute");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 41,
                column: "description",
                value: "Made exactly 2 purchases in a session");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 42,
                column: "description",
                value: "Made exactly 3 purchases in a session");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 43,
                column: "description",
                value: "Made exactly 7 purchases in a session");
        }
    }
}
