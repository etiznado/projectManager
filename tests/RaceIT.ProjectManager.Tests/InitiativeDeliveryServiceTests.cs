using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using RaceIT.ProjectManager.Components.Pages;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class InitiativeDeliveryServiceTests
{
    [Fact]
    public async Task Page_render_shows_personal_cards_dates_and_activity_states_without_repeating_detail_title()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            db.Projects.Add(new Project { Id = 4, Name = "Segundo proyecto visible", PersonId = 1, InitiativeId = 1 });
            db.Activities.Add(new Activity { Id = 6, ProjectId = 4, PersonId = 1, Title = "Segunda actividad", Status = ActivityStatus.InProgress, CurrentEstimateHours = 1 });
            await db.SaveChangesAsync();
        }
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(fixture.Factory);
        services.AddScoped(_ => new WorkAccess(new Authentication(Actor(1)), fixture.Factory));
        services.AddScoped<InitiativeDeliveryService>();
        services.AddScoped<ProjectService>();
        services.AddSingleton<IJSRuntime>(new NonInteractiveJS());
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            System.Net.WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Initiatives>()).ToHtmlString()));
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ");
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(text, "Mismo nombre").Count);
        var selectedButton = System.Text.RegularExpressions.Regex.Match(html, "<button\\b[^>]*\\bid=\"initiative-1\"[^>]*>").Value;
        var unselectedButton = System.Text.RegularExpressions.Regex.Match(html, "<button\\b[^>]*\\bid=\"initiative-2\"[^>]*>").Value;
        Assert.NotEmpty(selectedButton);
        Assert.NotEmpty(unselectedButton);
        Assert.Contains("aria-pressed=\"true\"", selectedButton);
        Assert.Contains("aria-pressed=\"false\"", unselectedButton);
        Assert.Contains("Fecha acordada", html);
        Assert.Contains("Estimada", html);
        var agreedControl = System.Text.RegularExpressions.Regex.Match(html, "<button\\b[^>]*\\bid=\"agreed-1\"[^>]*>.*?</button>").Value;
        var estimateControl = System.Text.RegularExpressions.Regex.Match(html, "<button\\b[^>]*\\bid=\"estimate-1\"[^>]*>.*?</button>").Value;
        Assert.Contains("aria-haspopup=\"dialog\"", agreedControl);
        Assert.Contains("Editar fecha acordada de Propio", agreedControl);
        Assert.Contains("20 ene 2027", agreedControl);
        Assert.Contains("aria-haspopup=\"dialog\"", estimateControl);
        Assert.Contains("Editar fecha estimada de Propio", estimateControl);
        Assert.Contains("23 ene 2027", estimateControl);
        Assert.DoesNotContain(">Editar estimación</button>", html);
        Assert.Contains("class=\"secondary fullscreen-toggle\"", html);
        Assert.Contains("aria-label=\"Entrar en pantalla completa\"", html);
        Assert.DoesNotContain("Pantalla completa ⛶", html);
        var reference = System.Text.RegularExpressions.Regex.Match(html, "<section\\b[^>]*class=\"initiative-detail delivery-size-reference\"[^>]*>.*?</section>", System.Text.RegularExpressions.RegexOptions.Singleline).Value;
        Assert.Contains("aria-hidden=\"true\"", reference);
        Assert.Contains("inert", reference);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(reference, "<article\\b").Count);
        Assert.DoesNotContain(" id=", reference);
        Assert.DoesNotContain("<button", reference);
        Assert.DoesNotContain("<input", reference);
        var ids = System.Text.RegularExpressions.Regex.Matches(html, "\\bid=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Contains("+3 días", html);
        Assert.Contains("Propia iniciada", html);
        Assert.DoesNotContain("Actividad privada ajena", html);
        Assert.DoesNotContain("Bolsa compartida", html);
        Assert.DoesNotContain("type=\"checkbox\"", html);
    }

    [Fact]
    public async Task Personal_read_isolated_for_admin_and_grouped_by_id_even_with_duplicate_names()
    {
        await using var fixture = await Fixture.CreateAsync();
        var initiatives = await fixture.Deliveries(1).GetAsync();
        Assert.Equal(new[] { 1, 2 }, initiatives.Select(initiative => initiative.Id));
        Assert.All(initiatives, initiative => Assert.Equal("Mismo nombre", initiative.Name));
        var project = Assert.Single(initiatives[0].Projects);
        Assert.Equal(1, project.Id);
        Assert.Equal(1, Assert.Single(project.Activities).Id);
        Assert.Equal(new DateOnly(2027, 1, 23), project.SummaryDate);
        Assert.Equal(3, project.DelayDays);
        var foreign = Assert.Single(await fixture.Deliveries(2).GetAsync());
        Assert.Equal(2, Assert.Single(foreign.Projects).Id);
    }

    [Fact]
    public async Task Active_filter_excludes_empty_pending_completed_and_closed_but_keeps_consumed_open_work()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            db.Projects.AddRange(Enumerable.Range(4, 6).Select(id => new Project { Id = id, InitiativeId = 1, PersonId = 1, Name = $"Proyecto {id}" }));
            db.Activities.AddRange(
                new Activity { Id = 10, ProjectId = 5, PersonId = 1, Title = "Sin iniciar", Status = ActivityStatus.Pending, CurrentEstimateHours = 1 },
                new Activity { Id = 11, ProjectId = 6, PersonId = 1, Title = "Terminada", Status = ActivityStatus.Completed, CurrentEstimateHours = 1 },
                new Activity { Id = 12, ProjectId = 7, PersonId = 1, Title = "Cerrado", Status = ActivityStatus.InProgress, CurrentEstimateHours = 1 },
                new Activity { Id = 13, ProjectId = 8, PersonId = 1, Title = "Agotada abierta", Status = ActivityStatus.Pending, CurrentEstimateHours = 1 },
                new Activity { Id = 14, ProjectId = 9, PersonId = 1, Title = "Trabajo cero", Status = ActivityStatus.Pending, CurrentEstimateHours = 1 });
            db.WorkRecords.AddRange(new WorkRecord { ActivityId = 13, PersonId = 1, Kind = WorkRecordKind.Manual, Hours = 3 },
                new WorkRecord { ActivityId = 14, PersonId = 1, Kind = WorkRecordKind.Manual, Hours = 0 });
            db.JarvisPlanningStates.Add(new JarvisPlanningState { EntityKind = "project", EntityId = 7, PersonId = 1, IsClosed = true });
            await db.SaveChangesAsync();
        }
        var active = (await fixture.Deliveries(1).GetAsync()).SelectMany(initiative => initiative.Projects).Select(project => project.Id).Order().ToArray();
        Assert.Equal(new[] { 1, 3, 8 }, active);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            db.JarvisPlanningStates.Add(new JarvisPlanningState { EntityKind = "initiative", EntityId = 1, PersonId = 1, IsClosed = true });
            await db.SaveChangesAsync();
        }
        Assert.Equal(2, Assert.Single(await fixture.Deliveries(1).GetAsync()).Id);
    }

    [Fact]
    public async Task Forecast_round_trip_clear_and_general_edit_preserve_agreement_and_planning()
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.PlanningSnapshot();
        var agreed = new DateOnly(2027, 1, 20);
        var estimated = new DateOnly(2027, 1, 25);
        // Owner with the regular user role, not just an administrator.
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.People.SingleAsync(person => person.Id == 1)).Role = PersonRole.User;
            await db.SaveChangesAsync();
        }
        await fixture.Projects(1).UpdateEstimatedDateAsync(1, estimated);
        Assert.Equal(estimated, (await fixture.Deliveries(1).GetAsync())[0].Projects[0].EstimatedDate);
        await fixture.Projects(1).UpdateProjectAsync(1, new ProjectInput { Name = "Nombre editado", InitiativeId = 1, PersonId = 1, TargetDate = agreed });
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            var project = await db.Projects.SingleAsync(project => project.Id == 1);
            Assert.Equal(agreed, project.TargetDate);
            Assert.Equal(estimated, project.EstimatedDate);
            // Changing effort/status never computes or replaces the manual date.
            (await db.Activities.SingleAsync(activity => activity.Id == 1)).CurrentEstimateHours = 20;
            await db.SaveChangesAsync();
        }
        Assert.Equal(estimated, (await fixture.Deliveries(1).GetAsync())[0].Projects[0].EstimatedDate);
        await fixture.Projects(1).UpdateEstimatedDateAsync(1, null);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            var project = await db.Projects.SingleAsync(project => project.Id == 1);
            Assert.Null(project.EstimatedDate);
            Assert.Equal(agreed, project.TargetDate);
        }
        Assert.Equal(before, await fixture.PlanningSnapshot());
    }

    [Fact]
    public async Task Agreed_date_round_trip_and_clear_preserve_forecast_and_planning()
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.PlanningSnapshot();
        var forecast = new DateOnly(2027, 1, 23);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.People.SingleAsync(person => person.Id == 1)).Role = PersonRole.User;
            await db.SaveChangesAsync();
        }
        foreach (var agreed in new DateOnly?[] { new(2027, 1, 25), null, new(2027, 1, 20) })
        {
            await fixture.Projects(1).UpdateAgreedDateAsync(1, agreed);
            var project = (await fixture.Deliveries(1).GetAsync())[0].Projects[0];
            Assert.Equal(agreed, project.AgreedDate);
            Assert.Equal(forecast, project.EstimatedDate);
            Assert.Equal(before, await fixture.PlanningSnapshot());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Admin_can_edit_foreign_dates_but_third_party_and_untrusted_actors_cannot(bool agreed)
    {
        await using var fixture = await Fixture.CreateAsync();
        var estimated = new DateOnly(2027, 2, 1);
        var before = await fixture.PlanningSnapshot();
        async Task SetDate(ClaimsPrincipal actor, int id, DateOnly? date)
        {
            if (agreed) await fixture.Projects(actor).UpdateAgreedDateAsync(id, date);
            else await fixture.Projects(actor).UpdateEstimatedDateAsync(id, date);
        }
        await SetDate(Actor(1), 2, estimated);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            var project = await db.Projects.SingleAsync(project => project.Id == 2);
            Assert.Equal(estimated, agreed ? project.TargetDate : project.EstimatedDate);
            Assert.Null(agreed ? project.EstimatedDate : project.TargetDate);
        }
        foreach (var actor in new[] { Actor(2), Actor(3), Actor(999), Actor(1, "unknown"), new ClaimsPrincipal(new ClaimsIdentity()) })
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => SetDate(actor, 1, estimated));
        await using var check = await fixture.Factory.CreateDbContextAsync();
        var unchanged = await check.Projects.SingleAsync(project => project.Id == 1);
        Assert.Equal(new DateOnly(2027, 1, 20), unchanged.TargetDate);
        Assert.Equal(new DateOnly(2027, 1, 23), unchanged.EstimatedDate);
        Assert.Equal(before, await fixture.PlanningSnapshot());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_inactive_anonymous_and_untrusted_sessions_cannot_read_deliveries_or_write_dates(bool agreed)
    {
        await using var fixture = await Fixture.CreateAsync();
        foreach (var actor in new[] { Actor(3), Actor(999), Actor(1, "unknown"), new ClaimsPrincipal(new ClaimsIdentity()) })
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Deliveries(actor).GetAsync());
        Task SetDate(int id, DateOnly? date) => agreed
            ? fixture.Projects(1).UpdateAgreedDateAsync(id, date)
            : fixture.Projects(1).UpdateEstimatedDateAsync(id, date);
        await Assert.ThrowsAsync<ArgumentException>(() => SetDate(999, DateOnly.MaxValue));
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.People.SingleAsync(person => person.Id == 1)).IsActive = false;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => SetDate(1, null));
    }

    private static ClaimsPrincipal Actor(int id, string source = "google") => new(new ClaimsIdentity(
        [new("PersonId", id.ToString()), new("auth_source", source)], "test"));
    private sealed class Authentication(ClaimsPrincipal actor) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(actor));
    }
    private sealed class NonInteractiveJS : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new InvalidOperationException("Static render must not invoke browser APIs.");
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class Fixture(SqliteConnection connection, ServiceProvider provider) : IAsyncDisposable
    {
        public IDbContextFactory<ProjectManagerDbContext> Factory { get; } = provider.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
        private WorkAccess Access(ClaimsPrincipal actor) => new(new Authentication(actor), Factory);
        public ProjectService Projects(int id) => Projects(Actor(id));
        public ProjectService Projects(ClaimsPrincipal actor) => new(Factory, Access(actor));
        public InitiativeDeliveryService Deliveries(int id) => Deliveries(Actor(id));
        public InitiativeDeliveryService Deliveries(ClaimsPrincipal actor) => new(Factory, Access(actor));

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddDbContextFactory<ProjectManagerDbContext>(options => options.UseSqlite(connection));
            var fixture = new Fixture(connection, services.BuildServiceProvider());
            await using var db = await fixture.Factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
            db.People.AddRange(new Person { Id = 2, Name = "Otro propietario", Role = PersonRole.User },
                new Person { Id = 3, Name = "Inactivo", IsActive = false });
            db.Initiatives.AddRange(new Initiative { Id = 1, Name = "Mismo nombre" }, new Initiative { Id = 2, Name = "Mismo nombre" });
            db.Projects.AddRange(new Project { Id = 1, Name = "Propio", PersonId = 1, InitiativeId = 1, TargetDate = new DateOnly(2027, 1, 20), EstimatedDate = new DateOnly(2027, 1, 23) },
                new Project { Id = 2, Name = "Ajeno", PersonId = 2, InitiativeId = 1 },
                new Project { Id = 3, Name = "Otro propio", PersonId = 1, InitiativeId = 2 });
            db.Activities.AddRange(new Activity { Id = 1, PersonId = 1, ProjectId = 1, Title = "Propia iniciada", Status = ActivityStatus.InProgress, CurrentEstimateHours = 8 },
                new Activity { Id = 2, PersonId = 2, ProjectId = 2, Title = "Ajena", Status = ActivityStatus.InProgress, CurrentEstimateHours = 8 },
                new Activity { Id = 3, PersonId = 2, ProjectId = 1, Title = "Actividad privada ajena", Status = ActivityStatus.InProgress, CurrentEstimateHours = 8 },
                new Activity { Id = 4, PersonId = 1, ProjectId = 3, Title = "Propia terminada", Status = ActivityStatus.Completed, CurrentEstimateHours = 8 },
                new Activity { Id = 5, PersonId = 1, ProjectId = 3, Title = "Propia pendiente", Status = ActivityStatus.Pending, CurrentEstimateHours = 8 });
            db.WeeklyPlans.Add(new WeeklyPlan { Id = 1, PersonId = 1, WeekStart = new DateOnly(2026, 11, 23) });
            db.PlanBlocks.Add(new PlanBlock { Id = 1, WeeklyPlanId = 1, ActivityId = 1, Position = 0, PlannedHours = 3 });
            db.WorkRecords.Add(new WorkRecord { Id = 1, PersonId = 1, ActivityId = 1, Kind = WorkRecordKind.Manual, Hours = 1 });
            await db.SaveChangesAsync();
            return fixture;
        }

        public async Task<string> PlanningSnapshot()
        {
            await using var db = await Factory.CreateDbContextAsync();
            return JsonSerializer.Serialize(new
            {
                Plans = await db.WeeklyPlans.AsNoTracking().Select(plan => new { plan.Id, plan.PersonId, plan.WeekStart }).ToArrayAsync(),
                Blocks = await db.PlanBlocks.AsNoTracking().Select(block => new { block.Id, block.ActivityId, block.Position, block.PlannedHours }).ToArrayAsync(),
                Records = await db.WorkRecords.AsNoTracking().Select(record => new { record.Id, record.PersonId, record.ActivityId, record.Kind, record.Start, record.End, record.Hours, record.Note }).ToArrayAsync()
            });
        }

        public async ValueTask DisposeAsync() { await provider.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
