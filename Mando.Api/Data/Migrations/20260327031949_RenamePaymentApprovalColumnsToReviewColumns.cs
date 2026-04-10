using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaymentApprovalColumnsToReviewColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_ApprovedByUserId",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "ApprovedByUserId",
                table: "Payments",
                newName: "ReviewedByUserId");

            migrationBuilder.RenameColumn(
                name: "ApprovedAtUtc",
                table: "Payments",
                newName: "ReviewedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_ApprovedByUserId",
                table: "Payments",
                newName: "IX_Payments_ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_ReviewedByUserId",
                table: "Payments",
                column: "ReviewedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_ReviewedByUserId",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "ReviewedByUserId",
                table: "Payments",
                newName: "ApprovedByUserId");

            migrationBuilder.RenameColumn(
                name: "ReviewedAtUtc",
                table: "Payments",
                newName: "ApprovedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_ReviewedByUserId",
                table: "Payments",
                newName: "IX_Payments_ApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_ApprovedByUserId",
                table: "Payments",
                column: "ApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
