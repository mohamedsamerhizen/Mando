using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckInAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckInLatitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    CheckInLongitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    CheckOutAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutLatitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    CheckOutLongitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visits_AspNetUsers_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visits_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CheckInAtUtc",
                table: "Visits",
                column: "CheckInAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CustomerId",
                table: "Visits",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_SalesRepId",
                table: "Visits",
                column: "SalesRepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Visits");
        }
    }
}
