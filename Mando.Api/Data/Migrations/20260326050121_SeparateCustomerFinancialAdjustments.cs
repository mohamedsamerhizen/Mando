using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateCustomerFinancialAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationsAlertReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AlertFingerprint = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShortReasonSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedByUserFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationsAlertReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationsAlertReviews_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationsAlertReviews_AlertFingerprint_ReviewedAtUtc",
                table: "OperationsAlertReviews",
                columns: new[] { "AlertFingerprint", "ReviewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationsAlertReviews_AlertKey_ReviewedAtUtc",
                table: "OperationsAlertReviews",
                columns: new[] { "AlertKey", "ReviewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationsAlertReviews_Category_EntityType_EntityId",
                table: "OperationsAlertReviews",
                columns: new[] { "Category", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationsAlertReviews_ReviewedByUserId",
                table: "OperationsAlertReviews",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationsAlertReviews");
        }
    }
}
