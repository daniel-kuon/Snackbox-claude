using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class WinterThemesAndThemeDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "first_purchase_only",
                table: "discounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "theme_key",
                table: "discounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.InsertData(
                table: "discounts",
                columns: new[] { "id", "created_at", "first_purchase_only", "is_active", "minimum_purchase_amount", "name", "theme_key", "type", "valid_from", "valid_to", "value" },
                values: new object[,]
                {
                    { 9001, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 0m, "Black Friday: 30 % off your first purchase", "black_friday", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2100, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30m },
                    { 9002, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 0m, "St. Nikolaus: 30 % off your first purchase", "nikolaus", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2100, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30m }
                });

            migrationBuilder.InsertData(
                table: "themes",
                columns: new[] { "id", "description", "end_day", "end_month", "key", "mode", "name", "start_day", "start_month", "updated_at" },
                values: new object[,]
                {
                    { 2, "Black and gold, neon and falling price tags - and 30 % off everyone's first purchase of the day.", 27, 11, "black_friday", 3, "Black Friday", 27, 11, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "St. Nikolaus with his sack, filled boots and snow - and 30 % off everyone's first purchase of the day. On a weekend it moves to the Monday after.", 6, 12, "nikolaus", 3, "St. Nikolaus", 6, 12, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Snow, a twinkling Christmas tree, presents and a snowman.", 26, 12, "christmas", 1, "Christmas", 1, 12, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Fireworks over the city, confetti and the new year's number.", 2, 1, "new_years_eve", 1, "New Year's Eve", 27, 12, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "discounts",
                keyColumn: "id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "discounts",
                keyColumn: "id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "first_purchase_only",
                table: "discounts");

            migrationBuilder.DropColumn(
                name: "theme_key",
                table: "discounts");
        }
    }
}
