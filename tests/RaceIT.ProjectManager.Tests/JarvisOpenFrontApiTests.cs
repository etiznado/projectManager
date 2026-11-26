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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaceIT.ProjectManager.Api;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class JarvisOpenFrontApiTests
{
    [Fact]
    public async Task List_is_limited_to_explicit_grants_and_active_configured_owner()
    {
        await using var fixture = await Fixture.CreateAsync([1, 3]);
        var rows = await fixture.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix);
        var front = Assert.Single(rows!);
        Assert.Equal(1, front.Id);
        Assert.Equal("Contexto original", front.Context);
        Assert.Equal("Avance original", front.LastAdvance);
        Assert.Equal(0, front.Revision);
        Assert.Equal("no-store", (await fixture.Client.GetAsync(JarvisOpenFrontEndpoints.Prefix)).Headers.CacheControl!.ToString());
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.OpenFronts.SingleAsync(x => x.Id == 1)).PersonId = 2;
            await db.SaveChangesAsync();
        }
        Assert.Empty((await fixture.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix))!);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.People.SingleAsync(x => x.Id == 1)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Client.GetAsync(JarvisOpenFrontEndpoints.Prefix)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.StatusAsync(1, 0, 2)).StatusCode);
        await using var empty = await Fixture.CreateAsync([]);
        Assert.Empty((await empty.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix))!);
    }

    [Fact]
    public async Task Real_authentication_rejects_missing_invalid_cookie_only_multiple_and_insecure_credentials()
    {
        await using var fixture = await Fixture.CreateAsync();
        var client = fixture.Client;
        client.DefaultRequestHeaders.Authorization = null;
        foreach (var header in new[] { "", "Bearer wrong", "Basic ignored", $"Bearer {fixture.Token}, Bearer {fixture.Token}" })
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            if (header.Length > 0) client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", header);
            foreach (var response in new[] { await client.GetAsync(JarvisOpenFrontEndpoints.Prefix), await fixture.StatusAsync(1, 0, 2) })
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Null(response.Headers.Location);
                Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
                Assert.Equal("{\"error\":\"unauthenticated\"}", await response.Content.ReadAsStringAsync());
            }
        }
        // Send malformed raw headers through TestServer: HttpClient canonicalizes Authorization whitespace.
        Assert.Equal(401, await fixture.RawHeaderStatusAsync([$"Bearer  {fixture.Token}"]));
        Assert.Equal(401, await fixture.RawHeaderStatusAsync([$"Bearer {fixture.Token}", $"Bearer {fixture.Token}"]));
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Cookie", fixture.BrowserCookie());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/browser-only")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(JarvisOpenFrontEndpoints.Prefix)).StatusCode);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.Token);
        Assert.NotEqual(HttpStatusCode.OK, (await client.GetAsync("/browser-only")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("http://localhost" + JarvisOpenFrontEndpoints.Prefix)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(JarvisOpenFrontEndpoints.Prefix + "?access_token=" + fixture.Token)).StatusCode);
        await using var db = await fixture.Factory.CreateDbContextAsync();
        Assert.Empty(await db.OpenFrontChanges.ToListAsync());
        Assert.Equal(0, (await db.OpenFronts.SingleAsync(x => x.Id == 1)).Revision);
    }

    [Fact]
    public async Task Default_disabled_and_invalid_operator_configuration_fail_closed()
    {
        await using var fixture = await Fixture.CreateAsync(enabled: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync(JarvisOpenFrontEndpoints.Prefix)).StatusCode);
        foreach (var change in new Dictionary<string, string?>[]
        {
            new() { ["Authentication:Jarvis:ConnectorId"] = "" },
            new() { ["Authentication:Jarvis:TokenSha256"] = "not-a-hash" },
            new() { ["Authentication:Jarvis:PersonId"] = "0" },
            new() { ["Authentication:Jarvis:AllowedFrontIds:0"] = "-1" },
            new() { ["Authentication:Jarvis:AllowedFrontIds:0"] = "1", ["Authentication:Jarvis:AllowedFrontIds:1"] = "1" }
        })
        {
            var values = Fixture.Configuration(fixture.Token, [1], true);
            foreach (var entry in change) values[entry.Key] = entry.Value;
            Assert.Throws<InvalidOperationException>(() => JarvisConnectorAccess.Read(new ConfigurationBuilder().AddInMemoryCollection(values).Build()));
        }
    }

    [Fact]
    public async Task All_three_operations_persist_and_are_visible_to_existing_app_service_without_planning_changes()
    {
        await using var fixture = await Fixture.CreateAsync();
        var planning = await fixture.PlanningSnapshotAsync();
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(1, 0, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/1/advances", new { expectedRevision = 1, text = "Segunda prueba terminada" })).StatusCode);
        var response = await fixture.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/1/next-step", new { expectedRevision = 2, text = "Elegir alternativa B" });
        var saved = await response.Content.ReadFromJsonAsync<OpenFrontDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, saved!.Revision);
        Assert.Equal(OpenFrontStatus.NeedsDecision, saved.Status);
        Assert.Equal("Segunda prueba terminada", saved.LastAdvance);
        Assert.Equal("Elegir alternativa B", saved.NextStep);
        Assert.Equal("Contexto original", saved.Context);
        Assert.Equal("Frente propio", saved.Title);
        var browserRows = await new OpenFrontService(fixture.Factory).ListAsync(Fixture.Person());
        var reopened = browserRows.Single(x => x.Id == 1);
        Assert.Equal(saved.LastAdvance, reopened.LastAdvance);
        Assert.Equal(saved.NextStep, reopened.NextStep);
        Assert.Equal(saved.Status, reopened.Status);
        Assert.Equal(saved.Revision, reopened.Revision);
        Assert.Equal("Frente sin permiso", browserRows.Single(x => x.Id == 2).Title);
        Assert.Equal(planning, await fixture.PlanningSnapshotAsync());
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/1/next-step", new { expectedRevision = 3, text = "" })).StatusCode);
    }

    [Fact]
    public async Task Missing_ungranted_and_foreign_ids_are_indistinguishable_and_request_cannot_expand_grants()
    {
        await using var fixture = await Fixture.CreateAsync([1, 3]);
        string? error = null;
        foreach (var id in new[] { 2, 3, 999 })
        {
            var response = await fixture.StatusAsync(id, 0, 2);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            if (error is null) error = content;
            Assert.Equal(error, content);
        }
        var forged = await fixture.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/1/status", new { expectedRevision = 0, status = 2, personId = 2, allowedFrontIds = new[] { 2, 3 } });
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal("application/json", forged.Content.Headers.ContentType!.MediaType);
        await using var db = await fixture.Factory.CreateDbContextAsync();
        Assert.Empty(await db.OpenFrontChanges.ToListAsync());
        Assert.All(await db.OpenFronts.ToListAsync(), front => Assert.Equal(0, front.Revision));
    }

    [Fact]
    public async Task Durable_history_preserves_previous_values_and_distinguishes_connector_from_interactive_actor()
    {
        await using var fixture = await Fixture.CreateAsync();
        var since = DateTimeOffset.UtcNow;
        await new OpenFrontService(fixture.Factory).ChangeStatusAsync(Fixture.Person(), 1, OpenFrontStatus.NeedsDecision, 0);
        await fixture.StatusAsync(1, 1, 2);
        await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/1/advances", new { expectedRevision = 2, text = "Esperando pieza" });
        await using var db = await fixture.Factory.CreateDbContextAsync();
        var history = await db.OpenFrontChanges.AsNoTracking().OrderBy(x => x.Revision).ToListAsync();
        Assert.Equal(3, history.Count);
        Assert.Equal(("person", "1", "status", "0", "1", 1L), Values(history[0]));
        Assert.Equal(("connector", "jarvis-test", "status", "1", "2", 2L), Values(history[1]));
        Assert.Equal(("connector", "jarvis-test", "lastAdvance", "Avance original", "Esperando pieza", 3L), Values(history[2]));
        foreach (var row in history)
        {
            Assert.Equal(1, row.OpenFrontId);
            Assert.Equal(1, row.PersonId);
            Assert.InRange(row.AtUtc, since, DateTimeOffset.UtcNow);
            Assert.Equal(TimeSpan.Zero, row.AtUtc.Offset);
        }
        var serialized = JsonSerializer.Serialize(history);
        Assert.DoesNotContain(fixture.Token, serialized);
        Assert.DoesNotContain(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Token))), serialized);
        var front = await db.OpenFronts.SingleAsync(x => x.Id == 1);
        Assert.Equal(OpenFrontStatus.Waiting, front.Status);
        Assert.Equal("Esperando pieza", front.LastAdvance);
    }

    [Fact]
    public async Task Invalid_inputs_and_stale_revisions_return_JSON_errors_without_overwrite()
    {
        await using var fixture = await Fixture.CreateAsync();
        var requests = new (string Path, string Json, HttpMethod Method)[]
        {
            ("status", "{\"expectedRevision\":0,\"status\":99}", HttpMethod.Put),
            ("status", "{\"status\":2}", HttpMethod.Put),
            ("status", "{\"expectedRevision\":-1,\"status\":2}", HttpMethod.Put),
            ("status", "{\"expectedRevision\":0}", HttpMethod.Put),
            ("status", "null", HttpMethod.Put),
            ("status", "{", HttpMethod.Put),
            ("advances", JsonSerializer.Serialize(new { expectedRevision = 0, text = new string('x', 2001) }), HttpMethod.Post),
            ("advances", "{\"expectedRevision\":0,\"text\":\"   \"}", HttpMethod.Post),
            ("advances", "{\"expectedRevision\":0,\"text\":null}", HttpMethod.Post),
            ("next-step", JsonSerializer.Serialize(new { expectedRevision = 0, text = new string('x', 2001) }), HttpMethod.Put),
            ("next-step", "{\"expectedRevision\":0,\"text\":null}", HttpMethod.Put)
        };
        foreach (var request in requests)
        {
            var response = await fixture.Client.SendAsync(new HttpRequestMessage(request.Method, JarvisOpenFrontEndpoints.Prefix + "/1/" + request.Path)
            { Content = new StringContent(request.Json, Encoding.UTF8, "application/json") });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        }
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(1, 0, 2)).StatusCode);
        var stale = await fixture.StatusAsync(1, 0, 1);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("{\"error\":\"revision_conflict\"}", await stale.Content.ReadAsStringAsync());
        await using var db = await fixture.Factory.CreateDbContextAsync();
        Assert.Single(await db.OpenFrontChanges.ToListAsync());
        Assert.Equal(OpenFrontStatus.Waiting, (await db.OpenFronts.SingleAsync(x => x.Id == 1)).Status);
    }

    [Fact]
    public async Task Relational_history_insert_failure_rolls_back_entire_mutation_and_retry_succeeds()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = await fixture.Factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_history AFTER INSERT ON OpenFrontChanges BEGIN SELECT RAISE(ABORT, 'synthetic history failure'); END;");
        var response = await fixture.StatusAsync(1, 0, 2);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("{\"error\":\"persistence_failed\"}", await response.Content.ReadAsStringAsync());
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            var front = await db.OpenFronts.SingleAsync(x => x.Id == 1);
            Assert.Equal(OpenFrontStatus.InProgress, front.Status);
            Assert.Equal(0, front.Revision);
            Assert.Empty(await db.OpenFrontChanges.ToListAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_history;");
        }
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(1, 0, 2)).StatusCode);
        await using var check = await fixture.Factory.CreateDbContextAsync();
        Assert.Equal(1, (await check.OpenFronts.SingleAsync(x => x.Id == 1)).Revision);
        Assert.Single(await check.OpenFrontChanges.ToListAsync());
    }

    [Fact]
    public async Task A_writer_that_loses_between_read_and_save_returns_conflict_and_keeps_only_winner_history()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Race.BeforeNextSave = async () =>
            await new OpenFrontService(fixture.Factory).ChangeStatusAsync(Fixture.Person(), 1, OpenFrontStatus.Waiting, 0);
        var response = await fixture.StatusAsync(1, 0, 1);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = await fixture.Factory.CreateDbContextAsync();
        var saved = await db.OpenFronts.SingleAsync(x => x.Id == 1);
        Assert.Equal(OpenFrontStatus.Waiting, saved.Status);
        Assert.Equal(1, saved.Revision);
        var history = Assert.Single(await db.OpenFrontChanges.ToListAsync());
        Assert.Equal("person", history.ActorKind);
        Assert.Equal("2", history.NewValue);
    }

    [Fact]
    public async Task Creation_returns_201_with_defaults_or_full_fields_and_one_atomic_attributable_event()
    {
        await using var fixture = await Fixture.CreateAsync();
        var planning = await fixture.PlanningSnapshotAsync();
        var before = DateTimeOffset.UtcNow;
        var response = await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Preparar prueba" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<OpenFrontDto>())!;
        Assert.True(created.Id > 3);
        Assert.Equal("Preparar prueba", created.Title);
        Assert.Null(created.Context);
        Assert.Equal(OpenFrontStatus.InProgress, created.Status);
        Assert.Equal("", created.LastAdvance);
        Assert.Equal("", created.NextStep);
        Assert.Equal(1, created.Revision);
        Assert.InRange(created.UpdatedAtUtc, before, DateTimeOffset.UtcNow);
        var full = new { title = new string('t', 200), context = new string('c', 200), status = 2, lastAdvance = new string('a', 2000), nextStep = new string('n', 2000) };
        var fullResponse = await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, full);
        Assert.Equal(HttpStatusCode.Created, fullResponse.StatusCode);
        var savedFull = (await fullResponse.Content.ReadFromJsonAsync<OpenFrontDto>())!;
        Assert.Equal(full.title, savedFull.Title);
        Assert.Equal(full.context, savedFull.Context);
        Assert.Equal(OpenFrontStatus.Waiting, savedFull.Status);
        Assert.Equal(full.lastAdvance, savedFull.LastAdvance);
        Assert.Equal(full.nextStep, savedFull.NextStep);
        await using var db = await fixture.Factory.CreateDbContextAsync();
        var card = await db.OpenFronts.SingleAsync(x => x.Id == created.Id);
        var history = await db.OpenFrontChanges.Where(x => x.OpenFrontId == created.Id).ToListAsync();
        var entry = Assert.Single(history);
        Assert.Equal(1, card.PersonId);
        Assert.Equal(("connector", "jarvis-test", "created", "", card.Title, 1L), Values(entry));
        Assert.Equal(card.Id, entry.OpenFrontId);
        Assert.Equal(card.PersonId, entry.PersonId);
        Assert.Equal(card.UpdatedAtUtc, entry.AtUtc);
        Assert.Equal(TimeSpan.Zero, entry.AtUtc.Offset);
        Assert.Equal("Frente propio", (await db.OpenFronts.SingleAsync(x => x.Id == 1)).Title);
        Assert.Equal(planning, await fixture.PlanningSnapshotAsync());
        Assert.Equal(2, await db.OpenFrontChanges.CountAsync());
        Assert.Contains(await new OpenFrontService(fixture.Factory).ListAsync(Fixture.Person()), x => x.Id == created.Id);
    }

    [Fact]
    public async Task Invalid_or_null_creation_fields_leave_no_front_or_creation_history()
    {
        await using var fixture = await Fixture.CreateAsync();
        var invalid = new[]
        {
            "{}", "null", "{\"title\":null}", "{\"title\":\"\"}", "{\"title\":\"  \"}",
            JsonSerializer.Serialize(new { title = new string('x', 201) }),
            JsonSerializer.Serialize(new { title = "Valid", context = new string('x', 201) }),
            "{\"title\":\"Valid\",\"status\":99}", "{\"title\":\"Valid\",\"status\":null}",
            JsonSerializer.Serialize(new { title = "Valid", lastAdvance = new string('x', 2001) }),
            JsonSerializer.Serialize(new { title = "Valid", nextStep = new string('x', 2001) }),
            "{\"title\":\"Valid\",\"lastAdvance\":null}", "{\"title\":\"Valid\",\"nextStep\":null}"
        };
        foreach (var json in invalid)
        {
            var response = await fixture.Client.PostAsync(JarvisOpenFrontEndpoints.Prefix, new StringContent(json, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        await using var db = await fixture.Factory.CreateDbContextAsync();
        Assert.Equal(3, await db.OpenFronts.CountAsync());
        Assert.Empty(await db.OpenFrontChanges.ToListAsync());
    }

    [Fact]
    public async Task Creation_authentication_inactive_owner_and_forged_server_fields_are_rejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        foreach (var auth in new AuthenticationHeaderValue?[] { null, new("Bearer", "invalid") })
        {
            fixture.Client.DefaultRequestHeaders.Authorization = auth;
            Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Valid" })).StatusCode);
        }
        fixture.Client.DefaultRequestHeaders.Authorization = null;
        fixture.Client.DefaultRequestHeaders.Add("Cookie", fixture.BrowserCookie());
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Valid" })).StatusCode);
        fixture.Client.DefaultRequestHeaders.Remove("Cookie");
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.Token);
        foreach (var field in new[] { "id", "personId", "ownerId", "allowedFrontIds", "actorId", "revision", "updatedAtUtc" })
        {
            var response = await fixture.Client.PostAsync(JarvisOpenFrontEndpoints.Prefix,
                new StringContent("{\"title\":\"Valid\",\"" + field + "\":1}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.People.SingleAsync(x => x.Id == 1)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Valid" })).StatusCode);
        await using var check = await fixture.Factory.CreateDbContextAsync();
        Assert.Equal(3, await check.OpenFronts.CountAsync());
        Assert.Empty(await check.OpenFrontChanges.ToListAsync());
    }

    [Fact]
    public async Task Creation_provenance_survives_new_service_and_grants_all_existing_updates_without_changing_config()
    {
        await using var fixture = await Fixture.CreateAsync([]);
        var response = await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Creado por Jarvis", context = (string?)null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var front = (await response.Content.ReadFromJsonAsync<OpenFrontDto>())!;
        var access = JarvisConnectorAccess.Read(new ConfigurationBuilder().AddInMemoryCollection(Fixture.Configuration(fixture.Token, [], true)).Build());
        var fresh = new OpenFrontService(fixture.Factory, access);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "connector:jarvis-test"), new Claim("auth_source", "jarvis")], JarvisConnectorAccess.Scheme));
        Assert.Equal(front.Id, Assert.Single(await fresh.ListForConnectorAsync(actor)).Id);
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(front.Id, 1, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + $"/{front.Id}/advances", new { expectedRevision = 2, text = "Avance nuevo" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + $"/{front.Id}/next-step", new { expectedRevision = 3, text = "Paso nuevo" })).StatusCode);
        var reopened = Assert.Single(await new OpenFrontService(fixture.Factory, access).ListForConnectorAsync(actor));
        Assert.Equal(4, reopened.Revision);
        Assert.Equal("Avance nuevo", reopened.LastAdvance);
        Assert.Equal("Paso nuevo", reopened.NextStep);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.StatusAsync(1, 0, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.StatusAsync(2, 0, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.StatusAsync(3, 0, 2)).StatusCode);
    }

    [Fact]
    public async Task Unrelated_history_other_connector_wrong_provenance_and_reassigned_front_do_not_grant_access()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            db.OpenFrontChanges.Add(new OpenFrontChange { OpenFrontId = 2, PersonId = 1, ActorKind = "connector", ActorId = "jarvis-test", Field = "status", OldValue = "0", NewValue = "1", Revision = 1 });
            db.OpenFrontChanges.Add(new OpenFrontChange { OpenFrontId = 3, PersonId = 1, ActorKind = "connector", ActorId = "jarvis-test", Field = "created", OldValue = "", NewValue = "Foreign", Revision = 1 });
            foreach (var (id, actor, owner, kind, revision, old) in new[]
            {
                (4, "another-connector", 1, "connector", 1L, ""), (5, "jarvis-test", 2, "connector", 1L, ""),
                (6, "jarvis-test", 1, "connector", 2L, ""), (7, "jarvis-test", 1, "connector", 1L, "not empty"),
                (8, "jarvis-test", 1, "person", 1L, ""), (9, "JARVIS-TEST", 1, "connector", 1L, "")
            })
            {
                var card = new OpenFront { Id = id, PersonId = 1, Title = "No autorizado" };
                db.OpenFronts.Add(card);
                db.OpenFrontChanges.Add(new OpenFrontChange { OpenFront = card, PersonId = owner, ActorKind = kind, ActorId = actor, Field = "created", OldValue = old, NewValue = card.Title, Revision = revision });
            }
            await db.SaveChangesAsync();
        }
        Assert.Equal(1, Assert.Single((await fixture.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix))!).Id);
        foreach (var id in Enumerable.Range(2, 8)) Assert.Equal(HttpStatusCode.NotFound, (await fixture.StatusAsync(id, 0, 2)).StatusCode);
        var response = await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Nuevo legítimo" });
        var created = (await response.Content.ReadFromJsonAsync<OpenFrontDto>())!;
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.OpenFronts.SingleAsync(x => x.Id == created.Id)).PersonId = 2;
            await db.SaveChangesAsync();
        }
        Assert.Equal(1, Assert.Single((await fixture.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix))!).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.StatusAsync(created.Id, 1, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + $"/{created.Id}/advances", new { expectedRevision = 1, text = "Denied" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + $"/{created.Id}/next-step", new { expectedRevision = 1, text = "Denied" })).StatusCode);
    }

    [Fact]
    public async Task Creation_history_insert_failure_rolls_back_new_card_and_retry_commits_exactly_one_pair()
    {
        await using var fixture = await Fixture.CreateAsync();
        var planning = await fixture.PlanningSnapshotAsync();
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(1, 0, 2)).StatusCode);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_creation AFTER INSERT ON OpenFrontChanges WHEN NEW.Field = 'created' BEGIN SELECT RAISE(ABORT, 'synthetic creation event failure'); END;");
        var failed = await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Rollback creation" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            Assert.Equal(3, await db.OpenFronts.CountAsync());
            var previous = Assert.Single(await db.OpenFrontChanges.ToListAsync());
            Assert.Equal("status", previous.Field);
            Assert.Equal("2", previous.NewValue);
            Assert.Equal(OpenFrontStatus.Waiting, (await db.OpenFronts.SingleAsync(x => x.Id == 1)).Status);
            Assert.DoesNotContain(await db.OpenFronts.ToListAsync(), x => x.Title == "Rollback creation");
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_creation;");
        }
        var retry = await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Rollback creation" });
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        await using var check = await fixture.Factory.CreateDbContextAsync();
        var card = Assert.Single(await check.OpenFronts.Where(x => x.Title == "Rollback creation").ToListAsync());
        Assert.Equal(2, await check.OpenFrontChanges.CountAsync());
        var creation = Assert.Single(await check.OpenFrontChanges.Where(x => x.Field == "created").ToListAsync());
        Assert.Equal(card.Id, creation.OpenFrontId);
        Assert.Equal(4, await check.OpenFronts.CountAsync());
        Assert.Equal(planning, await fixture.PlanningSnapshotAsync());
    }

    [Fact]
    public async Task Owner_wide_access_includes_existing_and_later_owner_cards_without_grants_or_creation_history()
    {
        await using var fixture = await Fixture.CreateAsync([], allowAllOwnerFronts: true);
        var planning = await fixture.PlanningSnapshotAsync();
        Assert.Equal(new[] { 1, 2 }, (await fixture.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix))!.Select(x => x.Id));
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            db.OpenFronts.Add(new OpenFront { Id = 4, PersonId = 1, Title = "Later owner card without provenance" });
            await db.SaveChangesAsync();
        }
        Assert.Equal(new[] { 1, 2, 4 }, (await fixture.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix))!.Select(x => x.Id));
        await using var check = await fixture.Factory.CreateDbContextAsync();
        Assert.Empty(await check.OpenFrontChanges.ToListAsync());
        Assert.All(await check.OpenFronts.ToListAsync(), x => Assert.Equal(0, x.Revision));
        Assert.Equal(planning, await fixture.PlanningSnapshotAsync());
    }

    [Fact]
    public async Task Owner_wide_updates_on_ungranted_card_preserve_other_fields_and_atomic_attributable_history()
    {
        await using var fixture = await Fixture.CreateAsync([], allowAllOwnerFronts: true);
        var planning = await fixture.PlanningSnapshotAsync();
        var since = DateTimeOffset.UtcNow;
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(2, 0, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/2/advances", new { expectedRevision = 1, text = "Revisión terminada" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/2/next-step", new { expectedRevision = 2, text = "Preparar siguiente prueba" })).StatusCode);
        await using var check = await fixture.Factory.CreateDbContextAsync();
        var card = await check.OpenFronts.SingleAsync(x => x.Id == 2);
        Assert.Equal((OpenFrontStatus.NeedsDecision, "Revisión terminada", "Preparar siguiente prueba", 3L), (card.Status, card.LastAdvance, card.NextStep, card.Revision));
        Assert.Equal("Frente sin permiso", card.Title);
        Assert.Null(card.Context);
        var rows = await check.OpenFrontChanges.OrderBy(x => x.Revision).ToListAsync();
        Assert.Equal(new[] { ("connector", "jarvis-test", "status", "0", "1", 1L),
            ("connector", "jarvis-test", "lastAdvance", "", "Revisión terminada", 2L),
            ("connector", "jarvis-test", "nextStep", "", "Preparar siguiente prueba", 3L) }, rows.Select(Values));
        Assert.All(rows, row => { Assert.Equal(2, row.OpenFrontId); Assert.Equal(1, row.PersonId); Assert.Equal(TimeSpan.Zero, row.AtUtc.Offset); Assert.InRange(row.AtUtc, since, DateTimeOffset.UtcNow); });
        Assert.All(await check.OpenFronts.Where(x => x.Id != 2).ToListAsync(), x => Assert.Equal(0, x.Revision));
        Assert.Equal(planning, await fixture.PlanningSnapshotAsync());
    }

    [Fact]
    public async Task Owner_wide_access_still_rejects_foreign_reassigned_missing_inactive_and_unauthenticated_requests()
    {
        await using var fixture = await Fixture.CreateAsync([], allowAllOwnerFronts: true);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.OpenFronts.SingleAsync(x => x.Id == 2)).PersonId = 2;
            await db.SaveChangesAsync();
        }
        Assert.Equal(1, Assert.Single((await fixture.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix))!).Id);
        foreach (var id in new[] { 2, 3, 999 })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await fixture.StatusAsync(id, 0, 1)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + $"/{id}/advances", new { expectedRevision = 0, text = "Denied" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + $"/{id}/next-step", new { expectedRevision = 0, text = "Denied" })).StatusCode);
        }
        foreach (var auth in new AuthenticationHeaderValue?[] { null, new("Bearer", "invalid") })
        {
            fixture.Client.DefaultRequestHeaders.Authorization = auth;
            Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync(JarvisOpenFrontEndpoints.Prefix)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.StatusAsync(1, 0, 1)).StatusCode);
        }
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.Token);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            (await db.People.SingleAsync(x => x.Id == 1)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Client.GetAsync(JarvisOpenFrontEndpoints.Prefix)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.StatusAsync(1, 0, 1)).StatusCode);
        await using var check = await fixture.Factory.CreateDbContextAsync();
        Assert.Empty(await check.OpenFrontChanges.ToListAsync());
        Assert.All(await check.OpenFronts.ToListAsync(), x => Assert.Equal(0, x.Revision));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public async Task Restricted_mode_retains_grants_and_creation_and_client_cannot_enable_owner_wide_access(bool? mode)
    {
        await using var fixture = await Fixture.CreateAsync(allowAllOwnerFronts: mode);
        var created = (await (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Trusted creation" })).Content.ReadFromJsonAsync<OpenFrontDto>())!;
        Assert.Equal(new[] { 1, created.Id }, (await fixture.Client.GetFromJsonAsync<OpenFrontDto[]>(JarvisOpenFrontEndpoints.Prefix))!.Select(x => x.Id));
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(1, 0, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(created.Id, 1, 1)).StatusCode);
        foreach (var id in new[] { 2, 3 }) Assert.Equal(HttpStatusCode.NotFound, (await fixture.StatusAsync(id, 0, 1)).StatusCode);
        var rowsBefore = 3;
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.PutAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/2/status", new { expectedRevision = 0, status = 1, allowAllOwnerFronts = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix, new { title = "Forged", allowAllOwnerFronts = true })).StatusCode);
        await using var check = await fixture.Factory.CreateDbContextAsync();
        Assert.Equal(rowsBefore, await check.OpenFrontChanges.CountAsync());
        Assert.Equal(4, await check.OpenFronts.CountAsync());
    }

    [Fact]
    public async Task Owner_wide_updates_keep_validation_stale_revision_and_history_failure_rollback()
    {
        await using var fixture = await Fixture.CreateAsync([], allowAllOwnerFronts: true);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.StatusAsync(2, 0, 99)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.PostAsJsonAsync(JarvisOpenFrontEndpoints.Prefix + "/2/advances", new { expectedRevision = 0, text = "  " })).StatusCode);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_owner_history AFTER INSERT ON OpenFrontChanges BEGIN SELECT RAISE(ABORT, 'synthetic history failure'); END;");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await fixture.StatusAsync(2, 0, 1)).StatusCode);
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            Assert.Equal(0, (await db.OpenFronts.SingleAsync(x => x.Id == 2)).Revision);
            Assert.Empty(await db.OpenFrontChanges.ToListAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_owner_history;");
        }
        Assert.Equal(HttpStatusCode.OK, (await fixture.StatusAsync(2, 0, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fixture.StatusAsync(2, 0, 2)).StatusCode);
        await using var check = await fixture.Factory.CreateDbContextAsync();
        Assert.Equal(OpenFrontStatus.NeedsDecision, (await check.OpenFronts.SingleAsync(x => x.Id == 2)).Status);
        Assert.Single(await check.OpenFrontChanges.ToListAsync());
    }

    private static (string, string, string, string, string, long) Values(OpenFrontChange row) =>
        (row.ActorKind, row.ActorId, row.Field, row.OldValue, row.NewValue, row.Revision);

    private sealed class BeforeSave : SaveChangesInterceptor
    {
        public Func<Task>? BeforeNextSave { get; set; }
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var action = BeforeNextSave;
            BeforeNextSave = null;
            if (action is not null) await action();
            return result;
        }
    }

    private sealed class Fixture(WebApplication app, SqliteConnection connection, string token, BeforeSave race) : IAsyncDisposable
    {
        public BeforeSave Race { get; } = race;
        public string Token { get; } = token;
        public IDbContextFactory<ProjectManagerDbContext> Factory => app.Services.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
        public HttpClient Client { get; } = app.GetTestClient();

        public static Dictionary<string, string?> Configuration(string token, int[] grants, bool enabled) => new Dictionary<string, string?>
        {
            ["Authentication:Jarvis:Enabled"] = enabled.ToString(), ["Authentication:Jarvis:ConnectorId"] = "jarvis-test",
            ["Authentication:Jarvis:TokenSha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))),
            ["Authentication:Jarvis:PersonId"] = "1"
        }.Concat(grants.Select((id, index) => new KeyValuePair<string, string?>("Authentication:Jarvis:AllowedFrontIds:" + index, id.ToString())))
            .ToDictionary(x => x.Key, x => x.Value);

        public static ClaimsPrincipal Person() => new(new ClaimsIdentity([
            new Claim("PersonId", "1"), new Claim("auth_source", "google"), new Claim(ClaimTypes.Role, "Administrador")], "Cookies"));

        public string BrowserCookie()
        {
            var options = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Cookies");
            return "RaceIT.Test=" + options.TicketDataFormat.Protect(new AuthenticationTicket(Person(), "Cookies"));
        }

        public Task<HttpResponseMessage> StatusAsync(int id, long revision, int status) => Client.PutAsJsonAsync(
            JarvisOpenFrontEndpoints.Prefix + $"/{id}/status", new { expectedRevision = revision, status });

        public async Task<int> RawHeaderStatusAsync(string[] headers)
        {
            var context = await app.GetTestServer().SendAsync(context =>
            {
                context.Request.Scheme = "https";
                context.Request.Method = "GET";
                context.Request.Path = JarvisOpenFrontEndpoints.Prefix;
                context.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues(headers);
            });
            return context.Response.StatusCode;
        }

        public static async Task<Fixture> CreateAsync(int[]? grants = null, bool enabled = true, bool? allowAllOwnerFronts = null)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(Configuration(token, grants ?? [1], enabled));
            if (allowAllOwnerFronts.HasValue) builder.Configuration["Authentication:Jarvis:AllowAllOwnerFronts"] = allowAllOwnerFronts.Value.ToString();
            builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", options => options.Cookie.Name = "RaceIT.Test");
            builder.Services.AddJarvisConnector(builder.Configuration);
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var race = new BeforeSave();
            builder.Services.AddDbContextFactory<ProjectManagerDbContext>(options => options.UseSqlite(connection).AddInterceptors(race));
            builder.Services.AddScoped<OpenFrontService>();
            var app = builder.Build();
            app.UseJarvisApiErrors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapJarvisOpenFronts();
            app.MapGet("/browser-only", () => "interactive").RequireAuthorization();
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
                await using var db = await factory.CreateDbContextAsync();
                await db.Database.EnsureCreatedAsync();
                db.People.Add(new Person { Id = 2, Name = "Synthetic other", IsActive = true });
                db.OpenFronts.AddRange(
                    new OpenFront { Id = 1, PersonId = 1, Title = "Frente propio", Context = "Contexto original", LastAdvance = "Avance original", NextStep = "Paso original" },
                    new OpenFront { Id = 2, PersonId = 1, Title = "Frente sin permiso" },
                    new OpenFront { Id = 3, PersonId = 2, Title = "Frente ajeno" });
                db.Activities.Add(new Activity { Id = 1, PersonId = 1, Title = "Actividad sintética", CurrentEstimateHours = 8 });
                db.WeeklyPlans.Add(new WeeklyPlan { Id = 1, PersonId = 1, WeekStart = new DateOnly(2026, 11, 23) });
                db.PlanBlocks.Add(new PlanBlock { Id = 1, WeeklyPlanId = 1, ActivityId = 1, PlannedHours = 3, Position = 0 });
                db.WorkRecords.Add(new WorkRecord { Id = 1, PersonId = 1, ActivityId = 1, Kind = WorkRecordKind.Manual, Hours = 1 });
                await db.SaveChangesAsync();
            }
            await app.StartAsync();
            var fixture = new Fixture(app, connection, token, race);
            fixture.Client.BaseAddress = new Uri("https://localhost");
            fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

        public async ValueTask DisposeAsync() { Client.Dispose(); await app.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
