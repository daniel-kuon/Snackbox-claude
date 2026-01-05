using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "linked_deposit_id",
                table: "payments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "deposits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    deposited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    linked_payment_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposits", x => x.id);
                    table.ForeignKey(
                        name: "FK_deposits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "payments",
                keyColumn: "id",
                keyValue: 1,
                column: "linked_deposit_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "payments",
                keyColumn: "id",
                keyValue: 2,
                column: "linked_deposit_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_payments_linked_deposit_id",
                table: "payments",
                column: "linked_deposit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deposits_user_id",
                table: "deposits",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_deposits_linked_deposit_id",
                table: "payments",
                column: "linked_deposit_id",
                principalTable: "deposits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_deposits_linked_deposit_id",
                table: "payments");

            migrationBuilder.DropTable(
                name: "deposits");

            migrationBuilder.DropIndex(
                name: "IX_payments_linked_deposit_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "linked_deposit_id",
                table: "payments");
        }
    }
}
