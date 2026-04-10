using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixVisitAttemptLogsDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VisitAttemptLogs_AspNetUsers_SalesRepId",
                table: "VisitAttemptLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitAttemptLogs_Customers_CustomerId",
                table: "VisitAttemptLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_AspNetUsers_SalesRepId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Customers_CustomerId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_CheckInAtUtc",
                table: "Visits");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckOutLongitude",
                table: "Visits",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckOutLatitude",
                table: "Visits",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckOutAccuracyInMeters",
                table: "Visits",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckInLongitude",
                table: "Visits",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckInLatitude",
                table: "Visits",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckInAccuracyInMeters",
                table: "Visits",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "VisitAttemptLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "VisitAttemptLogs",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "VisitAttemptLogs",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AccuracyInMeters",
                table: "VisitAttemptLogs",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_VisitAttemptLogs_ComplianceStatus",
                table: "VisitAttemptLogs",
                column: "ComplianceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_VisitAttemptLogs_CreatedAtUtc",
                table: "VisitAttemptLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VisitAttemptLogs_IsSuccessful",
                table: "VisitAttemptLogs",
                column: "IsSuccessful");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitAttemptLogs_AspNetUsers_SalesRepId",
                table: "VisitAttemptLogs",
                column: "SalesRepId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitAttemptLogs_Customers_CustomerId",
                table: "VisitAttemptLogs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_AspNetUsers_SalesRepId",
                table: "Visits",
                column: "SalesRepId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Customers_CustomerId",
                table: "Visits",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VisitAttemptLogs_AspNetUsers_SalesRepId",
                table: "VisitAttemptLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitAttemptLogs_Customers_CustomerId",
                table: "VisitAttemptLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_AspNetUsers_SalesRepId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Customers_CustomerId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_VisitAttemptLogs_ComplianceStatus",
                table: "VisitAttemptLogs");

            migrationBuilder.DropIndex(
                name: "IX_VisitAttemptLogs_CreatedAtUtc",
                table: "VisitAttemptLogs");

            migrationBuilder.DropIndex(
                name: "IX_VisitAttemptLogs_IsSuccessful",
                table: "VisitAttemptLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Visits",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckOutLongitude",
                table: "Visits",
                type: "decimal(9,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckOutLatitude",
                table: "Visits",
                type: "decimal(9,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckOutAccuracyInMeters",
                table: "Visits",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckInLongitude",
                table: "Visits",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckInLatitude",
                table: "Visits",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CheckInAccuracyInMeters",
                table: "Visits",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "VisitAttemptLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "VisitAttemptLogs",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "VisitAttemptLogs",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AccuracyInMeters",
                table: "VisitAttemptLogs",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CheckInAtUtc",
                table: "Visits",
                column: "CheckInAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitAttemptLogs_AspNetUsers_SalesRepId",
                table: "VisitAttemptLogs",
                column: "SalesRepId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitAttemptLogs_Customers_CustomerId",
                table: "VisitAttemptLogs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_AspNetUsers_SalesRepId",
                table: "Visits",
                column: "SalesRepId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Customers_CustomerId",
                table: "Visits",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
