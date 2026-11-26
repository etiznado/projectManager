using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Api;

public static class JarvisOpenFrontEndpoints
{
    public const string Prefix = "/api/jarvis/v1/fronts";

    public static IApplicationBuilder UseJarvisApiErrors(this IApplicationBuilder app) => app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/api/jarvis"), branch => branch.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            try { await next(context); }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { throw; }
            catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = exception.StatusCode;
            }
            catch (Exception) when (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            }
            if (context.Response.StatusCode >= 400 && !context.Response.HasStarted && context.Response.ContentType is null)
            {
                context.Response.Headers.CacheControl = "no-store";
                await context.Response.WriteAsJsonAsync(new { error = context.Response.StatusCode switch
                {
                    400 => "invalid_input", 401 => "unauthenticated", 403 => "forbidden", 404 => "not_found",
                    405 => "method_not_allowed", 415 => "unsupported_media_type", _ => "request_failed"
                }});
            }
        }));

    public static IEndpointRouteBuilder MapJarvisOpenFronts(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Prefix).RequireAuthorization(JarvisConnectorAccess.Scheme);
        group.AddEndpointFilter(async (context, next) =>
        {
            try { return await next(context); }
            catch (OpenFrontUnavailableException) { return Results.Json(new { error = "front_unavailable" }, statusCode: 404); }
            catch (UnauthorizedAccessException) { return Results.Json(new { error = "forbidden" }, statusCode: 403); }
            catch (OpenFrontConflictException) { return Results.Json(new { error = "revision_conflict" }, statusCode: 409); }
            catch (ArgumentException) { return Results.Json(new { error = "invalid_input" }, statusCode: 400); }
            catch (DbUpdateException) { return Results.Json(new { error = "persistence_failed" }, statusCode: 503); }
            catch (System.Data.Common.DbException) { return Results.Json(new { error = "persistence_failed" }, statusCode: 503); }
        });
        group.MapGet("", async (ClaimsPrincipal user, OpenFrontService service) =>
            Results.Ok((await service.ListForConnectorAsync(user)).Select(OpenFrontDto.From)));
        group.MapPost("", async (CreateFrontRequest request, ClaimsPrincipal user, OpenFrontService service) =>
            Results.Json(OpenFrontDto.From(await service.CreateForConnectorAsync(user, request.Title, request.Context,
                request.Status, request.LastAdvance, request.NextStep)), statusCode: StatusCodes.Status201Created));
        group.MapPut("/{frontId:int}/status", async (int frontId, StatusRequest request, ClaimsPrincipal user, OpenFrontService service) =>
            Results.Ok(OpenFrontDto.From(await service.ChangeStatusForConnectorAsync(user, frontId, request.Status, request.ExpectedRevision))));
        group.MapPost("/{frontId:int}/advances", async (int frontId, TextRequest request, ClaimsPrincipal user, OpenFrontService service) =>
            Results.Ok(OpenFrontDto.From(await service.RecordAdvanceForConnectorAsync(user, frontId, request.Text, request.ExpectedRevision))));
        group.MapPut("/{frontId:int}/next-step", async (int frontId, TextRequest request, ClaimsPrincipal user, OpenFrontService service) =>
            Results.Ok(OpenFrontDto.From(await service.UpdateNextStepForConnectorAsync(user, frontId, request.Text, request.ExpectedRevision))));
        return endpoints;
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateFrontRequest
{
    public required string Title { get; init; }
    public string? Context { get; init; }
    public OpenFrontStatus Status { get; init; } = OpenFrontStatus.InProgress;
    public string LastAdvance { get; init; } = "";
    public string NextStep { get; init; } = "";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StatusRequest
{
    public required long ExpectedRevision { get; init; }
    public required OpenFrontStatus Status { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TextRequest
{
    public required long ExpectedRevision { get; init; }
    public required string Text { get; init; }
}

public sealed record OpenFrontDto(int Id, string Title, string? Context, OpenFrontStatus Status,
    string LastAdvance, string NextStep, DateTimeOffset UpdatedAtUtc, long Revision)
{
    public static OpenFrontDto From(OpenFront front) => new(front.Id, front.Title, front.Context, front.Status,
        front.LastAdvance, front.NextStep, front.UpdatedAtUtc, front.Revision);
}
