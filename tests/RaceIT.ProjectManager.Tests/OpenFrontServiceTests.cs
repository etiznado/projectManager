using System.Security.Claims;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class OpenFrontServiceTests
{
    private static ClaimsPrincipal Actor(int id, string role = "Administrador", string source = "google") =>
        new(new ClaimsIdentity([new("PersonId", id.ToString()), new(ClaimTypes.Role, role), new("auth_source", source)], "test"));

    [Fact]
    public async Task Personal_list_never_includes_another_owner_even_for_admin()
    {
        await using var fixture = await Fixture.CreateAsync();
        var mine = Assert.Single(await fixture.Service.ListAsync(Actor(1)));
        Assert.Equal("Frente propio", mine.Title);
        Assert.Equal("Avance original", mine.LastAdvance);
        Assert.Equal("Paso original", mine.NextStep);
        Assert.Equal("Otro frente", Assert.Single(await fixture.Service.ListAsync(Actor(2, "Usuario"))).Title);
        Assert.Empty(await fixture.Service.ListAsync(Actor(4)));
    }

    [Fact]
    public async Task Status_survives_new_service_and_context_without_changing_content_or_planning()
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.PlanningSnapshotAsync();
        var front = Assert.Single(await fixture.Service.ListAsync(Actor(1)));
        var saved = await fixture.Service.ChangeStatusAsync(Actor(1), front.Id, OpenFrontStatus.Waiting, front.Revision);
        Assert.Equal(1, saved.Revision);
        var reopened = Assert.Single(await new OpenFrontService(fixture.Factory).ListAsync(Actor(1)));
        Assert.Equal(OpenFrontStatus.Waiting, reopened.Status);
        Assert.Equal(front.Title, reopened.Title);
        Assert.Equal(front.Context, reopened.Context);
        Assert.Equal(front.LastAdvance, reopened.LastAdvance);
        Assert.Equal(front.NextStep, reopened.NextStep);
        Assert.True(reopened.UpdatedAtUtc >= front.UpdatedAtUtc);
        Assert.Equal(before, await fixture.PlanningSnapshotAsync());
    }

    [Fact]
    public async Task Anonymous_inactive_and_untrusted_actors_cannot_read_or_save()
    {
        await using var fixture = await Fixture.CreateAsync();
        foreach (var actor in new[] { new ClaimsPrincipal(new ClaimsIdentity()), Actor(3), Actor(999), Actor(1, source: "unknown") })
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ListAsync(actor));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ChangeStatusAsync(actor, 1, OpenFrontStatus.Waiting, 0));
        }
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.People.SingleAsync(x => x.Id == 1)).IsActive = false;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ChangeStatusAsync(Actor(1), 1, OpenFrontStatus.Waiting, 0));
        await using var check = await fixture.Factory.CreateDbContextAsync();
        Assert.Equal(OpenFrontStatus.InProgress, (await check.OpenFronts.SingleAsync(x => x.Id == 1)).Status);
    }

    [Fact]
    public async Task Foreign_and_missing_ids_are_rejected_identically_even_for_admin()
    {
        await using var fixture = await Fixture.CreateAsync();
        var foreign = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ChangeStatusAsync(Actor(1), 2, OpenFrontStatus.Waiting, 0));
        var missing = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ChangeStatusAsync(Actor(1), 999, OpenFrontStatus.Waiting, 0));
        Assert.Equal(foreign.Message, missing.Message);
        Assert.Equal(OpenFrontStatus.NeedsDecision, Assert.Single(await fixture.Service.ListAsync(Actor(2))).Status);
    }

    [Fact]
    public async Task Invalid_state_and_stale_revision_do_not_overwrite_saved_state()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.ChangeStatusAsync(Actor(1), 1, (OpenFrontStatus)99, 0));
        Assert.Equal(OpenFrontStatus.InProgress, Assert.Single(await fixture.Service.ListAsync(Actor(1))).Status);
        await fixture.Service.ChangeStatusAsync(Actor(1), 1, OpenFrontStatus.Waiting, 0);
        await Assert.ThrowsAsync<OpenFrontConflictException>(() => fixture.Service.ChangeStatusAsync(Actor(1), 1, OpenFrontStatus.NeedsDecision, 0));
        var saved = Assert.Single(await fixture.Service.ListAsync(Actor(1)));
        Assert.Equal(OpenFrontStatus.Waiting, saved.Status);
        Assert.Equal(1, saved.Revision);
    }

    [Fact]
    public async Task Database_rejection_does_not_commit_and_valid_retry_succeeds()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Failure.RejectSave = true;
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Service.ChangeStatusAsync(Actor(1), 1, OpenFrontStatus.Waiting, 0));
        var unchanged = Assert.Single(await fixture.Service.ListAsync(Actor(1)));
        Assert.Equal(OpenFrontStatus.InProgress, unchanged.Status);
        Assert.Equal(0, unchanged.Revision);
        fixture.Failure.RejectSave = false;
        await fixture.Service.ChangeStatusAsync(Actor(1), 1, OpenFrontStatus.Waiting, 0);
        Assert.Equal(OpenFrontStatus.Waiting, Assert.Single(await fixture.Service.ListAsync(Actor(1))).Status);
    }

    private sealed class SaveFailure : SaveChangesInterceptor
    {
        public bool RejectSave { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            RejectSave ? throw new DbUpdateException("Synthetic pre-commit rejection.") : base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private sealed class Fixture(SqliteConnection connection, ServiceProvider provider, SaveFailure failure) : IAsyncDisposable
    {
        public IDbContextFactory<ProjectManagerDbContext> Factory { get; } = provider.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
        public SaveFailure Failure { get; } = failure;
        public OpenFrontService Service => new(Factory);

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var failure = new SaveFailure();
            var services = new ServiceCollection();
            services.AddDbContextFactory<ProjectManagerDbContext>(options => options.UseSqlite(connection).AddInterceptors(failure));
            var fixture = new Fixture(connection, services.BuildServiceProvider(), failure);
            await using var db = await fixture.Factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
            db.People.AddRange(new Person { Id = 2, Name = "Synthetic second owner", IsActive = true },
                new Person { Id = 3, Name = "Synthetic inactive", IsActive = false }, new Person { Id = 4, Name = "Synthetic empty owner", IsActive = true });
            db.OpenFronts.AddRange(new OpenFront { Id = 1, PersonId = 1, Title = "Frente propio", Context = "Contexto original", LastAdvance = "Avance original", NextStep = "Paso original" },
                new OpenFront { Id = 2, PersonId = 2, Title = "Otro frente", Status = OpenFrontStatus.NeedsDecision, LastAdvance = "Otro avance", NextStep = "Otro paso" });
            db.Activities.Add(new Activity { Id = 1, PersonId = 1, Title = "Actividad sintética", CurrentEstimateHours = 8 });
            db.WeeklyPlans.Add(new WeeklyPlan { Id = 1, PersonId = 1, WeekStart = new DateOnly(2026, 11, 23) });
            db.PlanBlocks.Add(new PlanBlock { Id = 1, WeeklyPlanId = 1, ActivityId = 1, PlannedHours = 3, Position = 0 });
            db.WorkRecords.Add(new WorkRecord { Id = 1, PersonId = 1, ActivityId = 1, Kind = WorkRecordKind.Manual, Hours = 1 });
            await db.SaveChangesAsync();
            return fixture;
        }

        public async Task<string> PlanningSnapshotAsync()
        {
            await using var db = await Factory.CreateDbContextAsync();
            return JsonSerializer.Serialize(new
            {
                People = await db.People.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Days = await db.PersonWorkdays.AsNoTracking().OrderBy(x => x.PersonId).ThenBy(x => x.DayOfWeek).ToListAsync(),
                Plans = await db.WeeklyPlans.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Blocks = await db.PlanBlocks.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Activities = await db.Activities.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Records = await db.WorkRecords.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
            });
        }

        public async ValueTask DisposeAsync() { await provider.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
