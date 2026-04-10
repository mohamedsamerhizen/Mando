using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mando.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncVisitMediaModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlotNumber",
                table: "VisitImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VisitImages_VisitId_SlotNumber",
                table: "VisitImages",
                columns: new[] { "VisitId", "SlotNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VisitImages_SlotNumber_Range",
                table: "VisitImages",
                sql: "[SlotNumber] >= 1 AND [SlotNumber] <= 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitImages_VisitId_SlotNumber",
                table: "VisitImages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VisitImages_SlotNumber_Range",
                table: "VisitImages");

            migrationBuilder.DropColumn(
                name: "SlotNumber",
                table: "VisitImages");
        }
    }
}
