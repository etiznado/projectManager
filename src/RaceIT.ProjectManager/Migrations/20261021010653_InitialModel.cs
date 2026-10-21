using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceIT.ProjectManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialModel : Migration
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
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Initiatives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    PlanningMode = table.Column<int>(type: "int", nullable: false),
                    WeeklyCapacityHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                    table.CheckConstraint("CK_Person_Capacity", "[WeeklyCapacityHours] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonWorkdays",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    WorkingHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonWorkdays", x => new { x.PersonId, x.DayOfWeek });
                    table.CheckConstraint("CK_PersonWorkday_Day", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                    table.CheckConstraint("CK_PersonWorkday_Hours", "[WorkingHours] >= 0 AND [WorkingHours] <= 24");
                    table.ForeignKey(
                        name: "FK_PersonWorkdays_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitiativeId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PersonId = table.Column<int>(type: "int", nullable: true)
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
                        name: "FK_Projects_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WeeklyPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyPlans_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateItems_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrentEstimateHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.CheckConstraint("CK_Activity_Estimate", "[CurrentEstimateHours] >= 0");
                    table.ForeignKey(
                        name: "FK_Activities_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Activities_Projects_ProjectId",
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
                name: "PlanBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeeklyPlanId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PlannedHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanBlocks", x => x.Id);
                    table.CheckConstraint("CK_PlanBlock_Hours", "[PlannedHours] > 0");
                    table.ForeignKey(
                        name: "FK_PlanBlocks_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanBlocks_WeeklyPlans_WeeklyPlanId",
                        column: x => x.WeeklyPlanId,
                        principalTable: "WeeklyPlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    End = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Hours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkRecords", x => x.Id);
                    table.CheckConstraint("CK_WorkRecord_Hours", "[Hours] >= 0");
                    table.ForeignKey(
                        name: "FK_WorkRecords_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkRecords_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "People",
                columns: new[] { "Id", "Email", "IsActive", "Name", "PlanningMode", "Role", "WeeklyCapacityHours" },
                values: new object[] { 1, null, true, "Administrador", 1, 1, 45m });

            migrationBuilder.InsertData(
                table: "PersonWorkdays",
                columns: new[] { "DayOfWeek", "PersonId", "WorkingHours" },
                values: new object[,]
                {
                    { 0, 1, 0m },
                    { 1, 1, 7.5m },
                    { 2, 1, 7.5m },
                    { 3, 1, 7.5m },
                    { 4, 1, 7.5m },
                    { 5, 1, 7.5m },
                    { 6, 1, 7.5m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_PersonId",
                table: "Activities",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ProjectId",
                table: "Activities",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDependencies_SuccessorId",
                table: "ActivityDependencies",
                column: "SuccessorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanBlocks_ActivityId",
                table: "PlanBlocks",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanBlocks_WeeklyPlanId",
                table: "PlanBlocks",
                column: "WeeklyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_InitiativeId",
                table: "Projects",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PersonId",
                table: "Projects",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_TemplateId",
                table: "TemplateItems",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyPlans_PersonId_WeekStart",
                table: "WeeklyPlans",
                columns: new[] { "PersonId", "WeekStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_ActivityId",
                table: "WorkRecords",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_PersonId",
                table: "WorkRecords",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDependencies");

            migrationBuilder.DropTable(
                name: "PersonWorkdays");

            migrationBuilder.DropTable(
                name: "PlanBlocks");

            migrationBuilder.DropTable(
                name: "TemplateItems");

            migrationBuilder.DropTable(
                name: "WorkRecords");

            migrationBuilder.DropTable(
                name: "WeeklyPlans");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Initiatives");

            migrationBuilder.DropTable(
                name: "People");
        }
    }
}
