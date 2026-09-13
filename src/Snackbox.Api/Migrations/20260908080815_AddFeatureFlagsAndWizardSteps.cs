using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlagsAndWizardSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_wizard_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    step_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_wizard_steps", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_wizard_steps_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "feature_flags",
                columns: new[] { "id", "description", "is_enabled", "key", "name", "updated_at" },
                values: new object[] { 1, "Show users how to install Snackbox as an app on their phone (install guide QR on the scan screen and the matching introduction step).", false, "mobile_app", "Snackbox on the phone", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_feature_flags_key",
                table: "feature_flags",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_wizard_steps_user_id_step_key",
                table: "user_wizard_steps",
                columns: new[] { "user_id", "step_key" },
                unique: true);

            // Backfill: users who already completed the old single-flag intro are treated as
            // having seen the steps that existed at that time. Steps added later (mobile_app,
            // and anything future) stay pending, so they are shown once without replaying the
            // whole introduction. Must run before has_seen_intro is dropped.
            migrationBuilder.Sql("""
                INSERT INTO user_wizard_steps (user_id, step_key, seen_at)
                SELECT u.id, s.step_key, NOW() AT TIME ZONE 'UTC'
                FROM users u
                CROSS JOIN (VALUES ('buying'), ('paying'), ('history')) AS s(step_key)
                WHERE u.has_seen_intro = TRUE;
                """);

            migrationBuilder.DropColumn(
                name: "has_seen_intro",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "user_wizard_steps");

            migrationBuilder.AddColumn<bool>(
                name: "has_seen_intro",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
