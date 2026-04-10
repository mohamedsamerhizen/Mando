using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitAttemptLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitAttemptLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccuracyInMeters = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DistanceFromCustomerInMeters = table.Column<double>(type: "float", nullable: false),
                    ComplianceStatus = table.Column<int>(type: "int", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitAttemptLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitAttemptLogs_AspNetUsers_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisitAttemptLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitAttemptLogs_CustomerId",
                table: "VisitAttemptLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitAttemptLogs_SalesRepId",
                table: "VisitAttemptLogs",
                column: "SalesRepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitAttemptLogs");
        }
    }
}
