using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceIT.ProjectManager.Migrations
{
    /// <inheritdoc />
    public partial class OpenFronts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenFronts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastAdvance = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NextStep = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenFronts", x => x.Id);
                    table.CheckConstraint("CK_OpenFront_Revision", "[Revision] >= 0");
                    table.CheckConstraint("CK_OpenFront_Status", "[Status] >= 0 AND [Status] <= 2");
                    table.ForeignKey(
                        name: "FK_OpenFronts_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpenFronts_PersonId",
                table: "OpenFronts",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenFronts");
        }
    }
}
