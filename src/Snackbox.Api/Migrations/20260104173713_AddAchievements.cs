using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    achievement_id = table.Column<int>(type: "integer", nullable: false),
                    earned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    has_been_shown = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievements", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_achievements_achievements_achievement_id",
                        column: x => x.achievement_id,
                        principalTable: "achievements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_achievements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "id", "category", "code", "description", "image_url", "name" },
                values: new object[,]
                {
                    { 1, 0, "BIG_SPENDER_5", "Spent €5 or more in a single purchase", null, "Snack Attack!" },
                    { 2, 0, "BIG_SPENDER_10", "Spent €10 or more in a single purchase", null, "Hunger Games Champion" },
                    { 3, 0, "BIG_SPENDER_15", "Spent €15 or more in a single purchase", null, "Snack Hoarder" },
                    { 4, 1, "DAILY_BUYER_5", "Made 5 or more purchases in a single day", null, "Frequent Flyer" },
                    { 5, 1, "DAILY_BUYER_10", "Made 10 or more purchases in a single day", null, "Snack Marathon" },
                    { 6, 2, "STREAK_DAILY_3", "Made a purchase 3 days in a row", null, "Three-peat" },
                    { 7, 2, "STREAK_DAILY_7", "Made a purchase 7 days in a row", null, "Week Warrior" },
                    { 8, 2, "STREAK_WEEKLY_4", "Made at least one purchase per week for 4 weeks", null, "Monthly Muncher" },
                    { 9, 3, "COMEBACK_30", "First purchase after 1 month away", null, "Long Time No See" },
                    { 10, 3, "COMEBACK_60", "First purchase after 2 months away", null, "The Return" },
                    { 11, 3, "COMEBACK_90", "First purchase after 3 months away", null, "Lazarus Rising" },
                    { 12, 4, "IN_DEBT_50", "Unpaid balance of €50 or more", null, "Credit Card Lifestyle" },
                    { 13, 4, "IN_DEBT_100", "Unpaid balance of €100 or more", null, "Financial Freedom? Never Heard of It" },
                    { 14, 4, "IN_DEBT_150", "Unpaid balance of €150 or more", null, "Living on the Edge" },
                    { 15, 5, "TOTAL_SPENT_100", "Spent €100 or more in total", null, "Century Club" },
                    { 16, 5, "TOTAL_SPENT_150", "Spent €150 or more in total", null, "Snack Connoisseur" },
                    { 17, 5, "TOTAL_SPENT_200", "Spent €200 or more in total", null, "Snackbox Legend" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_achievements_code",
                table: "achievements",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_achievement_id",
                table: "user_achievements",
                column: "achievement_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_user_id_achievement_id",
                table: "user_achievements",
                columns: new[] { "user_id", "achievement_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "achievements");
        }
    }
}
