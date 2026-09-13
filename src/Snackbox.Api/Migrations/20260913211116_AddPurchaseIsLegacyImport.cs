using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseIsLegacyImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_legacy_import",
                table: "purchases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Everything already in the table at this point predates going live with the new
            // Snackbox, so it is carried-over history: it keeps counting towards money spent
            // and streaks, but not towards purchase-count milestones.
            migrationBuilder.Sql("UPDATE purchases SET is_legacy_import = TRUE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_legacy_import",
                table: "purchases");
        }
    }
}
