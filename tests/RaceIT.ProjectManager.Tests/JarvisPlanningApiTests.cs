using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaceIT.ProjectManager.Api;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class JarvisPlanningApiTests
{
    [Fact]
    public async Task Equivalent_database_decimal_scales_keep_the_same_fingerprint_and_valid_precondition()
    {
        Assert.Equal(JarvisPlanningAccess.ValuesHash(new { hours = 8m }), JarvisPlanningAccess.ValuesHash(new { hours = 8.00m }));
        await using var f = await Fixture.CreateAsync();
        var before = await f.Get("activities", 1);
        await using (var db = await f.Factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("UPDATE Activities SET CurrentEstimateHours = '8.00' WHERE Id = 1;");
        var reloaded = await f.Get("activities", 1);
        Assert.Equal(before.CurrentEstimateHours, reloaded.CurrentEstimateHours);
        Assert.Equal(before.Fingerprint, reloaded.Fingerprint);
        Assert.Equal(HttpStatusCode.OK, (await f.Post("activities", before, "/advances", new { text = "Same numeric state" })).StatusCode);
    }

    [Fact]
    public async Task Authentication_and_owner_boundaries_cover_collections_details_relationships_and_history()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.Equal(new[] { 1 }, (await f.List("initiatives")).Select(x => x.Id));
        Assert.DoesNotContain(await f.List("projects"), x => x.Id is 2 or 4);
        Assert.Null((await f.Get("projects", 6)).ParentId);
        Assert.DoesNotContain(await f.List("activities"), x => x.Id is 2 or 6);
        foreach (var (collection, ids) in new[] { ("initiatives", new[] { 2, 3, 4, 999 }), ("projects", new[] { 2, 4, 999 }), ("activities", new[] { 2, 6, 999 }) })
        {
            foreach (var suffix in new[] { "", "/history" })
            {
                string? body = null;
                foreach (var id in ids)
                {
                    var response = await f.Client.GetAsync(Fixture.Path(collection, id) + suffix);
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                    body ??= await response.Content.ReadAsStringAsync();
                    Assert.Equal(body, await response.Content.ReadAsStringAsync());
                }
            }
        }
        await using (var db = await f.Factory.CreateDbContextAsync())
        {
            db.ActivityDependencies.Add(new ActivityDependency { PredecessorId = 2, SuccessorId = 1 });
            await db.SaveChangesAsync();
        }
        Assert.Empty((await f.Get("activities", 1)).PredecessorIds);
        f.Client.DefaultRequestHeaders.Authorization = null;
        f.Client.DefaultRequestHeaders.Add("Cookie", f.BrowserCookie());
        Assert.Equal(HttpStatusCode.OK, (await f.Client.GetAsync("/browser-only")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync(Fixture.Path("activities"))).StatusCode);
        f.Client.DefaultRequestHeaders.Remove("Cookie");
        foreach (var authorization in new AuthenticationHeaderValue?[] { null, new("Bearer", "invalid") })
        {
            f.Client.DefaultRequestHeaders.Authorization = authorization;
            Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync(Fixture.Path("initiatives"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.PostAsJsonAsync(Fixture.Path("activities"), new { planId = "plan-a", title = "x", initialEstimateHours = 1 })).StatusCode);
        }
        f.Authenticate();
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync("http://localhost" + Fixture.Path("projects"))).StatusCode);
        await using (var db = await f.Factory.CreateDbContextAsync()) { (await db.People.SingleAsync(x => x.Id == 1)).IsActive = false; await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Client.GetAsync(Fixture.Path("activities"))).StatusCode);
    }

    [Fact]
    public async Task Approved_plan_creates_hierarchy_and_independent_work_and_updates_without_repeated_approval()
    {
        await using var f = await Fixture.CreateAsync();
        var unchanged = await f.ProtectedSnapshot();
        var initiative = await f.Create("initiatives", new { planId = "plan-a", name = "Nueva", description = "Contexto", targetDate = "2027-02-01" });
        var project = await f.Create("projects", new { planId = "plan-a", name = "Proyecto", initiativeId = initiative.Id });
        var activity = await f.Create("activities", new { planId = "plan-a", title = "Trabajo", projectId = project.Id, initialEstimateHours = 12.25 });
        Assert.Equal(initiative.Id, project.ParentId);
        Assert.Equal(project.Id, activity.ParentId);
        Assert.Equal(12.25m, activity.CurrentEstimateHours);
        var independent = await f.Create("projects", new { planId = "plan-a", name = "Independiente" });
        var loose = await f.Create("activities", new { planId = "plan-a", title = "Suelta", initialEstimateHours = 2 });
        Assert.Null(independent.ParentId); Assert.Null(loose.ParentId);
        Assert.Equal(HttpStatusCode.OK, (await f.Put("activities", activity, "", new { title = "Trabajo editado" })).StatusCode);
        activity = await f.Get("activities", activity.Id);
        Assert.Equal(project.Id, activity.ParentId); // omitted parent preserves the relationship
        Assert.Equal(HttpStatusCode.OK, (await f.Put("activities", activity, "", new { title = "Suelta ahora", projectId = (int?)null })).StatusCode);
        activity = await f.Get("activities", activity.Id);
        Assert.Null(activity.ParentId);
        Assert.Equal(HttpStatusCode.OK, (await f.Post("activities", activity, "/advances", new { text = "Avance sin horas" })).StatusCode);
        activity = await f.Get("activities", activity.Id);
        Assert.Equal(ActivityStatus.Pending, activity.Status);
        Assert.Equal(HttpStatusCode.OK, (await f.Put("activities", activity, "/status", new { status = 1 })).StatusCode);
        Assert.Equal(unchanged, await f.ProtectedSnapshot());
        await using (var db = await f.Factory.CreateDbContextAsync())
        {
            Assert.All(await db.JarvisPlanningStates.ToListAsync(), state => Assert.Equal(1, state.PersonId));
            Assert.All(await db.Activities.Where(x => x.Id > 8).ToListAsync(), row => Assert.Equal(1, row.PersonId));
        }
        await f.RestartAsync();
        Assert.Equal("Avance sin horas", (await f.Get("activities", activity.Id)).LastAdvance);
        var history = await f.Client.GetFromJsonAsync<PlanningHistoryDto[]>(Fixture.Path("activities", activity.Id) + "/history");
        Assert.Equal(new[] { "created", "edit", "edit", "advance", "status" }, history!.Select(x => x.Operation));
        Assert.All(history!, row => { Assert.Equal("jarvis-planning-test", row.ConnectorId); Assert.Equal(TimeSpan.Zero, row.AtUtc.Offset); Assert.Equal("approval-of-plan-a", row.PlanApprovalReference); });
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Client.PostAsJsonAsync(Fixture.Path("projects"), new { planId = "client-invented", name = "Denied" })).StatusCode);
        var outside = await f.Get("activities", 4);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Post("activities", outside, "/advances", new { text = "Outside" })).StatusCode);
    }

    [Fact]
    public async Task Exact_operator_approvals_allow_dates_dependencies_and_closures_with_traceable_one_time_use()
    {
        await using var f = await Fixture.CreateAsync();
        var initial = await f.ProtectedSnapshot();
        var initiative = await f.Get("initiatives", 1);
        await f.Approve(initiative, "target-date", new { targetDate = new DateOnly(2027, 3, 5) }, "date-ok");
        Assert.Equal(HttpStatusCode.OK, (await f.Put("initiatives", initiative, "/target-date", new { targetDate = "2027-03-05", approvalId = "date-ok" })).StatusCode);
        var activity = await f.Get("activities", 1);
        await f.Approve(activity, "dependencies", new { predecessorIds = new[] { 5 } }, "deps-ok");
        Assert.Equal(HttpStatusCode.OK, (await f.Put("activities", activity, "/dependencies", new { predecessorIds = new[] { 5, 5 }, approvalId = "deps-ok" })).StatusCode);
        activity = await f.Get("activities", 1);
        Assert.Equal(new[] { 5 }, activity.PredecessorIds);
        Assert.Equal(HttpStatusCode.OK, (await f.Put("activities", activity, "/dependencies", new { predecessorIds = new[] { 5 } })).StatusCode);
        foreach (var collection in new[] { "projects", "initiatives" })
        {
            var item = await f.Get(collection, 1);
            await f.Approve(item, "close", new { isClosed = true }, collection + "-close");
            Assert.Equal(HttpStatusCode.OK, (await f.Post(collection, item, "/close", new { approvalId = collection + "-close" })).StatusCode);
            Assert.True((await f.Get(collection, 1)).IsClosed);
        }
        await using var db = await f.Factory.CreateDbContextAsync();
        Assert.Single(await db.ActivityDependencies.ToListAsync());
        var approved = await db.JarvisPlanningChanges.Where(x => x.ApprovalId != null).ToListAsync();
        Assert.Equal(4, approved.Count);
        Assert.All(approved, x => Assert.Equal("Edgar-specific-approval", x.ApprovalReference));
        Assert.Equal(initial, await f.ProtectedSnapshot());
        Assert.Equal(ActivityStatus.Pending, (await db.Activities.SingleAsync(x => x.Id == 1)).Status);
    }

    [Fact]
    public async Task Missing_forged_mismatched_and_stale_approvals_and_client_permissions_are_rejected_without_events()
    {
        await using var f = await Fixture.CreateAsync();
        var item = await f.Get("initiatives", 1);
        await f.Approve(item, "target-date", new { targetDate = new DateOnly(2027, 3, 5) }, "date-only");
        foreach (var extra in new object[]
        {
            new { targetDate = "2027-03-05", approvalId = "missing" },
            new { targetDate = "2027-03-06", approvalId = "date-only" },
            new { targetDate = "2027-03-05", approvalId = "missing", approved = true, approvedBy = "Edgar" }
        })
        {
            var response = await f.Put("initiatives", item, "/target-date", extra);
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.BadRequest });
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Post("initiatives", item, "/close", new { approvalId = "date-only" })).StatusCode);
        var project = await f.Get("projects", 1);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Put("projects", project, "/target-date", new { targetDate = "2027-03-05", approvalId = "date-only" })).StatusCode);
        var activity = await f.Get("activities", 1);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Put("activities", activity, "/dependencies", new { predecessorIds = new[] { 5 } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsJsonAsync(Fixture.Path("activities"), new { planId = "plan-a", title = "Forged", initialEstimateHours = 1, personId = 2, planApproved = true })).StatusCode);
        await using (var db = await f.Factory.CreateDbContextAsync()) { Assert.Empty(await db.JarvisPlanningChanges.ToListAsync()); }
        Assert.Equal(HttpStatusCode.OK, (await f.Post("initiatives", item, "/advances", new { text = "New revision" })).StatusCode);
        var changed = await f.Get("initiatives", 1);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Put("initiatives", changed, "/target-date", new { targetDate = "2027-03-05", approvalId = "date-only" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Put("initiatives", item, "/target-date", new { targetDate = "2027-03-05", approvalId = "date-only" })).StatusCode);
    }

    [Fact]
    public async Task Writes_cannot_delete_edit_hours_or_attach_foreign_missing_or_cyclic_relationships()
    {
        await using var f = await Fixture.CreateAsync();
        var baseline = await f.ProtectedSnapshot();
        foreach (var collection in new[] { "initiatives", "projects", "activities" })
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await f.Client.DeleteAsync(Fixture.Path(collection, 1))).StatusCode);
        var activity = await f.Get("activities", 1);
        foreach (var extra in new object[] { new { title = "x", currentEstimateHours = 99 }, new { title = "x", plannedHours = 99 }, new { title = "x", originalPlannedHours = 99 }, new { title = "x", personId = 2 } })
            Assert.Equal(HttpStatusCode.BadRequest, (await f.Put("activities", activity, "", extra)).StatusCode);
        foreach (var id in new[] { 2, 999 })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await f.Put("activities", activity, "", new { title = "x", projectId = id })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await f.Put("activities", activity, "/dependencies", new { predecessorIds = new[] { id } })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Put("activities", activity, "/dependencies", new { predecessorIds = new[] { 1 } })).StatusCode);
        foreach (var hours in new[] { -1m, 0.001m, 100000000m })
            Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsJsonAsync(Fixture.Path("activities"), new { planId = "plan-a", title = "Bad hours", initialEstimateHours = hours })).StatusCode);
        await f.Approve(activity, "dependencies", new { predecessorIds = new[] { 5 } }, "dep");
        Assert.Equal(HttpStatusCode.OK, (await f.Put("activities", activity, "/dependencies", new { predecessorIds = new[] { 5 }, approvalId = "dep" })).StatusCode);
        var predecessor = await f.Get("activities", 5);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Put("activities", predecessor, "/dependencies", new { predecessorIds = new[] { 1 } })).StatusCode);
        Assert.Equal(baseline, await f.ProtectedSnapshot());
        await using var db = await f.Factory.CreateDbContextAsync();
        Assert.Single(await db.ActivityDependencies.ToListAsync());
        Assert.Single(await db.JarvisPlanningChanges.ToListAsync());
    }

    [Fact]
    public async Task Fingerprint_detects_interactive_edits_and_completion_closes_real_sessions_without_changing_estimates()
    {
        await using var f = await Fixture.CreateAsync();
        var old = await f.Get("activities", 1);
        await using (var db = await f.Factory.CreateDbContextAsync())
        {
            (await db.Activities.SingleAsync(x => x.Id == 1)).Title = "Interactive change";
            db.WorkRecords.Add(new WorkRecord { PersonId = 1, ActivityId = 1, Kind = WorkRecordKind.Session, Start = DateTimeOffset.UtcNow.AddHours(-1) });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await f.Put("activities", old, "/status", new { status = 2 })).StatusCode);
        var fresh = await f.Get("activities", 1);
        Assert.Equal(0, fresh.Revision);
        Assert.NotEqual(old.Fingerprint, fresh.Fingerprint);
        Assert.Equal(HttpStatusCode.OK, (await f.Put("activities", fresh, "/status", new { status = 2 })).StatusCode);
        await using var check = await f.Factory.CreateDbContextAsync();
        var session = await check.WorkRecords.SingleAsync(x => x.Kind == WorkRecordKind.Session);
        Assert.NotNull(session.End); Assert.InRange(session.Hours, 1, 1.01m);
        Assert.Equal(8m, (await check.Activities.SingleAsync(x => x.Id == 1)).CurrentEstimateHours);
        Assert.Equal(3m, (await check.PlanBlocks.SingleAsync()).PlannedHours);
        Assert.Single(await check.JarvisPlanningChanges.ToListAsync());
    }

    [Fact]
    public async Task Failed_history_insert_rolls_back_creation_update_and_approval_consumption_then_retry_succeeds()
    {
        await using var f = await Fixture.CreateAsync();
        var item = await f.Get("projects", 1);
        await f.Approve(item, "close", new { isClosed = true }, "close-once");
        await using (var db = await f.Factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_planning_history AFTER INSERT ON JarvisPlanningChanges BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await f.Client.PostAsJsonAsync(Fixture.Path("activities"), new { planId = "plan-a", title = "Rollback", initialEstimateHours = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await f.Post("projects", item, "/close", new { approvalId = "close-once" })).StatusCode);
        await using (var db = await f.Factory.CreateDbContextAsync())
        {
            Assert.Equal(8, await db.Activities.CountAsync());
            Assert.Empty(await db.JarvisPlanningStates.ToListAsync());
            Assert.Empty(await db.JarvisPlanningChanges.ToListAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_planning_history;");
        }
        Assert.Equal(HttpStatusCode.OK, (await f.Post("projects", item, "/close", new { approvalId = "close-once" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Post("projects", item, "/close", new { approvalId = "close-once" })).StatusCode);
        var created = await f.Create("initiatives", new { planId = "plan-a", name = "Long history", description = new string('x', 2000) });
        await f.RestartAsync();
        var history = await f.Client.GetFromJsonAsync<PlanningHistoryDto[]>(Fixture.Path("initiatives", created.Id) + "/history");
        Assert.True(Assert.Single(history!).NewValue.Length > 2000);
        Assert.Contains(new string('x', 2000), history![0].NewValue);
    }

    [Fact]
    public async Task Reassignment_removes_access_and_prevents_history_relationship_leaks()
    {
        await using var f = await Fixture.CreateAsync();
        var activity = await f.Get("activities", 1);
        await f.Approve(activity, "dependencies", new { predecessorIds = new[] { 5 } }, "dep");
        Assert.Equal(HttpStatusCode.OK, (await f.Put("activities", activity, "/dependencies", new { predecessorIds = new[] { 5 }, approvalId = "dep" })).StatusCode);
        await using (var db = await f.Factory.CreateDbContextAsync()) { (await db.Activities.SingleAsync(x => x.Id == 5)).PersonId = 2; await db.SaveChangesAsync(); }
        Assert.Empty((await f.Get("activities", 1)).PredecessorIds);
        Assert.Empty((await f.Client.GetFromJsonAsync<PlanningHistoryDto[]>(Fixture.Path("activities", 1) + "/history"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Client.GetAsync(Fixture.Path("activities", 5))).StatusCode);
        Assert.DoesNotContain(await f.List("initiatives"), x => x.Id == 1);
        var fresh = await f.Get("activities", 1);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Put("activities", fresh, "/status", new { status = 1 })).StatusCode);
    }

    [Fact]
    public async Task Planning_is_closed_by_default_and_cannot_change_fronts_configuration_or_existing_contract()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.Equal(HttpStatusCode.OK, (await f.Client.GetAsync(JarvisOpenFrontEndpoints.Prefix)).StatusCode);
        var front = await f.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix);
        Assert.Equal(1, Assert.Single(front!).Id);
        Assert.Equal(HttpStatusCode.OK, (await f.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/1/status", new { expectedRevision = 0, status = 1 })).StatusCode);
        await f.RestartAsync(config => config.Remove("Authentication:Jarvis:Planning:Enabled"));
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Client.GetAsync(Fixture.Path("activities"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await f.Client.GetAsync(JarvisOpenFrontEndpoints.Prefix)).StatusCode);
        await using var db = await f.Factory.CreateDbContextAsync();
        Assert.Single(await db.OpenFrontChanges.ToListAsync());
        Assert.Empty(await db.JarvisPlanningChanges.ToListAsync());
    }

    [Fact]
    public void Invalid_operator_plan_or_approval_configuration_fails_closed()
    {
        var config = Fixture.Configuration(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        foreach (var change in new[]
        {
            ("Authentication:Jarvis:Planning:Plans:0:ApprovalReference", ""),
            ("Authentication:Jarvis:Planning:OwnedInitiativeIds:0", "-1"),
            ("Authentication:Jarvis:Planning:Plans:0:InitiativeIds:0", "0"),
            ("Authentication:Jarvis:Planning:Approvals:0:Id", "incomplete")
        })
        {
            var modified = new Dictionary<string, string?>(config) { [change.Item1] = change.Item2 };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(modified).Build();
            Assert.Throws<InvalidOperationException>(() => JarvisPlanningAccess.Read(configuration, JarvisConnectorAccess.Read(configuration)));
        }
    }

    private sealed class Fixture(SqliteConnection connection, Dictionary<string, string?> configuration, string token) : IAsyncDisposable
    {
        private WebApplication app = null!;
        private int approvals;
        public HttpClient Client { get; private set; } = null!;
        public IDbContextFactory<ProjectManagerDbContext> Factory => app.Services.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
        public static string Path(string collection, int? id = null) => JarvisPlanningEndpoints.Prefix + "/" + collection + (id is null ? "" : "/" + id);
        public void Authenticate() => Client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        public Task<PlanningItemDto[]> List(string collection) => Client.GetFromJsonAsync<PlanningItemDto[]>(Path(collection))!;
        public async Task<PlanningItemDto> Get(string collection, int id) => (await Client.GetFromJsonAsync<PlanningItemDto>(Path(collection, id)))!;
        public async Task<PlanningItemDto> Create(string collection, object body)
        {
            var response = await Client.PostAsJsonAsync(Path(collection), body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            return (await response.Content.ReadFromJsonAsync<PlanningItemDto>())!;
        }
        private static Dictionary<string, object?> Body(PlanningItemDto item, object extra)
        {
            var body = new Dictionary<string, object?> { ["planId"] = "plan-a", ["expectedRevision"] = item.Revision, ["expectedFingerprint"] = item.Fingerprint };
            foreach (var property in JsonSerializer.SerializeToElement(extra).EnumerateObject()) body[property.Name] = property.Value.Clone();
            return body;
        }
        public Task<HttpResponseMessage> Put(string collection, PlanningItemDto item, string suffix, object extra) => Client.PutAsJsonAsync(Path(collection, item.Id) + suffix, Body(item, extra));
        public Task<HttpResponseMessage> Post(string collection, PlanningItemDto item, string suffix, object extra) => Client.PostAsJsonAsync(Path(collection, item.Id) + suffix, Body(item, extra));
        public async Task Approve(PlanningItemDto item, string operation, object values, string id)
        {
            var prefix = "Authentication:Jarvis:Planning:Approvals:" + approvals++ + ":";
            await RestartAsync(config =>
            {
                foreach (var (key, value) in new Dictionary<string, string?>
                {
                    ["Id"] = id, ["ApprovalReference"] = "Edgar-specific-approval", ["PlanId"] = "plan-a", ["PersonId"] = "1",
                    ["EntityKind"] = item.Kind, ["EntityId"] = item.Id.ToString(), ["Operation"] = operation,
                    ["ExpectedRevision"] = item.Revision.ToString(), ["ExpectedFingerprint"] = item.Fingerprint,
                    ["ValuesSha256"] = JarvisPlanningAccess.ValuesHash(values)
                }) config[prefix + key] = value;
            });
        }
        public static Dictionary<string, string?> Configuration(string token) => new()
        {
            ["Authentication:Jarvis:Enabled"] = "true", ["Authentication:Jarvis:ConnectorId"] = "jarvis-planning-test",
            ["Authentication:Jarvis:TokenSha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))),
            ["Authentication:Jarvis:PersonId"] = "1", ["Authentication:Jarvis:AllowedFrontIds:0"] = "1",
            ["Authentication:Jarvis:Planning:Enabled"] = "true", ["Authentication:Jarvis:Planning:OwnedInitiativeIds:0"] = "1",
            ["Authentication:Jarvis:Planning:OwnedInitiativeIds:1"] = "3", ["Authentication:Jarvis:Planning:Plans:0:Id"] = "plan-a",
            ["Authentication:Jarvis:Planning:Plans:0:ApprovalReference"] = "approval-of-plan-a",
            ["Authentication:Jarvis:Planning:Plans:0:InitiativeIds:0"] = "1",
            ["Authentication:Jarvis:Planning:Plans:0:AllowCreateInitiatives"] = "true", ["Authentication:Jarvis:Planning:Plans:0:AllowCreateProjects"] = "true",
            ["Authentication:Jarvis:Planning:Plans:0:AllowCreateActivities"] = "true", ["Authentication:Jarvis:Planning:Plans:0:AllowIndependentProjects"] = "true",
            ["Authentication:Jarvis:Planning:Plans:0:AllowLooseActivities"] = "true"
        };
        public static async Task<Fixture> CreateAsync()
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var f = new Fixture(connection, Configuration(token), token);
            await f.StartAsync();
            await using var db = await f.Factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
            db.People.Add(new Person { Id = 2, Name = "Synthetic other", IsActive = true });
            db.Initiatives.AddRange(Enumerable.Range(1, 4).Select(id => new Initiative { Id = id, Name = "Initiative " + id }));
            db.Projects.AddRange(new Project { Id = 1, Name = "Own", InitiativeId = 1, PersonId = 1 }, new Project { Id = 2, Name = "Other", InitiativeId = 2, PersonId = 2 },
                new Project { Id = 3, Name = "Mixed own", InitiativeId = 3, PersonId = 1 }, new Project { Id = 4, Name = "Mixed other", InitiativeId = 3, PersonId = 2 },
                new Project { Id = 5, Name = "Outside plan", PersonId = 1 }, new Project { Id = 6, Name = "Foreign context", PersonId = 1, InitiativeId = 2 });
            db.Activities.AddRange(new Activity { Id = 1, Title = "Own", ProjectId = 1, PersonId = 1, CurrentEstimateHours = 8 }, new Activity { Id = 2, Title = "Other", ProjectId = 2, PersonId = 2 },
                new Activity { Id = 3, Title = "Mixed own", ProjectId = 3, PersonId = 1 }, new Activity { Id = 4, Title = "Loose outside", PersonId = 1 },
                new Activity { Id = 5, Title = "Own predecessor", ProjectId = 1, PersonId = 1, CurrentEstimateHours = 4 }, new Activity { Id = 6, Title = "Mixed other", ProjectId = 4, PersonId = 2 },
                new Activity { Id = 7, Title = "Foreign context", ProjectId = 6, PersonId = 1 }, new Activity { Id = 8, Title = "Outside plan", ProjectId = 5, PersonId = 1 });
            db.WeeklyPlans.Add(new WeeklyPlan { Id = 1, PersonId = 1, WeekStart = new DateOnly(2026, 11, 23) });
            db.PlanBlocks.Add(new PlanBlock { Id = 1, WeeklyPlanId = 1, ActivityId = 1, PlannedHours = 3 });
            db.WorkRecords.Add(new WorkRecord { Id = 1, PersonId = 1, ActivityId = 1, Kind = WorkRecordKind.Manual, Hours = 1, Note = "Original" });
            db.OpenFronts.Add(new OpenFront { Id = 1, PersonId = 1, Title = "Existing front" });
            await db.SaveChangesAsync();
            return f;
        }
        private async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders(); builder.WebHost.UseTestServer(); builder.Configuration.AddInMemoryCollection(configuration);
            builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", options => options.Cookie.Name = "RaceIT.PlanningTest");
            builder.Services.AddJarvisConnector(builder.Configuration); builder.Services.AddJarvisPlanning(builder.Configuration);
            builder.Services.AddDbContextFactory<ProjectManagerDbContext>(options => options.UseSqlite(connection));
            builder.Services.AddScoped<OpenFrontService>();
            app = builder.Build(); app.UseJarvisApiErrors(); app.UseAuthentication(); app.UseAuthorization();
            app.MapJarvisOpenFronts(); app.MapJarvisPlanning(); app.MapGet("/browser-only", () => "interactive").RequireAuthorization();
            await app.StartAsync(); Client = app.GetTestClient(); Client.BaseAddress = new Uri("https://localhost"); Authenticate();
        }
        public async Task RestartAsync(Action<Dictionary<string, string?>>? change = null)
        {
            Client.Dispose(); await app.StopAsync(); await app.DisposeAsync(); change?.Invoke(configuration); await StartAsync();
        }
        public string BrowserCookie()
        {
            var options = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Cookies");
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("PersonId", "1"), new Claim("auth_source", "google")], "Cookies"));
            return "RaceIT.PlanningTest=" + options.TicketDataFormat.Protect(new AuthenticationTicket(user, "Cookies"));
        }
        public async Task<string> ProtectedSnapshot()
        {
            await using var db = await Factory.CreateDbContextAsync();
            return JsonSerializer.Serialize(new
            {
                Estimates = await db.Activities.Where(x => x.Id <= 8).OrderBy(x => x.Id).Select(x => new { x.Id, x.CurrentEstimateHours }).ToListAsync(),
                Blocks = await db.PlanBlocks.OrderBy(x => x.Id).Select(x => new { x.Id, x.ActivityId, x.PlannedHours, x.Position }).ToListAsync(),
                Records = await db.WorkRecords.OrderBy(x => x.Id).Select(x => new { x.Id, x.ActivityId, x.Hours, x.Note, x.Start, x.End }).ToListAsync()
            });
        }
        public async ValueTask DisposeAsync() { Client.Dispose(); await app.StopAsync(); await app.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
