using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductBatchIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shelving_actions_product_batches_product_batch_id",
                table: "shelving_actions");

            migrationBuilder.AlterColumn<int>(
                name: "product_batch_id",
                table: "shelving_actions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_shelving_actions_product_batches_product_batch_id",
                table: "shelving_actions",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shelving_actions_product_batches_product_batch_id",
                table: "shelving_actions");

            migrationBuilder.AlterColumn<int>(
                name: "product_batch_id",
                table: "shelving_actions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_shelving_actions_product_batches_product_batch_id",
                table: "shelving_actions",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
