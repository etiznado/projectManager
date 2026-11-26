using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceIT.ProjectManager.Migrations
{
    /// <inheritdoc />
    public partial class JarvisPlanningHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JarvisPlanningChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityKind = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PlanApprovalReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConnectorId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApprovalReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JarvisPlanningChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JarvisPlanningStates",
                columns: table => new
                {
                    EntityKind = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    CreatedInPlanId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    LastAdvance = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JarvisPlanningStates", x => new { x.EntityKind, x.EntityId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_JarvisPlanningChanges_ApprovalId",
                table: "JarvisPlanningChanges",
                column: "ApprovalId",
                unique: true,
                filter: "[ApprovalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JarvisPlanningChanges_EntityKind_EntityId_Revision",
                table: "JarvisPlanningChanges",
                columns: new[] { "EntityKind", "EntityId", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JarvisPlanningChanges");

            migrationBuilder.DropTable(
                name: "JarvisPlanningStates");
        }
    }
}
