using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Api;

public static class JarvisPlanningEndpoints
{
    public const string Prefix = "/api/jarvis/v1";

    public static IServiceCollection AddJarvisPlanning(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(provider => JarvisPlanningAccess.Read(configuration, provider.GetRequiredService<JarvisConnectorAccess>()));
        services.AddScoped<JarvisPlanningService>();
        return services;
    }

    public static IEndpointRouteBuilder MapJarvisPlanning(this IEndpointRouteBuilder endpoints)
    {
        foreach (var (collection, kind) in new[] { ("initiatives", "initiative"), ("projects", "project"), ("activities", "activity") })
        {
            var group = endpoints.MapGroup($"{Prefix}/{collection}").RequireAuthorization(JarvisConnectorAccess.Scheme);
            group.AddEndpointFilter(async (context, next) =>
            {
                try { return await next(context); }
                catch (PlanningUnavailableException) { return Results.Json(new { error = "item_unavailable" }, statusCode: 404); }
                catch (UnauthorizedAccessException) { return Results.Json(new { error = "forbidden" }, statusCode: 403); }
                catch (PlanningConflictException) { return Results.Json(new { error = "revision_conflict" }, statusCode: 409); }
                catch (ArgumentException) { return Results.Json(new { error = "invalid_input" }, statusCode: 400); }
                catch (DbUpdateConcurrencyException) { return Results.Json(new { error = "revision_conflict" }, statusCode: 409); }
                catch (DbUpdateException) { return Results.Json(new { error = "persistence_failed" }, statusCode: 503); }
                catch (System.Data.Common.DbException) { return Results.Json(new { error = "persistence_failed" }, statusCode: 503); }
            });
            group.MapGet("", async (ClaimsPrincipal user, JarvisPlanningService service) => Results.Ok(await service.ListAsync(user, kind)));
            group.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, JarvisPlanningService service) => Results.Ok(await service.GetAsync(user, kind, id)));
            group.MapGet("/{id:int}/history", async (int id, ClaimsPrincipal user, JarvisPlanningService service) => Results.Ok(await service.HistoryAsync(user, kind, id)));
            group.MapPost("/{id:int}/advances", async (int id, PlanningAdvanceRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
                Results.Ok(await service.MutateAsync(user, kind, id, "advance", request.Command() with { Text = request.Text })));
            if (kind != "activity")
            {
                group.MapPut("/{id:int}/target-date", async (int id, PlanningDateRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
                    Results.Ok(await service.MutateAsync(user, kind, id, "target-date", request.Command() with { TargetDate = request.TargetDate, ApprovalId = request.ApprovalId })));
                group.MapPost("/{id:int}/close", async (int id, PlanningCloseRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
                    Results.Ok(await service.MutateAsync(user, kind, id, "close", request.Command() with { ApprovalId = request.ApprovalId })));
            }
        }
        endpoints.MapPost(Prefix + "/initiatives", async (CreateInitiativeRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
            await service.ResultAsync(() => service.CreateAsync(user, "initiative", new(request.PlanId, request.Name, request.Description, null, request.TargetDate)), 201)).RequireAuthorization(JarvisConnectorAccess.Scheme);
        endpoints.MapPost(Prefix + "/projects", async (CreateProjectRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
            await service.ResultAsync(() => service.CreateAsync(user, "project", new(request.PlanId, request.Name, null, request.InitiativeId, request.TargetDate)), 201)).RequireAuthorization(JarvisConnectorAccess.Scheme);
        endpoints.MapPost(Prefix + "/activities", async (CreateActivityRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
            await service.ResultAsync(() => service.CreateAsync(user, "activity", new(request.PlanId, request.Title, null, request.ProjectId, null, request.InitialEstimateHours)), 201)).RequireAuthorization(JarvisConnectorAccess.Scheme);
        // Creation uses the same typed error contract as other operations without changing /fronts.
        endpoints.MapPut(Prefix + "/initiatives/{id:int}", async (int id, EditInitiativeRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
            await service.ResultAsync(() => service.MutateAsync(user, "initiative", id, "edit", request.Command() with { Name = request.Name, Description = request.Description, DescriptionSpecified = true }))).RequireAuthorization(JarvisConnectorAccess.Scheme);
        endpoints.MapPut(Prefix + "/projects/{id:int}", async (int id, EditProjectRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
            await service.ResultAsync(() => service.MutateAsync(user, "project", id, "edit", request.Command() with { Name = request.Name, ParentSpecified = request.InitiativeId.ValueKind != JsonValueKind.Undefined, ParentId = Parent(request.InitiativeId) }))).RequireAuthorization(JarvisConnectorAccess.Scheme);
        endpoints.MapPut(Prefix + "/activities/{id:int}", async (int id, EditActivityRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
            await service.ResultAsync(() => service.MutateAsync(user, "activity", id, "edit", request.Command() with { Name = request.Title, ParentSpecified = request.ProjectId.ValueKind != JsonValueKind.Undefined, ParentId = Parent(request.ProjectId) }))).RequireAuthorization(JarvisConnectorAccess.Scheme);
        endpoints.MapPut(Prefix + "/activities/{id:int}/status", async (int id, PlanningStatusRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
            await service.ResultAsync(() => service.MutateAsync(user, "activity", id, "status", request.Command() with { Status = request.Status }))).RequireAuthorization(JarvisConnectorAccess.Scheme);
        endpoints.MapPut(Prefix + "/activities/{id:int}/dependencies", async (int id, PlanningDependenciesRequest request, ClaimsPrincipal user, JarvisPlanningService service) =>
            await service.ResultAsync(() => service.MutateAsync(user, "activity", id, "dependencies", request.Command() with { PredecessorIds = request.PredecessorIds, ApprovalId = request.ApprovalId }))).RequireAuthorization(JarvisConnectorAccess.Scheme);
        return endpoints;
    }

    private static int? Parent(JsonElement value) => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? null :
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var id) && id > 0 ? id : throw new ArgumentException("Relación inválida.");
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateInitiativeRequest { public required string PlanId { get; init; } public required string Name { get; init; } public string? Description { get; init; } public DateOnly? TargetDate { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateProjectRequest { public required string PlanId { get; init; } public required string Name { get; init; } public int? InitiativeId { get; init; } public DateOnly? TargetDate { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateActivityRequest { public required string PlanId { get; init; } public required string Title { get; init; } public int? ProjectId { get; init; } public required decimal InitialEstimateHours { get; init; } }
public abstract record PlanningPreconditions
{
    public required string PlanId { get; init; }
    public required long ExpectedRevision { get; init; }
    public required string ExpectedFingerprint { get; init; }
    internal PlanningWrite Command() => new(PlanId, ExpectedRevision, ExpectedFingerprint);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EditInitiativeRequest : PlanningPreconditions { public required string Name { get; init; } public string? Description { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EditProjectRequest : PlanningPreconditions { public required string Name { get; init; } public JsonElement InitiativeId { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EditActivityRequest : PlanningPreconditions { public required string Title { get; init; } public JsonElement ProjectId { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlanningAdvanceRequest : PlanningPreconditions { public required string Text { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlanningDateRequest : PlanningPreconditions { public required DateOnly? TargetDate { get; init; } public required string ApprovalId { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlanningCloseRequest : PlanningPreconditions { public required string ApprovalId { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlanningStatusRequest : PlanningPreconditions { public required ActivityStatus Status { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlanningDependenciesRequest : PlanningPreconditions { public required int[] PredecessorIds { get; init; } public string? ApprovalId { get; init; } }

public sealed record PlanningCreate(string PlanId, string Name, string? Description = null, int? ParentId = null, DateOnly? TargetDate = null, decimal InitialEstimateHours = 0);
public sealed record PlanningWrite(string PlanId, long ExpectedRevision, string ExpectedFingerprint,
    string? Name = null, string? Description = null, bool DescriptionSpecified = false, bool ParentSpecified = false, int? ParentId = null,
    ActivityStatus? Status = null, string? Text = null, DateOnly? TargetDate = null, IReadOnlyList<int>? PredecessorIds = null, string? ApprovalId = null);
