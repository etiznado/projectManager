using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using RaceIT.ProjectManager.Data;

namespace RaceIT.ProjectManager.Tests;

public sealed class JarvisPlanningMigrationTests
{
    [Fact]
    public void Planning_migration_and_snapshot_are_strictly_additive_and_aligned_without_database_access()
    {
        var blocker = new RejectConnection();
        var options = new DbContextOptionsBuilder<ProjectManagerDbContext>()
            .UseSqlServer("Server=unused.invalid;Database=OfflineModelOnly;Integrated Security=True;Encrypt=True")
            .AddInterceptors(blocker).Options;
        using var db = new ProjectManagerDbContext(options);
        var assembly = db.GetService<IMigrationsAssembly>();
        const string previousId = "20261126001849_JarvisOpenFrontHistory";
        const string currentId = "20261126150236_JarvisPlanningHistory";
        Assert.Contains(currentId, assembly.Migrations.Keys);
        var migration = assembly.CreateMigration(assembly.Migrations[currentId], "Microsoft.EntityFrameworkCore.SqlServer");
        AssertAdditionalSchema(migration.UpOperations);
        Assert.Equal(new[] { "JarvisPlanningChanges", "JarvisPlanningStates" }, migration.DownOperations.Select(operation => Assert.IsType<DropTableOperation>(operation).Name).Order());

        // Compare the generated target model with its predecessor, including seed data and old relationships.
        // An alteration of any prior entity/seed would add an operation and fail this assertion.
        var previous = assembly.CreateMigration(assembly.Migrations[previousId], "Microsoft.EntityFrameworkCore.SqlServer");
        var initializer = db.GetService<IModelRuntimeInitializer>();
        var before = initializer.Initialize(previous.TargetModel, designTime: true);
        var after = initializer.Initialize(migration.TargetModel, designTime: true);
        var differences = db.GetService<IMigrationsModelDiffer>().GetDifferences(before.GetRelationalModel(), after.GetRelationalModel());
        AssertAdditionalSchema(differences);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(0, blocker.Attempts);
    }

    private static void AssertAdditionalSchema(IReadOnlyList<MigrationOperation> operations)
    {
        Assert.Equal(4, operations.Count);
        var tables = operations.OfType<CreateTableOperation>().OrderBy(table => table.Name).ToArray();
        Assert.Equal(new[] { "JarvisPlanningChanges", "JarvisPlanningStates" }, tables.Select(table => table.Name));
        Assert.All(tables, table => Assert.Empty(table.ForeignKeys));
        Assert.Equal(new[] { "EntityKind", "EntityId" }, tables[1].PrimaryKey!.Columns);
        Assert.Equal("nvarchar(max)", tables[0].Columns.Single(column => column.Name == "OldValue").ColumnType);
        Assert.Equal("nvarchar(max)", tables[0].Columns.Single(column => column.Name == "NewValue").ColumnType);
        var indexes = operations.OfType<CreateIndexOperation>().ToArray();
        Assert.Equal(2, indexes.Length);
        Assert.All(indexes, index => { Assert.True(index.IsUnique); Assert.Equal("JarvisPlanningChanges", index.Table); });
        Assert.Contains(indexes, index => index.Columns.SequenceEqual(new[] { "ApprovalId" }) && index.Filter == "[ApprovalId] IS NOT NULL");
        Assert.Contains(indexes, index => index.Columns.SequenceEqual(new[] { "EntityKind", "EntityId", "Revision" }));
    }

    private sealed class RejectConnection : DbConnectionInterceptor
    {
        public int Attempts { get; private set; }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            Attempts++;
            throw new InvalidOperationException("Offline schema verification attempted a database connection.");
        }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException("Offline schema verification attempted a database connection.");
        }
    }
}
