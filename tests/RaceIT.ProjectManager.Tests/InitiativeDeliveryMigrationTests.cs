using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using RaceIT.ProjectManager.Data;

namespace RaceIT.ProjectManager.Tests;

public sealed class InitiativeDeliveryMigrationTests
{
    [Fact]
    public void Estimated_date_is_the_only_delta_and_model_is_aligned_without_connections()
    {
        var blocker = new RejectConnection();
        using var db = new ProjectManagerDbContext(new DbContextOptionsBuilder<ProjectManagerDbContext>()
            .UseSqlServer("Server=unused.invalid;Database=OfflineModelOnly;Integrated Security=True;Encrypt=True")
            .AddInterceptors(blocker).Options);
        var assembly = db.GetService<IMigrationsAssembly>();
        const string current = "20261126182000_ProjectEstimatedDate";
        Assert.Equal(current, assembly.Migrations.Last().Key);
        var migration = assembly.CreateMigration(assembly.Migrations[current], "Microsoft.EntityFrameworkCore.SqlServer");
        AssertColumn(Assert.Single(migration.UpOperations));
        var drop = Assert.IsType<DropColumnOperation>(Assert.Single(migration.DownOperations));
        Assert.Equal("Projects", drop.Table);
        Assert.Equal("EstimatedDate", drop.Name);
        var previous = assembly.CreateMigration(assembly.Migrations["20261126150236_JarvisPlanningHistory"], "Microsoft.EntityFrameworkCore.SqlServer");
        var initializer = db.GetService<IModelRuntimeInitializer>();
        var before = initializer.Initialize(previous.TargetModel, designTime: true);
        var after = initializer.Initialize(migration.TargetModel, designTime: true);
        AssertColumn(Assert.Single(db.GetService<IMigrationsModelDiffer>().GetDifferences(before.GetRelationalModel(), after.GetRelationalModel())));
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(0, blocker.Attempts);
    }

    private static void AssertColumn(MigrationOperation operation)
    {
        var column = Assert.IsType<AddColumnOperation>(operation);
        Assert.Equal("Projects", column.Table);
        Assert.Equal("EstimatedDate", column.Name);
        Assert.Equal("date", column.ColumnType);
        Assert.True(column.IsNullable);
        Assert.Null(column.DefaultValue);
        Assert.Null(column.DefaultValueSql);
        Assert.Null(column.ComputedColumnSql);
    }

    private sealed class RejectConnection : DbConnectionInterceptor
    {
        public int Attempts { get; private set; }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        { Attempts++; throw new InvalidOperationException("Offline model verification attempted a connection."); }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        { Attempts++; throw new InvalidOperationException("Offline model verification attempted a connection."); }
    }
}
