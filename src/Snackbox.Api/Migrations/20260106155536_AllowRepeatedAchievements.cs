using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AllowRepeatedAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_achievements_user_id_achievement_id",
                table: "user_achievements");

            migrationBuilder.AddColumn<decimal>(
                name: "debt_at_earning",
                table: "user_achievements",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "BIG_SPENDER_2", "Spent €2 or more in a single purchase", "Snack Nibbler" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "BIG_SPENDER_3", "Spent €3 or more in a single purchase", "Snack Attack!" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "BIG_SPENDER_4", "Spent €4 or more in a single purchase", "Hungry Hippo" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 12,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "IN_DEBT_15", "Unpaid balance of €15 or more", "Tab Starter" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 13,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "IN_DEBT_20", "Unpaid balance of €20 or more", "Credit Curious" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 14,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "IN_DEBT_25", "Unpaid balance of €25 or more", "Living on Credit" });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "id", "category", "code", "description", "image_url", "name" },
                values: new object[,]
                {
                    { 18, 0, "BIG_SPENDER_5", "Spent €5 or more in a single purchase", null, "Snack Hoarder" },
                    { 19, 0, "BIG_SPENDER_6", "Spent €6 or more in a single purchase", null, "The Whale" },
                    { 20, 1, "DAILY_BUYER_3", "Made 3 purchases in a single day", null, "Hat Trick" },
                    { 21, 2, "STREAK_DAILY_14", "Made a purchase 14 days in a row", null, "Fortnight Fanatic" },
                    { 22, 2, "STREAK_DAILY_30", "Made a purchase 30 days in a row", null, "Snack Addict" },
                    { 23, 4, "IN_DEBT_30", "Unpaid balance of €30 or more", null, "Debt Collector's Friend" },
                    { 24, 4, "IN_DEBT_35", "Unpaid balance of €35 or more", null, "Financial Freedom? Never Heard of It" },
                    { 25, 5, "TOTAL_SPENT_50", "Spent €50 or more in total", null, "First Fifty" },
                    { 26, 5, "TOTAL_SPENT_300", "Spent €300 or more in total", null, "Snack Royalty" },
                    { 27, 5, "TOTAL_SPENT_500", "Spent €500 or more in total", null, "Snack God" },
                    { 28, 6, "EARLY_BIRD", "Made a purchase before 8 AM", null, "Early Bird" },
                    { 29, 6, "NIGHT_OWL", "Made a purchase after 8 PM", null, "Night Owl" },
                    { 30, 6, "LUNCH_RUSH", "Made a purchase between 12-1 PM", null, "Lunch Rush Survivor" },
                    { 31, 6, "WEEKEND_WARRIOR", "Made a purchase on a Saturday or Sunday", null, "Weekend Warrior" },
                    { 32, 6, "MONDAY_BLUES", "Made a purchase on a Monday", null, "Monday Blues Cure" },
                    { 33, 6, "FRIDAY_TREAT", "Made a purchase on a Friday", null, "Friday Treat Yourself" },
                    { 34, 7, "FIRST_PURCHASE", "Made your first purchase", null, "Welcome to the Club!" },
                    { 35, 7, "PURCHASE_10", "Made 10 purchases total", null, "Regular Customer" },
                    { 36, 7, "PURCHASE_50", "Made 50 purchases total", null, "Snack Veteran" },
                    { 37, 7, "PURCHASE_100", "Made 100 purchases total", null, "Snack Centurion" },
                    { 38, 7, "PURCHASE_250", "Made 250 purchases total", null, "Snack Master" },
                    { 39, 7, "PURCHASE_500", "Made 500 purchases total", null, "Snack Overlord" },
                    { 40, 8, "SPEED_DEMON", "Made 2 scans within 3 seconds", null, "Speed Demon" },
                    { 41, 8, "DOUBLE_TROUBLE", "Made 2 or more scans in a session", null, "Double Trouble" },
                    { 42, 8, "TRIPLE_THREAT", "Made 3 or more scans in a session", null, "Triple Threat" },
                    { 43, 8, "LUCKY_SEVEN", "Made 7 or more scans in a session", null, "Lucky Seven" },
                    { 44, 8, "ROUND_NUMBER", "Made a purchase totaling exactly €5 or €10", null, "OCD Approved" },
                    { 45, 8, "SAME_AGAIN", "Made 3 identical purchases in a row", null, "Same Again, Please" },
                    { 46, 8, "PAID_UP", "Paid off your entire balance", null, "Debt Free!" },
                    { 47, 8, "GENEROUS_SOUL", "Have a positive balance (credit) of €10 or more", null, "Generous Soul" },
                    { 48, 8, "SNACK_BIRTHDAY", "Made a purchase exactly 1 year after your first", null, "Happy Snack-iversary!" },
                    { 49, 8, "THIRTEENTH", "Made a purchase totaling exactly €13", null, "Unlucky 13" },
                    { 50, 8, "NICE", "Made a purchase totaling exactly €6.90", null, "Nice." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_user_id_achievement_id_earned_at",
                table: "user_achievements",
                columns: new[] { "user_id", "achievement_id", "earned_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_achievements_user_id_achievement_id_earned_at",
                table: "user_achievements");

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 50);

            migrationBuilder.DropColumn(
                name: "debt_at_earning",
                table: "user_achievements");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "BIG_SPENDER_5", "Spent €5 or more in a single purchase", "Snack Attack!" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "BIG_SPENDER_10", "Spent €10 or more in a single purchase", "Hunger Games Champion" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "BIG_SPENDER_15", "Spent €15 or more in a single purchase", "Snack Hoarder" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 12,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "IN_DEBT_50", "Unpaid balance of €50 or more", "Credit Card Lifestyle" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 13,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "IN_DEBT_100", "Unpaid balance of €100 or more", "Financial Freedom? Never Heard of It" });

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 14,
                columns: new[] { "code", "description", "name" },
                values: new object[] { "IN_DEBT_150", "Unpaid balance of €150 or more", "Living on the Edge" });

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_user_id_achievement_id",
                table: "user_achievements",
                columns: new[] { "user_id", "achievement_id" },
                unique: true);
        }
    }
}
