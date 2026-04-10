using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLifecycleDiscipline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SalesRepId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "OrderActionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByUserFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BalanceBeforeAction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BalanceAfterAction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderActionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderActionHistories_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderActionHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CancelledByUserId",
                table: "Orders",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_Status",
                table: "Orders",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SalesRepId_Status",
                table: "Orders",
                columns: new[] { "SalesRepId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderActionHistories_OrderId_ActionAtUtc",
                table: "OrderActionHistories",
                columns: new[] { "OrderId", "ActionAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderActionHistories_PerformedByUserId",
                table: "OrderActionHistories",
                column: "PerformedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_CancelledByUserId",
                table: "Orders",
                column: "CancelledByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_CancelledByUserId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderActionHistories");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CancelledByUserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SalesRepId_Status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SalesRepId",
                table: "Orders",
                column: "SalesRepId");
        }
    }
}
