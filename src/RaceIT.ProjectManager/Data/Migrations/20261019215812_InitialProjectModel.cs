using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceIT.ProjectManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialProjectModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Initiatives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Initiatives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Responsibles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responsibles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitiativeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WorkCalendarId = table.Column<int>(type: "int", nullable: false),
                    ResponsibleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Responsibles_ResponsibleId",
                        column: x => x.ResponsibleId,
                        principalTable: "Responsibles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_WorkCalendars_WorkCalendarId",
                        column: x => x.WorkCalendarId,
                        principalTable: "WorkCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkCalendarDays",
                columns: table => new
                {
                    WorkCalendarId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    WorkingHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendarDays", x => new { x.WorkCalendarId, x.DayOfWeek });
                    table.CheckConstraint("CK_CalendarDay_Day", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                    table.CheckConstraint("CK_CalendarDay_Hours", "[WorkingHours] >= 0 AND [WorkingHours] <= 24");
                    table.ForeignKey(
                        name: "FK_WorkCalendarDays_WorkCalendars_WorkCalendarId",
                        column: x => x.WorkCalendarId,
                        principalTable: "WorkCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResponsibleId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PlannedDurationHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    EstimatedDurationHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PlannedStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedFinish = table.Column<DateOnly>(type: "date", nullable: false),
                    EstimatedStart = table.Column<DateOnly>(type: "date", nullable: false),
                    EstimatedFinish = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualStart = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualFinish = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.CheckConstraint("CK_Activity_ActualDates", "[ActualFinish] IS NULL OR ([ActualStart] IS NOT NULL AND [ActualFinish] >= [ActualStart])");
                    table.CheckConstraint("CK_Activity_Duration", "[PlannedDurationHours] > 0 AND [EstimatedDurationHours] > 0");
                    table.CheckConstraint("CK_Activity_EstimatedDates", "[EstimatedFinish] >= [EstimatedStart]");
                    table.CheckConstraint("CK_Activity_PlanDates", "[PlannedFinish] >= [PlannedStart]");
                    table.CheckConstraint("CK_Activity_Status", "[Status] IN (0,1,2)");
                    table.ForeignKey(
                        name: "FK_Activities_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Activities_Responsibles_ResponsibleId",
                        column: x => x.ResponsibleId,
                        principalTable: "Responsibles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EstimatedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ActivityDependencies",
                columns: table => new
                {
                    PredecessorId = table.Column<int>(type: "int", nullable: false),
                    SuccessorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDependencies", x => new { x.PredecessorId, x.SuccessorId });
                    table.CheckConstraint("CK_Dependency_NotSelf", "[PredecessorId] <> [SuccessorId]");
                    table.ForeignKey(
                        name: "FK_ActivityDependencies_Activities_PredecessorId",
                        column: x => x.PredecessorId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActivityDependencies_Activities_SuccessorId",
                        column: x => x.SuccessorId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Blockers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResponsibleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blockers", x => x.Id);
                    table.CheckConstraint("CK_Blocker_Dates", "[ResolvedAt] IS NULL OR [ResolvedAt] >= [CreatedAt]");
                    table.ForeignKey(
                        name: "FK_Blockers_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Blockers_Responsibles_ResponsibleId",
                        column: x => x.ResponsibleId,
                        principalTable: "Responsibles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MilestoneRequirements",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    ApprovedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneRequirements", x => new { x.MilestoneId, x.ActivityId });
                    table.ForeignKey(
                        name: "FK_MilestoneRequirements_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MilestoneRequirements_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MilestoneRequirements_Responsibles_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Responsibles",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "WorkCalendars",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Lunes a viernes - 40 horas" },
                    { 2, "Lunes a sabado - 45 horas" }
                });

            migrationBuilder.InsertData(
                table: "WorkCalendarDays",
                columns: new[] { "DayOfWeek", "WorkCalendarId", "WorkingHours" },
                values: new object[,]
                {
                    { 0, 1, 0m },
                    { 1, 1, 8m },
                    { 2, 1, 8m },
                    { 3, 1, 8m },
                    { 4, 1, 8m },
                    { 5, 1, 8m },
                    { 6, 1, 0m },
                    { 0, 2, 0m },
                    { 1, 2, 8m },
                    { 2, 2, 8m },
                    { 3, 2, 8m },
                    { 4, 2, 8m },
                    { 5, 2, 8m },
                    { 6, 2, 5m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ProjectId",
                table: "Activities",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ResponsibleId",
                table: "Activities",
                column: "ResponsibleId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDependencies_SuccessorId",
                table: "ActivityDependencies",
                column: "SuccessorId");

            migrationBuilder.CreateIndex(
                name: "IX_Blockers_ActivityId",
                table: "Blockers",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Blockers_ResponsibleId",
                table: "Blockers",
                column: "ResponsibleId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneRequirements_ActivityId",
                table: "MilestoneRequirements",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneRequirements_ApprovedById",
                table: "MilestoneRequirements",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId",
                table: "Milestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_InitiativeId",
                table: "Projects",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ResponsibleId",
                table: "Projects",
                column: "ResponsibleId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkCalendarId",
                table: "Projects",
                column: "WorkCalendarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDependencies");

            migrationBuilder.DropTable(
                name: "Blockers");

            migrationBuilder.DropTable(
                name: "MilestoneRequirements");

            migrationBuilder.DropTable(
                name: "WorkCalendarDays");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Initiatives");

            migrationBuilder.DropTable(
                name: "Responsibles");

            migrationBuilder.DropTable(
                name: "WorkCalendars");
        }
    }
}
