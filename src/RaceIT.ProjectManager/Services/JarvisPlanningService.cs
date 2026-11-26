using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Api;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed record PlanningItemDto(string Kind, int Id, string Name, string? Description, int? ParentId,
    DateOnly? TargetDate, decimal? CurrentEstimateHours, ActivityStatus? Status, bool IsClosed, string LastAdvance,
    long Revision, string Fingerprint, IReadOnlyList<int> PredecessorIds);
public sealed record PlanningHistoryDto(long Id, string Kind, int EntityId, string Operation, string ConnectorId,
    DateTimeOffset AtUtc, long Revision, string OldValue, string NewValue, string PlanId, string PlanApprovalReference,
    string? ApprovalId, string? ApprovalReference);

public sealed class JarvisPlanningService(IDbContextFactory<ProjectManagerDbContext> factory,
    JarvisConnectorAccess connector, JarvisPlanningAccess access)
{
    public async Task<IReadOnlyList<PlanningItemDto>> ListAsync(ClaimsPrincipal actor, string kind)
    {
        Require(actor);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ActiveAsync(db);
        var graph = await Graph.LoadAsync(db, access, connector.PersonId);
        var result = graph.Ids(kind).Where(id => graph.Visible(kind, id)).Select(id => graph.Dto(kind, id)).ToArray();
        await tx.CommitAsync();
        return result;
    }

    public async Task<PlanningItemDto> GetAsync(ClaimsPrincipal actor, string kind, int id)
    {
        Require(actor);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ActiveAsync(db);
        var graph = await Graph.LoadAsync(db, access, connector.PersonId);
        graph.RequireVisible(kind, id);
        var result = graph.Dto(kind, id);
        await tx.CommitAsync();
        return result;
    }

    public async Task<IReadOnlyList<PlanningHistoryDto>> HistoryAsync(ClaimsPrincipal actor, string kind, int id)
    {
        Require(actor);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ActiveAsync(db);
        var graph = await Graph.LoadAsync(db, access, connector.PersonId);
        graph.RequireVisible(kind, id);
        // History can contain former relationships. Suppress records whose snapshots mention now-inaccessible data.
        var rows = await db.JarvisPlanningChanges.AsNoTracking().Where(x => x.EntityKind == kind && x.EntityId == id && x.PersonId == connector.PersonId)
            .OrderBy(x => x.Revision).ToListAsync();
        var result = rows.Where(row => graph.SnapshotVisible(row.OldValue) && graph.SnapshotVisible(row.NewValue))
            .Select(row => new PlanningHistoryDto(row.Id, kind, id, row.Operation, row.ConnectorId, row.AtUtc,
                row.Revision, row.OldValue, row.NewValue, row.PlanId, row.PlanApprovalReference, row.ApprovalId, row.ApprovalReference)).ToArray();
        await tx.CommitAsync();
        return result;
    }

    public async Task<PlanningItemDto> CreateAsync(ClaimsPrincipal actor, string kind, PlanningCreate input)
    {
        Require(actor);
        ValidateName(input.Name);
        if (input.Description?.Length > 2000 || input.InitialEstimateHours < 0 || input.InitialEstimateHours > 99999999.99m ||
            decimal.Round(input.InitialEstimateHours, 2) != input.InitialEstimateHours) throw new ArgumentException("Entrada inválida.");
        var plan = Plan(input.PlanId);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ActiveAsync(db);
        var graph = await Graph.LoadAsync(db, access, connector.PersonId);
        var allowed = kind switch { "initiative" => plan.AllowCreateInitiatives, "project" => plan.AllowCreateProjects, "activity" => plan.AllowCreateActivities, _ => false };
        if (!allowed) throw new UnauthorizedAccessException();
        if (kind != "initiative") RequireParent(graph, plan, kind, input.ParentId);
        int id;
        switch (kind)
        {
            case "initiative":
                var initiative = new Initiative { Name = input.Name.Trim(), Description = input.Description, TargetDate = input.TargetDate };
                db.Initiatives.Add(initiative);
                await db.SaveChangesAsync();
                graph.Initiatives.Add(initiative.Id, initiative);
                id = initiative.Id;
                break;
            case "project":
                var project = new Project { Name = input.Name.Trim(), InitiativeId = input.ParentId, PersonId = connector.PersonId, TargetDate = input.TargetDate };
                db.Projects.Add(project);
                await db.SaveChangesAsync();
                graph.Projects.Add(project.Id, project);
                id = project.Id;
                break;
            case "activity":
                var activity = new Activity { Title = input.Name.Trim(), ProjectId = input.ParentId, PersonId = connector.PersonId, CurrentEstimateHours = input.InitialEstimateHours };
                db.Activities.Add(activity);
                await db.SaveChangesAsync();
                graph.Activities.Add(activity.Id, activity);
                id = activity.Id;
                break;
            default: throw new ArgumentException("Tipo inválido.");
        }
        var state = new JarvisPlanningState { EntityKind = kind, EntityId = id, PersonId = connector.PersonId, CreatedInPlanId = plan.Id, Revision = 1 };
        graph.States.Add((kind, id), state);
        db.JarvisPlanningStates.Add(state);
        AddHistory(db, graph, kind, id, "created", "", plan, null);
        await db.SaveChangesAsync();
        var dto = graph.Dto(kind, id);
        await tx.CommitAsync();
        return dto;
    }

    public async Task<PlanningItemDto> MutateAsync(ClaimsPrincipal actor, string kind, int id, string operation, PlanningWrite input)
    {
        Require(actor);
        if (input.ExpectedRevision < 0 || input.ExpectedRevision == long.MaxValue || !JarvisPlanningAccess.Hash(input.ExpectedFingerprint)) throw new ArgumentException("Precondición inválida.");
        var plan = Plan(input.PlanId);
        await using var db = await factory.CreateDbContextAsync();
        // Serializable protects ownership, hierarchy, sessions and the complete dependency graph until commit.
        // SQL Server may choose a deadlock victim; that request fails rather than reporting partial success.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ActiveAsync(db);
        var graph = await Graph.LoadAsync(db, access, connector.PersonId);
        graph.RequireVisible(kind, id);
        RequireScope(graph, plan, kind, id);
        RequireContext(graph, plan, kind, id);
        var before = graph.Dto(kind, id);
        if (before.Revision != input.ExpectedRevision || !string.Equals(before.Fingerprint, input.ExpectedFingerprint, StringComparison.OrdinalIgnoreCase)) throw new PlanningConflictException();
        var oldValue = graph.Snapshot(kind, id);
        PlanningApproval? approval = null;
        switch (operation)
        {
            case "edit":
                ValidateName(input.Name);
                if (input.Description?.Length > 2000) throw new ArgumentException("Descripción inválida.");
                if (input.ParentSpecified) RequireParent(graph, plan, kind, input.ParentId);
                if (kind == "initiative") { graph.Initiatives[id].Name = input.Name!.Trim(); if (input.DescriptionSpecified) graph.Initiatives[id].Description = input.Description; }
                else if (kind == "project") { graph.Projects[id].Name = input.Name!.Trim(); if (input.ParentSpecified) graph.Projects[id].InitiativeId = input.ParentId; }
                else { graph.Activities[id].Title = input.Name!.Trim(); if (input.ParentSpecified) graph.Activities[id].ProjectId = input.ParentId; }
                break;
            case "advance":
                if (string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > 2000) throw new ArgumentException("Avance inválido.");
                break;
            case "status":
                if (kind != "activity" || input.Status is null || !Enum.IsDefined(input.Status.Value)) throw new ArgumentException("Estado inválido.");
                if (input.Status == ActivityStatus.Completed)
                {
                    var now = DateTimeOffset.UtcNow;
                    foreach (var session in graph.Records.Where(x => x.ActivityId == id && x.Kind == WorkRecordKind.Session && x.End is null))
                    {
                        if (session.PersonId != connector.PersonId || session.Start is null || session.Start > now) throw new UnauthorizedAccessException();
                        session.End = now;
                        session.Hours = decimal.Round(PlanningRules.SessionHours(session.Start.Value, now), 2);
                    }
                }
                graph.Activities[id].Status = input.Status.Value;
                break;
            case "target-date":
                if (kind == "activity") throw new ArgumentException("Operación inválida.");
                if (before.TargetDate != input.TargetDate)
                    approval = await ApprovalAsync(db, plan, kind, id, operation, before, input.ApprovalId, new { targetDate = input.TargetDate });
                if (kind == "initiative") graph.Initiatives[id].TargetDate = input.TargetDate;
                else graph.Projects[id].TargetDate = input.TargetDate;
                break;
            case "close":
                if (kind == "activity") throw new ArgumentException("Operación inválida.");
                if (!before.IsClosed) approval = await ApprovalAsync(db, plan, kind, id, operation, before, input.ApprovalId, new { isClosed = true });
                break;
            case "dependencies":
                if (kind != "activity" || input.PredecessorIds is null || input.PredecessorIds.Any(x => x <= 0) || input.PredecessorIds.Count > 1000) throw new ArgumentException("Dependencias inválidas.");
                var predecessors = input.PredecessorIds.Distinct().Order().ToArray();
                if (predecessors.Contains(id)) throw new ArgumentException("Dependencia circular.");
                foreach (var predecessor in predecessors) { graph.RequireVisible("activity", predecessor); RequireScope(graph, plan, "activity", predecessor); RequireContext(graph, plan, "activity", predecessor); }
                var candidates = graph.Dependencies.Where(x => x.SuccessorId != id).Select(x => (x.PredecessorId, x.SuccessorId)).ToList();
                candidates.AddRange(predecessors.Select(p => (p, id)));
                if (PlanningRules.HasCycle(graph.Activities.Keys.ToArray(), candidates)) throw new ArgumentException("Dependencia circular.");
                if (!before.PredecessorIds.SequenceEqual(predecessors))
                {
                    approval = await ApprovalAsync(db, plan, kind, id, operation, before, input.ApprovalId, new { predecessorIds = predecessors });
                    var removed = graph.Dependencies.Where(x => x.SuccessorId == id && !predecessors.Contains(x.PredecessorId)).ToArray();
                    db.ActivityDependencies.RemoveRange(removed);
                    foreach (var edge in removed) graph.Dependencies.Remove(edge);
                    foreach (var predecessor in predecessors.Where(p => !graph.Dependencies.Any(x => x.PredecessorId == p && x.SuccessorId == id)))
                    {
                        var edge = new ActivityDependency { PredecessorId = predecessor, SuccessorId = id };
                        db.ActivityDependencies.Add(edge);
                        graph.Dependencies.Add(edge);
                    }
                }
                break;
            default: throw new ArgumentException("Operación inválida.");
        }
        if (!graph.States.TryGetValue((kind, id), out var state))
        {
            state = new JarvisPlanningState { EntityKind = kind, EntityId = id, PersonId = connector.PersonId };
            db.JarvisPlanningStates.Add(state);
            graph.States.Add((kind, id), state);
        }
        if (operation == "advance") state.LastAdvance = input.Text!;
        if (operation == "close") state.IsClosed = true;
        state.Revision = checked(state.Revision + 1);
        AddHistory(db, graph, kind, id, operation, oldValue, plan, approval);
        await db.SaveChangesAsync();
        var result = graph.Dto(kind, id);
        await tx.CommitAsync();
        return result;
    }

    // All standalone routes share errors with grouped routes. Kept local to this API extension.
    public async Task<IResult> ResultAsync(Func<Task<PlanningItemDto>> action, int status = 200)
    {
        try { return Results.Json(await action(), statusCode: status); }
        catch (PlanningUnavailableException) { return Results.Json(new { error = "item_unavailable" }, statusCode: 404); }
        catch (UnauthorizedAccessException) { return Results.Json(new { error = "forbidden" }, statusCode: 403); }
        catch (PlanningConflictException) { return Results.Json(new { error = "revision_conflict" }, statusCode: 409); }
        catch (ArgumentException) { return Results.Json(new { error = "invalid_input" }, statusCode: 400); }
        catch (DbUpdateConcurrencyException) { return Results.Json(new { error = "revision_conflict" }, statusCode: 409); }
        catch (DbUpdateException) { return Results.Json(new { error = "persistence_failed" }, statusCode: 503); }
        catch (System.Data.Common.DbException) { return Results.Json(new { error = "persistence_failed" }, statusCode: 503); }
    }

    private void Require(ClaimsPrincipal actor) { connector.RequireIdentity(actor); if (!access.Enabled) throw new UnauthorizedAccessException(); }
    private async Task ActiveAsync(ProjectManagerDbContext db) { if (!await db.People.AnyAsync(x => x.Id == connector.PersonId && x.IsActive)) throw new UnauthorizedAccessException(); }
    private PlanningPlan Plan(string id) => id is not null && access.Plans.TryGetValue(id, out var plan) ? plan : throw new UnauthorizedAccessException();
    private static void ValidateName(string? value) { if (string.IsNullOrWhiteSpace(value) || value.Length > 200) throw new ArgumentException("Nombre inválido."); }
    private static void RequireScope(Graph graph, PlanningPlan plan, string kind, int id)
    {
        if (!graph.InPlan(plan, kind, id)) throw new UnauthorizedAccessException();
    }
    private static void RequireParent(Graph graph, PlanningPlan plan, string kind, int? parent)
    {
        if (parent is null)
        {
            if (kind == "project" && !plan.AllowIndependentProjects || kind == "activity" && !plan.AllowLooseActivities) throw new UnauthorizedAccessException();
            return;
        }
        var parentKind = kind == "project" ? "initiative" : "project";
        graph.RequireVisible(parentKind, parent.Value);
        RequireScope(graph, plan, parentKind, parent.Value);
        RequireContext(graph, plan, parentKind, parent.Value);
    }
    private static void RequireContext(Graph graph, PlanningPlan plan, string kind, int id)
    {
        // A change to an aggregate/context must not affect work outside this approved plan.
        foreach (var (childKind, childId) in graph.Descendants(kind, id))
        {
            graph.RequireVisible(childKind, childId);
            RequireScope(graph, plan, childKind, childId);
        }
        var parent = graph.Parent(kind, id);
        if (parent is int parentId)
        {
            var parentKind = kind == "activity" ? "project" : "initiative";
            graph.RequireVisible(parentKind, parentId);
            RequireScope(graph, plan, parentKind, parentId);
            var grandparent = graph.Parent(parentKind, parentId);
            if (grandparent is int initiativeId) { graph.RequireVisible("initiative", initiativeId); RequireScope(graph, plan, "initiative", initiativeId); }
        }
        if (kind == "activity" && graph.Dependencies.Any(edge => (edge.PredecessorId == id || edge.SuccessorId == id) &&
            (!graph.Visible("activity", edge.PredecessorId) || !graph.Visible("activity", edge.SuccessorId) || !graph.InPlan(plan, "activity", edge.PredecessorId) || !graph.InPlan(plan, "activity", edge.SuccessorId))))
            throw new UnauthorizedAccessException();
        if (kind == "activity" && graph.Records.Any(record => record.ActivityId == id && record.PersonId != graph.Owner))
            throw new UnauthorizedAccessException();
    }

    private async Task<PlanningApproval> ApprovalAsync(ProjectManagerDbContext db, PlanningPlan plan, string kind, int id, string operation, PlanningItemDto before, string? approvalId, object values)
    {
        if (approvalId is null || !access.Approvals.TryGetValue(approvalId, out var approval) || approval.PlanId != plan.Id ||
            approval.PersonId != connector.PersonId || approval.EntityKind != kind || approval.EntityId != id || approval.Operation != operation ||
            approval.ExpectedRevision != before.Revision || !string.Equals(approval.ExpectedFingerprint, before.Fingerprint, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(approval.ValuesSha256, JarvisPlanningAccess.ValuesHash(values), StringComparison.OrdinalIgnoreCase) ||
            await db.JarvisPlanningChanges.AnyAsync(x => x.ApprovalId == approvalId)) throw new UnauthorizedAccessException();
        return approval;
    }
    private void AddHistory(ProjectManagerDbContext db, Graph graph, string kind, int id, string operation, string oldValue, PlanningPlan plan, PlanningApproval? approval)
    {
        db.JarvisPlanningChanges.Add(new JarvisPlanningChange
        {
            EntityKind = kind, EntityId = id, PersonId = connector.PersonId, Revision = graph.States[(kind, id)].Revision,
            PlanId = plan.Id, PlanApprovalReference = plan.ApprovalReference, Operation = operation, ConnectorId = connector.ConnectorId,
            AtUtc = DateTimeOffset.UtcNow, OldValue = oldValue, NewValue = graph.Snapshot(kind, id), ApprovalId = approval?.Id, ApprovalReference = approval?.ApprovalReference
        });
    }

    private sealed class Graph(JarvisPlanningAccess access, int owner)
    {
        public int Owner => owner;
        public Dictionary<int, Initiative> Initiatives { get; private init; } = [];
        public Dictionary<int, Project> Projects { get; private init; } = [];
        public Dictionary<int, Activity> Activities { get; private init; } = [];
        public Dictionary<(string, int), JarvisPlanningState> States { get; private init; } = [];
        public List<ActivityDependency> Dependencies { get; private init; } = [];
        public List<WorkRecord> Records { get; private init; } = [];
        public static async Task<Graph> LoadAsync(ProjectManagerDbContext db, JarvisPlanningAccess access, int owner) => new(access, owner)
        {
            Initiatives = await db.Initiatives.ToDictionaryAsync(x => x.Id), Projects = await db.Projects.ToDictionaryAsync(x => x.Id),
            Activities = await db.Activities.ToDictionaryAsync(x => x.Id), States = (await db.JarvisPlanningStates.ToListAsync()).ToDictionary(x => (x.EntityKind, x.EntityId)),
            Dependencies = await db.ActivityDependencies.ToListAsync(), Records = await db.WorkRecords.ToListAsync()
        };
        public IEnumerable<int> Ids(string kind) => (kind switch { "initiative" => Initiatives.Keys.AsEnumerable(), "project" => Projects.Keys, "activity" => Activities.Keys, _ => [] }).Order();
        public bool Visible(string kind, int id)
        {
            if (States.TryGetValue((kind, id), out var state) && state.PersonId != owner) return false;
            return kind switch
            {
                "initiative" => Initiatives.ContainsKey(id) && (access.OwnedInitiativeIds.Contains(id) || state?.CreatedInPlanId is not null && state.PersonId == owner) &&
                    Projects.Values.Where(x => x.InitiativeId == id).All(x => Visible("project", x.Id)),
                "project" => Projects.TryGetValue(id, out var project) && project.PersonId == owner && Activities.Values.Where(x => x.ProjectId == id).All(x => x.PersonId == owner),
                "activity" => Activities.TryGetValue(id, out var activity) && activity.PersonId == owner,
                _ => false
            };
        }
        public void RequireVisible(string kind, int id) { if (!Visible(kind, id)) throw new PlanningUnavailableException(); }
        public int? Parent(string kind, int id) => kind switch { "project" => Projects[id].InitiativeId, "activity" => Activities[id].ProjectId, _ => null };
        public bool InPlan(PlanningPlan plan, string kind, int id)
        {
            if (States.TryGetValue((kind, id), out var state) && state.CreatedInPlanId is not null) return state.CreatedInPlanId == plan.Id;
            return kind switch
            {
                "initiative" => plan.InitiativeIds.Contains(id),
                "project" => plan.ProjectIds.Contains(id) || Projects[id].InitiativeId is int parent && InPlan(plan, "initiative", parent),
                "activity" => plan.ActivityIds.Contains(id) || Activities[id].ProjectId is int parent && InPlan(plan, "project", parent),
                _ => false
            };
        }
        public IEnumerable<(string, int)> Descendants(string kind, int id)
        {
            if (kind == "initiative")
                foreach (var project in Projects.Values.Where(x => x.InitiativeId == id))
                {
                    yield return ("project", project.Id);
                    foreach (var activity in Activities.Values.Where(x => x.ProjectId == project.Id)) yield return ("activity", activity.Id);
                }
            if (kind == "project") foreach (var activity in Activities.Values.Where(x => x.ProjectId == id)) yield return ("activity", activity.Id);
        }
        public PlanningItemDto Dto(string kind, int id)
        {
            var state = States.GetValueOrDefault((kind, id));
            var parent = Parent(kind, id);
            if (parent is int parentId && !Visible(kind == "activity" ? "project" : "initiative", parentId)) parent = null;
            return new(kind, id, kind switch { "initiative" => Initiatives[id].Name, "project" => Projects[id].Name, _ => Activities[id].Title },
                kind == "initiative" ? Initiatives[id].Description : null, parent,
                kind switch { "initiative" => Initiatives[id].TargetDate, "project" => Projects[id].TargetDate, _ => null },
                kind == "activity" ? Activities[id].CurrentEstimateHours : null, kind == "activity" ? Activities[id].Status : null,
                state?.IsClosed ?? false, state?.LastAdvance ?? "", state?.Revision ?? 0, JarvisPlanningAccess.ValuesHash(SnapshotObject(kind, id)),
                kind == "activity" ? Dependencies.Where(x => x.SuccessorId == id && Visible("activity", x.PredecessorId)).Select(x => x.PredecessorId).Order().ToArray() : []);
        }
        public string Snapshot(string kind, int id) => JarvisPlanningAccess.SerializeValues(SnapshotObject(kind, id));
        private object SnapshotObject(string kind, int id)
        {
            var state = States.GetValueOrDefault((kind, id));
            return new
            {
                kind, id, owner,
                name = kind switch { "initiative" => Initiatives[id].Name, "project" => Projects[id].Name, _ => Activities[id].Title },
                description = kind == "initiative" ? Initiatives[id].Description : null,
                parentId = Parent(kind, id),
                targetDate = kind switch { "initiative" => Initiatives[id].TargetDate, "project" => Projects[id].TargetDate, _ => null },
                estimate = kind == "activity" ? (decimal?)Activities[id].CurrentEstimateHours : null,
                status = kind == "activity" ? (ActivityStatus?)Activities[id].Status : null,
                revision = state?.Revision ?? 0, isClosed = state?.IsClosed ?? false, lastAdvance = state?.LastAdvance ?? "",
                createdInPlanId = state?.CreatedInPlanId,
                contextFingerprint = ContextFingerprint(kind, id),
                children = Descendants(kind, id).OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => new { kind = x.Item1, id = x.Item2 }).ToArray(),
                predecessors = kind == "activity" ? Dependencies.Where(x => x.SuccessorId == id).Select(x => x.PredecessorId).Order().ToArray() : [],
                successors = kind == "activity" ? Dependencies.Where(x => x.PredecessorId == id).Select(x => x.SuccessorId).Order().ToArray() : [],
                sessions = kind == "activity" ? Records.Where(x => x.ActivityId == id && x.Kind == WorkRecordKind.Session && x.End is null).OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.PersonId, x.Start, x.End, x.Hours }).ToArray() : []
            };
        }
        private string ContextFingerprint(string kind, int id)
        {
            var related = Descendants(kind, id).ToList();
            var parent = Parent(kind, id);
            if (parent is int parentId)
            {
                var parentKind = kind == "activity" ? "project" : "initiative";
                related.Add((parentKind, parentId));
                if (Parent(parentKind, parentId) is int initiativeId) related.Add(("initiative", initiativeId));
            }
            return JarvisPlanningAccess.ValuesHash(related.Distinct().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(item => new
            {
                kind = item.Item1, id = item.Item2,
                name = item.Item1 switch { "initiative" => Initiatives[item.Item2].Name, "project" => Projects[item.Item2].Name, _ => Activities[item.Item2].Title },
                parentId = Parent(item.Item1, item.Item2),
                targetDate = item.Item1 switch { "initiative" => Initiatives[item.Item2].TargetDate, "project" => Projects[item.Item2].TargetDate, _ => null },
                status = item.Item1 == "activity" ? (ActivityStatus?)Activities[item.Item2].Status : null,
                estimate = item.Item1 == "activity" ? (decimal?)Activities[item.Item2].CurrentEstimateHours : null,
                revision = States.GetValueOrDefault(item)?.Revision ?? 0,
                predecessors = item.Item1 == "activity" ? Dependencies.Where(x => x.SuccessorId == item.Item2).Select(x => x.PredecessorId).Order().ToArray() : []
            }).ToArray());
        }
        public bool SnapshotVisible(string json)
        {
            if (json.Length == 0) return true;
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var kind = root.GetProperty("kind").GetString()!;
            var parent = root.GetProperty("parentId");
            if (parent.ValueKind == JsonValueKind.Number && !Visible(kind == "activity" ? "project" : "initiative", parent.GetInt32())) return false;
            foreach (var child in root.GetProperty("children").EnumerateArray()) if (!Visible(child.GetProperty("kind").GetString()!, child.GetProperty("id").GetInt32())) return false;
            foreach (var name in new[] { "predecessors", "successors" }) foreach (var dependency in root.GetProperty(name).EnumerateArray()) if (!Visible("activity", dependency.GetInt32())) return false;
            foreach (var session in root.GetProperty("sessions").EnumerateArray()) if (session.GetProperty("PersonId").GetInt32() != owner) return false;
            return true;
        }
    }
}

public sealed class PlanningUnavailableException() : Exception("Elemento no disponible.");
public sealed class PlanningConflictException() : Exception("El elemento cambió. Consulta su estado vigente.");
