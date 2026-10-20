using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceIT.ProjectManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class DependencyScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedFinishHour",
                table: "Activities",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedStartHour",
                table: "Activities",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RequestedStart",
                table: "Activities",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql("UPDATE [Activities] SET [RequestedStart] = [EstimatedStart]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedFinishHour",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "EstimatedStartHour",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "RequestedStart",
                table: "Activities");
        }
    }
}

