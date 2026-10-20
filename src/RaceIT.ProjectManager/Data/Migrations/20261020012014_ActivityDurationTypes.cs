using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceIT.ProjectManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class ActivityDurationTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationType",
                table: "Activities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlannedDurationType",
                table: "Activities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Activity_DurationType",
                table: "Activities",
                sql: "[PlannedDurationType] IN (0,1) AND [EstimatedDurationType] IN (0,1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Activity_DurationType",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationType",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PlannedDurationType",
                table: "Activities");
        }
    }
}
