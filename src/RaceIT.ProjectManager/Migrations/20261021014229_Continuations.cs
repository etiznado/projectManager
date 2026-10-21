using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceIT.ProjectManager.Migrations
{
    /// <inheritdoc />
    public partial class Continuations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Continuations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromActivityId = table.Column<int>(type: "int", nullable: false),
                    ToActivityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Continuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Continuations_Activities_FromActivityId",
                        column: x => x.FromActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Continuations_Activities_ToActivityId",
                        column: x => x.ToActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Continuations_FromActivityId",
                table: "Continuations",
                column: "FromActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Continuations_ToActivityId",
                table: "Continuations",
                column: "ToActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Continuations");
        }
    }
}
