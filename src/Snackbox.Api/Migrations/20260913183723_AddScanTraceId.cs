using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScanTraceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "trace_id",
                table: "barcode_scans",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trace_id",
                table: "barcode_scans");
        }
    }
}
