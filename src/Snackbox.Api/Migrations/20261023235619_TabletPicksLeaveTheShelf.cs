using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class TabletPicksLeaveTheShelf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "barcode_scan_id",
                table: "shelving_actions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_shelving_actions_barcode_scan_id",
                table: "shelving_actions",
                column: "barcode_scan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_shelving_actions_barcode_scans_barcode_scan_id",
                table: "shelving_actions",
                column: "barcode_scan_id",
                principalTable: "barcode_scans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shelving_actions_barcode_scans_barcode_scan_id",
                table: "shelving_actions");

            migrationBuilder.DropIndex(
                name: "IX_shelving_actions_barcode_scan_id",
                table: "shelving_actions");

            migrationBuilder.DropColumn(
                name: "barcode_scan_id",
                table: "shelving_actions");
        }
    }
}
