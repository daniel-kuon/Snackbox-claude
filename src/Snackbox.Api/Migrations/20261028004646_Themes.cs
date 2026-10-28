using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class Themes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "theme_key",
                table: "achievements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "themes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    start_month = table.Column<int>(type: "integer", nullable: false),
                    start_day = table.Column<int>(type: "integer", nullable: false),
                    end_month = table.Column<int>(type: "integer", nullable: false),
                    end_day = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_themes", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 1,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 2,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 3,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 4,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 5,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 6,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 7,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 8,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 9,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 10,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 11,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 12,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 13,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 14,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 15,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 16,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 17,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 18,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 19,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 20,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 21,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 22,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 23,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 24,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 25,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 26,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 27,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 28,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 29,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 30,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 31,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 32,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 33,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 34,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 35,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 36,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 37,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 38,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 39,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 40,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 41,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 42,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 43,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 44,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 45,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 46,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 47,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 48,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 49,
                column: "theme_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 50,
                column: "theme_key",
                value: null);

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "id", "category", "code", "description", "image_url", "name", "theme_key" },
                values: new object[,]
                {
                    { 51, 9, "HALLOWEEN_TREAT", "Bought a snack while Snackbox dressed up for Halloween", "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#2B1240\" stroke=\"#FF7518\" stroke-width=\"4\"/><text x=\"60\" y=\"62\" font-size=\"44\" text-anchor=\"middle\">🎃</text><text x=\"60\" y=\"94\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF7518\">BOO!</text></svg>", "Trick or Treat!", "halloween" },
                    { 52, 9, "HALLOWEEN_DAY", "Bought a snack on 31 October", "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#1A0B2E\" stroke=\"#FF7518\" stroke-width=\"4\"/><text x=\"60\" y=\"62\" font-size=\"44\" text-anchor=\"middle\">👻</text><text x=\"60\" y=\"94\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF7518\">31.10.</text></svg>", "Happy Halloween!", "halloween" },
                    { 53, 9, "HALLOWEEN_WITCHING_HOUR", "Bought a snack after 6 pm in Halloween week", "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#120A1C\" stroke=\"#FF7518\" stroke-width=\"4\"/><text x=\"60\" y=\"62\" font-size=\"44\" text-anchor=\"middle\">🧙</text><text x=\"60\" y=\"94\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF7518\">6 PM</text></svg>", "Witching Hour", "halloween" },
                    { 54, 9, "HALLOWEEN_MONSTER", "Made 5 purchases in Halloween week", "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#2B1240\" stroke=\"#FF7518\" stroke-width=\"4\"/><text x=\"60\" y=\"62\" font-size=\"44\" text-anchor=\"middle\">🧟</text><text x=\"60\" y=\"94\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF7518\">x5</text></svg>", "Monster Muncher", "halloween" }
                });

            migrationBuilder.InsertData(
                table: "themes",
                columns: new[] { "id", "description", "end_day", "end_month", "key", "mode", "name", "start_day", "start_month", "updated_at" },
                values: new object[] { 1, "Night sky, bats, ghosts, a haunted house and glowing pumpkins on the kiosk - and Halloween achievements.", 1, 11, "halloween", 1, "Halloween", 24, 10, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_themes_key",
                table: "themes",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "themes");

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 54);

            migrationBuilder.DropColumn(
                name: "theme_key",
                table: "achievements");
        }
    }
}
