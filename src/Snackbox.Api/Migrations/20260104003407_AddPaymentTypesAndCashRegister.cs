using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTypesAndCashRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "admin_user_id",
                table: "payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "linked_withdrawal_id",
                table: "payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "cash_register",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    current_balance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated_by_user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_register", x => x.id);
                    table.ForeignKey(
                        name: "FK_cash_register_users_last_updated_by_user_id",
                        column: x => x.last_updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "withdrawals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    withdrawn_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    linked_payment_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_withdrawals", x => x.id);
                    table.ForeignKey(
                        name: "FK_withdrawals_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "payments",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "admin_user_id", "linked_withdrawal_id", "type" },
                values: new object[] { null, null, 0 });

            migrationBuilder.UpdateData(
                table: "payments",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "admin_user_id", "linked_withdrawal_id", "type" },
                values: new object[] { null, null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_payments_admin_user_id",
                table: "payments",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_last_updated_by_user_id",
                table: "cash_register",
                column: "last_updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawals_user_id",
                table: "withdrawals",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_users_admin_user_id",
                table: "payments",
                column: "admin_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_users_admin_user_id",
                table: "payments");

            migrationBuilder.DropTable(
                name: "cash_register");

            migrationBuilder.DropTable(
                name: "withdrawals");

            migrationBuilder.DropIndex(
                name: "IX_payments_admin_user_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "admin_user_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "linked_withdrawal_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "type",
                table: "payments");
        }
    }
}
