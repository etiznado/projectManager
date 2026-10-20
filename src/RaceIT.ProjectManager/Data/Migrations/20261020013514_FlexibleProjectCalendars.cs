using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceIT.ProjectManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class FlexibleProjectCalendars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Activity_Duration",
                table: "Activities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Activity_DurationType",
                table: "Activities");

            migrationBuilder.AddColumn<bool>(
                name: "IsFlexible",
                table: "WorkCalendars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RequestedFinish",
                table: "Activities",
                type: "date",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "WorkCalendars",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsFlexible",
                value: false);

            migrationBuilder.UpdateData(
                table: "WorkCalendars",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsFlexible",
                value: false);

            migrationBuilder.InsertData(
                table: "WorkCalendars",
                columns: new[] { "Id", "IsFlexible", "Name" },
                values: new object[] { 3, true, "Flexible / sin horario" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Activity_Duration",
                table: "Activities",
                sql: "(([PlannedDurationType] = 2 AND [PlannedDurationHours] = 0) OR ([PlannedDurationType] <> 2 AND [PlannedDurationHours] > 0)) AND (([EstimatedDurationType] = 2 AND [EstimatedDurationHours] = 0) OR ([EstimatedDurationType] <> 2 AND [EstimatedDurationHours] > 0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Activity_DurationType",
                table: "Activities",
                sql: "[PlannedDurationType] IN (0,1,2) AND [EstimatedDurationType] IN (0,1,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Activity_Duration",
                table: "Activities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Activity_DurationType",
                table: "Activities");

            migrationBuilder.DeleteData(
                table: "WorkCalendars",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "IsFlexible",
                table: "WorkCalendars");

            migrationBuilder.DropColumn(
                name: "RequestedFinish",
                table: "Activities");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Activity_Duration",
                table: "Activities",
                sql: "[PlannedDurationHours] > 0 AND [EstimatedDurationHours] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Activity_DurationType",
                table: "Activities",
                sql: "[PlannedDurationType] IN (0,1) AND [EstimatedDurationType] IN (0,1)");
        }
    }
}
