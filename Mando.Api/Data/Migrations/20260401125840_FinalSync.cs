using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_CustomerId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId_Reference_Pending",
                table: "Payments",
                columns: new[] { "CustomerId", "Reference" },
                unique: true,
                filter: "[Status] = 1 AND [Reference] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_Latitude_Range",
                table: "Customers",
                sql: "[Latitude] >= -90 AND [Latitude] <= 90");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_Longitude_Range",
                table: "Customers",
                sql: "[Longitude] >= -180 AND [Longitude] <= 180");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_CustomerId_Reference_Pending",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_Latitude_Range",
                table: "Customers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_Longitude_Range",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId",
                table: "Payments",
                column: "CustomerId");
        }
    }
}
