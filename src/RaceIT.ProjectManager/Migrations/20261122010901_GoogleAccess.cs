using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceIT.ProjectManager.Data;

#nullable disable

namespace RaceIT.ProjectManager.Migrations;

[DbContext(typeof(ProjectManagerDbContext))]
[Migration("20261122010901_GoogleAccess")]
public sealed class GoogleAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("AuthenticationAuditEvents", columns: table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            PersonId = table.Column<int>(type: "int", nullable: true),
            AtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_AuthenticationAuditEvents", x => x.Id));

        migrationBuilder.CreateTable("GoogleIdentityLinks", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            PersonId = table.Column<int>(type: "int", nullable: false),
            Issuer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GoogleIdentityLinks", x => x.Id);
            table.ForeignKey("FK_GoogleIdentityLinks_People_PersonId", x => x.PersonId, "People", "Id");
        });

        migrationBuilder.CreateTable("GoogleInvitations", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            PersonId = table.Column<int>(type: "int", nullable: false),
            ExpectedEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
            ExpectedIssuer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
            ExpectedSubject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
            TokenHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            ConsumedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            IsBootstrap = table.Column<bool>(type: "bit", nullable: false),
            IssuedByPersonId = table.Column<int>(type: "int", nullable: true),
            IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_GoogleInvitations", x => x.Id);
            table.ForeignKey("FK_GoogleInvitations_People_PersonId", x => x.PersonId, "People", "Id");
        });

        migrationBuilder.CreateIndex("IX_GoogleIdentityLinks_Issuer_Subject", "GoogleIdentityLinks", new[] { "Issuer", "Subject" }, unique: true);
        migrationBuilder.CreateIndex("IX_GoogleIdentityLinks_PersonId", "GoogleIdentityLinks", "PersonId", unique: true);
        migrationBuilder.CreateIndex("IX_GoogleInvitations_PersonId", "GoogleInvitations", "PersonId");
        migrationBuilder.CreateIndex("IX_GoogleInvitations_TokenHash", "GoogleInvitations", "TokenHash", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AuthenticationAuditEvents");
        migrationBuilder.DropTable("GoogleIdentityLinks");
        migrationBuilder.DropTable("GoogleInvitations");
    }
}
