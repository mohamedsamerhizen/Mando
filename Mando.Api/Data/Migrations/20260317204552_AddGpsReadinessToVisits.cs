using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGpsReadinessToVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CheckInAccuracyInMeters",
                table: "Visits",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckOutAccuracyInMeters",
                table: "Visits",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DistanceFromCustomerInMeters",
                table: "Visits",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInAccuracyInMeters",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CheckOutAccuracyInMeters",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "DistanceFromCustomerInMeters",
                table: "Visits");
        }
    }
}
