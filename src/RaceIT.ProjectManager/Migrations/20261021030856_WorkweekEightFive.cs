using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceIT.ProjectManager.Migrations
{
    /// <inheritdoc />
    public partial class WorkweekEightFive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE w SET WorkingHours = CASE WHEN w.DayOfWeek = 6 THEN 5 ELSE 8 END
                FROM PersonWorkdays w JOIN People p ON p.Id = w.PersonId
                WHERE p.WeeklyCapacityHours = 45 AND w.DayOfWeek BETWEEN 1 AND 6
                  AND w.WorkingHours = 7.5;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
