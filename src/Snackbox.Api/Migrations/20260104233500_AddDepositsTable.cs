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
                    table.PrimaryKey("pk_deposits", x => x.id);
                    table.ForeignKey(
                        name: "fk_deposits_payments_linked_payment_id",
                        column: x => x.linked_payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deposits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deposits_linked_payment_id",
                table: "deposits",
                column: "linked_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_user_id",
                table: "deposits",
                column: "user_id");

            migrationBuilder.AddColumn<int>(
                name: "linked_deposit_id",
                table: "payments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_linked_deposit_id",
                table: "payments",
                column: "linked_deposit_id");

            migrationBuilder.AddForeignKey(
                name: "fk_payments_deposits_linked_deposit_id",
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
                name: "fk_payments_deposits_linked_deposit_id",
                table: "payments");

            migrationBuilder.DropTable(
                name: "deposits");

            migrationBuilder.DropIndex(
                name: "ix_payments_linked_deposit_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "linked_deposit_id",
                table: "payments");
        }
    }
}
