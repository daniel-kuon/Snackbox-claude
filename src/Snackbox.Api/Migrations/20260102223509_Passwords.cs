using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class Passwords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1,
                column: "password_hash",
                value: "$2a$11$7EW8wLqhqKQZH8J6rX5kQ.gBAGZERO3WEhOw84rLKyWNOe90gtIZi");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 2,
                column: "password_hash",
                value: "$2a$11$7EW8wLqhqKQZH8J6rX5kQ.nhwgnkeU3Z3ua5zEq5X5I6iMvqFFYkO");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1,
                column: "password_hash",
                value: "$2a$11$7EW8wLqhqKQZH8J6rX5kQ.VzB4L5rZ5lYJ3VN2vY8K8eH5F0oJ8.G");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 2,
                column: "password_hash",
                value: "$2a$11$7EW8wLqhqKQZH8J6rX5kQ.VzB4L5rZ5lYJ3VN2vY8K8eH5F0oJ8.G");
        }
    }
}
