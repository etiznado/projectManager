using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

/// <summary>Personal front operations. Callers supply a server-authenticated actor, never a client owner ID.</summary>
public sealed class OpenFrontService(IDbContextFactory<ProjectManagerDbContext> factory, JarvisConnectorAccess? connector = null)
{
    public async Task<IReadOnlyList<OpenFront>> ListAsync(ClaimsPrincipal actor)
    {
        await using var db = await factory.CreateDbContextAsync();
        var personId = await RequireActiveActorAsync(db, actor);
        return await db.OpenFronts.AsNoTracking().Where(x => x.PersonId == personId)
            .OrderBy(x => x.Id).ToListAsync();
    }

    public Task<OpenFront> ChangeStatusAsync(ClaimsPrincipal actor, int frontId, OpenFrontStatus status, long expectedRevision) =>
        MutateAsync(actor, frontId, expectedRevision, "status", status, null, false);

    public async Task<IReadOnlyList<OpenFront>> ListForConnectorAsync(ClaimsPrincipal actor)
    {
        await using var db = await factory.CreateDbContextAsync();
        var personId = await RequireConnectorAsync(db, actor);
        return await ConnectorFronts(db, personId).AsNoTracking()
            .OrderBy(x => x.Id).ToListAsync();
    }

    public async Task<OpenFront> CreateForConnectorAsync(ClaimsPrincipal actor, string title, string? context = null,
        OpenFrontStatus status = OpenFrontStatus.InProgress, string lastAdvance = "", string nextStep = "")
    {
        await using var db = await factory.CreateDbContextAsync();
        var personId = await RequireConnectorAsync(db, actor);
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200 || context?.Length > 200 || !Enum.IsDefined(status) ||
            lastAdvance is null || lastAdvance.Length > 2000 || nextStep is null || nextStep.Length > 2000)
            throw new ArgumentException("Los campos de la tarjeta no son válidos.");
        var front = new OpenFront
        {
            PersonId = personId, Title = title, Context = context, Status = status, LastAdvance = lastAdvance, NextStep = nextStep,
            UpdatedAtUtc = DateTimeOffset.UtcNow, Revision = 1
        };
        db.OpenFronts.Add(front);
        db.OpenFrontChanges.Add(new OpenFrontChange
        {
            OpenFront = front, PersonId = personId, ActorKind = "connector", ActorId = connector!.ConnectorId,
            Field = "created", OldValue = "", NewValue = title, AtUtc = front.UpdatedAtUtc, Revision = 1
        });
        await db.SaveChangesAsync();
        return front;
    }

    private IQueryable<OpenFront> ConnectorFronts(ProjectManagerDbContext db, int personId)
    {
        if (connector!.AllowAllOwnerFronts) return db.OpenFronts.Where(front => front.PersonId == personId);
        var grants = connector!.FrontIds;
        var actorId = connector.ConnectorId;
        var creations = db.OpenFrontChanges.Where(change => change.PersonId == personId && change.ActorKind == "connector" &&
            change.Field == "created" && change.Revision == 1 && change.OldValue == "");
        // Connector authentication uses ordinal IDs; SQL Server's default collation must not merge distinct IDs.
        creations = db.Database.IsSqlServer()
            ? creations.Where(change => EF.Functions.Collate(change.ActorId, "Latin1_General_100_BIN2") == actorId)
            : creations.Where(change => change.ActorId == actorId);
        return db.OpenFronts.Where(front => front.PersonId == personId &&
            (grants.Contains(front.Id) || creations.Any(change => change.OpenFrontId == front.Id)));
    }

    public Task<OpenFront> ChangeStatusForConnectorAsync(ClaimsPrincipal actor, int frontId, OpenFrontStatus status, long expectedRevision) =>
        MutateAsync(actor, frontId, expectedRevision, "status", status, null, true);

    public Task<OpenFront> RecordAdvanceForConnectorAsync(ClaimsPrincipal actor, int frontId, string text, long expectedRevision) =>
        MutateAsync(actor, frontId, expectedRevision, "lastAdvance", null, text, true);

    public Task<OpenFront> UpdateNextStepForConnectorAsync(ClaimsPrincipal actor, int frontId, string text, long expectedRevision) =>
        MutateAsync(actor, frontId, expectedRevision, "nextStep", null, text, true);

    private async Task<OpenFront> MutateAsync(ClaimsPrincipal actor, int frontId, long expectedRevision, string field,
        OpenFrontStatus? status, string? text, bool isConnector)
    {
        await using var db = await factory.CreateDbContextAsync();
        var personId = isConnector ? await RequireConnectorAsync(db, actor) : await RequireActiveActorAsync(db, actor);
        if (expectedRevision < 0 || expectedRevision == long.MaxValue) throw new ArgumentException("Revisión no válida.");
        if (status.HasValue && !Enum.IsDefined(status.Value)) throw new ArgumentException("Selecciona un estado válido.");
        if (field != "status" && (text is null || text.Length > 2000 || field == "lastAdvance" && string.IsNullOrWhiteSpace(text)))
            throw new ArgumentException("El texto no es válido.");
        var fronts = isConnector ? ConnectorFronts(db, personId) : db.OpenFronts.Where(x => x.PersonId == personId);
        var front = await fronts.SingleOrDefaultAsync(x => x.Id == frontId && x.Person.IsActive);
        if (front is null)
        {
            if (isConnector) throw new OpenFrontUnavailableException();
            throw new UnauthorizedAccessException("El frente no está disponible.");
        }
        if (front.Revision != expectedRevision) throw new OpenFrontConflictException();
        var oldValue = field switch { "status" => ((int)front.Status).ToString(System.Globalization.CultureInfo.InvariantCulture), "lastAdvance" => front.LastAdvance, _ => front.NextStep };
        if (field == "status") front.Status = status!.Value;
        else if (field == "lastAdvance") front.LastAdvance = text!;
        else front.NextStep = text!;
        front.UpdatedAtUtc = DateTimeOffset.UtcNow;
        front.Revision = checked(front.Revision + 1);
        db.OpenFrontChanges.Add(new OpenFrontChange
        {
            OpenFrontId = front.Id, PersonId = personId,
            ActorKind = isConnector ? "connector" : "person",
            ActorId = isConnector ? connector!.ConnectorId : personId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Field = field, OldValue = oldValue,
            NewValue = field == "status" ? ((int)front.Status).ToString(System.Globalization.CultureInfo.InvariantCulture) : text!,
            AtUtc = front.UpdatedAtUtc, Revision = front.Revision
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new OpenFrontConflictException(); }
        catch (DbUpdateException)
        {
            // A concurrent winner may insert the unique history revision before our UPDATE is attempted.
            if (await db.OpenFronts.AsNoTracking().AnyAsync(x => x.Id == front.Id && x.Revision != expectedRevision))
                throw new OpenFrontConflictException();
            throw;
        }
        return front;
    }

    private async Task<int> RequireConnectorAsync(ProjectManagerDbContext db, ClaimsPrincipal actor)
    {
        if (connector is null) throw new UnauthorizedAccessException("Acceso de conector no disponible.");
        connector.RequireIdentity(actor);
        if (!await db.People.AsNoTracking().AnyAsync(x => x.Id == connector.PersonId && x.IsActive))
            throw new UnauthorizedAccessException("Acceso de conector no disponible.");
        return connector.PersonId;
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
public sealed class OpenFrontUnavailableException() : Exception("El frente no está disponible.");

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
