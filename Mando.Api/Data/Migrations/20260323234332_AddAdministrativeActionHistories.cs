using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrativeActionHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerActionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    PreviousName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreviousCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NewCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    PreviousAssignedSalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousAssignedSalesRepName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewAssignedSalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewAssignedSalesRepName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreviousCreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewCreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousOpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewOpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByUserFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerActionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerActionHistories_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerActionHistories_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductActionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    PreviousName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreviousCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NewCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreviousUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByUserFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductActionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductActionHistories_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductActionHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserActionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    FullNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmailSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RolesSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviousIsActive = table.Column<bool>(type: "bit", nullable: true),
                    NewIsActive = table.Column<bool>(type: "bit", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByUserFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActionHistories_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserActionHistories_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerActionHistories_CustomerId_ActionAtUtc",
                table: "CustomerActionHistories",
                columns: new[] { "CustomerId", "ActionAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerActionHistories_PerformedByUserId",
                table: "CustomerActionHistories",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductActionHistories_PerformedByUserId",
                table: "ProductActionHistories",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductActionHistories_ProductId_ActionAtUtc",
                table: "ProductActionHistories",
                columns: new[] { "ProductId", "ActionAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserActionHistories_PerformedByUserId",
                table: "UserActionHistories",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActionHistories_TargetUserId_ActionAtUtc",
                table: "UserActionHistories",
                columns: new[] { "TargetUserId", "ActionAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerActionHistories");

            migrationBuilder.DropTable(
                name: "ProductActionHistories");

            migrationBuilder.DropTable(
                name: "UserActionHistories");
        }
    }
}
