using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddKioskBackgroundSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "feature_flags",
                columns: new[] { "id", "audience", "description", "key", "name", "updated_at" },
                values: new object[] { 2, 0, "The kiosk window stays minimized and is never pulled to the front by a scan (parallel run with the old Snackbox). Scans are still recorded.", "kiosk_background", "Kiosk in the background", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "feature_flags",
                keyColumn: "id",
                keyValue: 2);
        }
    }
}
