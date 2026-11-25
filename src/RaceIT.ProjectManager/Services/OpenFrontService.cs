using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

/// <summary>Personal front operations. Callers supply a server-authenticated actor, never a client owner ID.</summary>
public sealed class OpenFrontService(IDbContextFactory<ProjectManagerDbContext> factory)
{
    public async Task<IReadOnlyList<OpenFront>> ListAsync(ClaimsPrincipal actor)
    {
        await using var db = await factory.CreateDbContextAsync();
        var personId = await RequireActiveActorAsync(db, actor);
        return await db.OpenFronts.AsNoTracking().Where(x => x.PersonId == personId)
            .OrderBy(x => x.Id).ToListAsync();
    }

    public async Task<OpenFront> ChangeStatusAsync(ClaimsPrincipal actor, int frontId, OpenFrontStatus status, long expectedRevision)
    {
        await using var db = await factory.CreateDbContextAsync();
        var personId = await RequireActiveActorAsync(db, actor);
        if (!Enum.IsDefined(status)) throw new ArgumentException("Selecciona un estado válido.", nameof(status));
        var front = await db.OpenFronts.SingleOrDefaultAsync(x => x.Id == frontId && x.PersonId == personId && x.Person.IsActive)
            ?? throw new UnauthorizedAccessException("El frente no está disponible.");
        if (front.Revision != expectedRevision) throw new OpenFrontConflictException();

        front.Status = status;
        front.UpdatedAtUtc = DateTimeOffset.UtcNow;
        front.Revision = checked(front.Revision + 1);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new OpenFrontConflictException(); }
        return front;
    }

    private static async Task<int> RequireActiveActorAsync(ProjectManagerDbContext db, ClaimsPrincipal actor)
    {
        var personId = WorkAccess.PersonIdOf(actor);
        if (actor.Identity?.IsAuthenticated != true || personId is null ||
            actor.FindFirst("auth_source")?.Value is not ("google" or "selector") ||
            !await db.People.AsNoTracking().AnyAsync(x => x.Id == personId && x.IsActive))
            throw new UnauthorizedAccessException("Inicia sesión para continuar.");
        return personId.Value;
    }
}

public sealed class OpenFrontConflictException() : Exception("El frente cambió desde que lo abriste. Recarga la ficha antes de guardar.");

public static class OpenFrontPresentation
{
    public static string Label(OpenFrontStatus status) => status switch
    {
        OpenFrontStatus.InProgress => "En curso",
        OpenFrontStatus.NeedsDecision => "Necesita decisión",
        OpenFrontStatus.Waiting => "En espera",
        _ => "Desconocido"
    };

    public static string StatusClass(OpenFrontStatus status) => status switch
    {
        OpenFrontStatus.InProgress => "fronts-in-progress",
        OpenFrontStatus.NeedsDecision => "fronts-needs-decision",
        OpenFrontStatus.Waiting => "fronts-waiting",
        _ => ""
    };
}
