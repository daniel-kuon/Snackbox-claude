using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class TabletBetaAndShelfSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "shelf_slot",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.InsertData(
                table: "feature_flags",
                columns: new[] { "id", "audience", "description", "key", "name", "updated_at" },
                values: new object[] { 3, 1, "Users with \"picks products on the tablet\" ticked choose their snacks on the tablet next to the scanner instead of paying the fixed amounts printed on their card.", "tablet", "Picking products on the tablet", new DateTime(2026, 10, 25, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_shelf_slot",
                table: "products",
                sql: "shelf_slot IS NULL OR (shelf_slot BETWEEN 1 AND 9 AND shelf_slot <> 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_shelf_slot",
                table: "products");

            migrationBuilder.DeleteData(
                table: "feature_flags",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "shelf_slot",
                table: "products");
        }
    }
}
