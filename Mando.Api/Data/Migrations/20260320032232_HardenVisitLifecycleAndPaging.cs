using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenVisitLifecycleAndPaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_SalesRepId",
                table: "Visits");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_SalesRepId_OneActiveVisit",
                table: "Visits",
                column: "SalesRepId",
                unique: true,
                filter: "[Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_SalesRepId_OneActiveVisit",
                table: "Visits");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_SalesRepId",
                table: "Visits",
                column: "SalesRepId");
        }
    }
}
