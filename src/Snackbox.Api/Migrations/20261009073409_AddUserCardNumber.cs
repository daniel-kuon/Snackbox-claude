using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCardNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "card_number",
                table: "users",
                type: "integer",
                nullable: true);

            // Cards that already exist ("Karte 05", imported from the old Snackbox) get their
            // number, so the card wizard sees them as taken. A number two users would share is
            // left out rather than failing the unique index below.
            migrationBuilder.Sql("""
                UPDATE users u
                SET card_number = n.num
                FROM (SELECT id,
                             CAST(substring(username FROM '[0-9]+') AS integer) AS num,
                             count(*) OVER (PARTITION BY CAST(substring(username FROM '[0-9]+') AS integer)) AS shared
                      FROM users
                      WHERE username ~ '^Karte [0-9]+$') n
                WHERE u.id = n.id AND n.shared = 1 AND n.num > 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_users_card_number",
                table: "users",
                column: "card_number",
                unique: true,
                filter: "card_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_card_number",
                table: "users");

            migrationBuilder.DropColumn(
                name: "card_number",
                table: "users");
        }
    }
}
