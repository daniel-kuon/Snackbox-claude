using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBetaTesterAndFeatureAudience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_beta_tester",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "audience",
                table: "feature_flags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Convert the old on/off switch into an audience before dropping it:
            // enabled -> Everyone (2), disabled -> Disabled (0). Adding the column first and
            // backfilling keeps whatever was already switched on.
            migrationBuilder.Sql("UPDATE feature_flags SET audience = 2 WHERE is_enabled = TRUE;");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "feature_flags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_beta_tester",
                table: "users");

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "feature_flags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Everyone -> enabled; beta-only becomes disabled, the old model cannot express it
            migrationBuilder.Sql("UPDATE feature_flags SET is_enabled = TRUE WHERE audience = 2;");

            migrationBuilder.DropColumn(
                name: "audience",
                table: "feature_flags");
        }
    }
}
