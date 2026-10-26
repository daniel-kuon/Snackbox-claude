using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class PayPalPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "paypal_payers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payer_key = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paypal_payers", x => x.id);
                    table.ForeignKey(
                        name: "FK_paypal_payers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paypal_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payer_key = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    payer_name = table.Column<string>(type: "text", nullable: true),
                    payer_email = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    payment_id = table.Column<int>(type: "integer", nullable: true),
                    dismissed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paypal_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_paypal_transactions_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_paypal_payers_payer_key",
                table: "paypal_payers",
                column: "payer_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paypal_payers_user_id",
                table: "paypal_payers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_paypal_transactions_payer_key",
                table: "paypal_transactions",
                column: "payer_key");

            migrationBuilder.CreateIndex(
                name: "IX_paypal_transactions_payment_id",
                table: "paypal_transactions",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_paypal_transactions_transaction_id",
                table: "paypal_transactions",
                column: "transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paypal_payers");

            migrationBuilder.DropTable(
                name: "paypal_transactions");
        }
    }
}
