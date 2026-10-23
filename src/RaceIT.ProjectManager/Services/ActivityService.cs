using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class ActivityService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public sealed record ActivityRow(Activity Activity, decimal RecordedHours)
    {
        public decimal RemainingHours => PlanningRules.RemainingHours(Activity.CurrentEstimateHours, RecordedHours);
        public bool NeedsReestimate => PlanningRules.NeedsReestimate(Activity.Status, RemainingHours);
    }

    public async Task<List<ActivityRow>> ListRowsAsync(int? personId = null)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        if (!await access.IsAdminAsync()) personId = await access.PersonIdAsync();
        IQueryable<Activity> query = db.Activities.AsNoTracking().Include(x => x.Project).Include(x => x.Person);
        if (personId is int pid) query = query.Where(x => x.PersonId == pid);
        var activities = await query.OrderBy(x => x.CreatedAt).ToListAsync();
        var ids = activities.Select(x => x.Id).ToList();
        var map = new Dictionary<int, decimal>();
        if (ids.Count > 0)
        {
            var totals = await db.WorkRecords.Where(x => ids.Contains(x.ActivityId))
                .GroupBy(x => x.ActivityId)
                .Select(g => new { ActivityId = g.Key, Hours = g.Sum(x => x.Hours) })
                .ToListAsync();
            map = totals.ToDictionary(x => x.ActivityId, x => x.Hours);
        }
        return activities.Select(x => new ActivityRow(x, map.GetValueOrDefault(x.Id))).ToList();
    }

    public async Task<List<Activity>> ListAsync(int? personId = null)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        if (!await access.IsAdminAsync()) personId = await access.PersonIdAsync();
        IQueryable<Activity> query = db.Activities.AsNoTracking().Include(x => x.Project).Include(x => x.Person);
        if (personId is int pid) query = query.Where(x => x.PersonId == pid);
        return await query.OrderBy(x => x.CreatedAt).ToListAsync();
    }

    public Task<Activity> CreateAsync(ActivityInput input) => CreateCoreAsync(input, ownWorkOnly: false);

    public Task<Activity> CreateOwnAsync(ActivityInput input) => CreateCoreAsync(input, ownWorkOnly: true);

    public async Task<Activity> UpdateAsync(int activityId, ActivityInput input)
    {
        await access.RequireAdminAsync();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true) || string.IsNullOrWhiteSpace(input.Title))
            throw new ArgumentException(errors.Count > 0 ? string.Join(" ", errors.Select(x => x.ErrorMessage)) : "Captura el título.");
        await using var db = await factory.CreateDbContextAsync();
        var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == activityId)
            ?? throw new ArgumentException("La actividad no existe.");
        if (input.ProjectId is int projectId && !await db.Projects.AnyAsync(x => x.Id == projectId))
            throw new ArgumentException("Selecciona un proyecto válido.");
        if (input.PersonId is int personId && !await db.People.AnyAsync(x => x.Id == personId && x.IsActive))
            throw new ArgumentException("Selecciona un responsable activo.");
        activity.Title = input.Title.Trim();
        activity.ProjectId = input.ProjectId;
        activity.PersonId = input.PersonId;
        activity.CurrentEstimateHours = input.CurrentEstimateHours;
        await db.SaveChangesAsync();
        return activity;
    }

    private async Task<Activity> CreateCoreAsync(ActivityInput input, bool ownWorkOnly)
    {
        await access.RequireAuthenticatedAsync();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true) || string.IsNullOrWhiteSpace(input.Title))
            throw new ArgumentException(string.Join(" ", errors.Select(x => x.ErrorMessage)));
        var isAdmin = await access.IsAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        int? personId = ownWorkOnly
            ? await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.")
            : isAdmin
            ? input.PersonId
            : await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        if (input.ProjectId is int projectId)
        {
            var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == projectId);
            if (project is null || !WorkAccess.CanCreateActivityInProject(isAdmin, ownWorkOnly, personId, project.PersonId))
                throw new ArgumentException("Selecciona un proyecto disponible para ti.");
        }
        if (personId is int pid && !await db.People.AnyAsync(x => x.Id == pid))
            throw new ArgumentException("Selecciona un responsable válido.");
        var entity = new Activity
        {
            Title = input.Title.Trim(),
            ProjectId = input.ProjectId,
            PersonId = personId,
            CurrentEstimateHours = input.CurrentEstimateHours,
            Status = ActivityStatus.Pending
        };
        db.Activities.Add(entity);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();
        if (input.PredecessorIds.Count > 0)
            await SetDependenciesCoreAsync(db, entity.Id, input.PredecessorIds);
        await transaction.CommitAsync();
        return entity;
    }

    public async Task SetDependenciesAsync(int activityId, IReadOnlyCollection<int> predecessorIds)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        await SetDependenciesCoreAsync(db, activityId, predecessorIds);
    }

    private async Task SetDependenciesCoreAsync(ProjectManagerDbContext db, int activityId, IReadOnlyCollection<int> predecessorIds)
    {
        var activity = await db.Activities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == activityId)
            ?? throw new ArgumentException("La actividad no existe.");
        if (!await access.IsAdminAsync()) await access.RequireOwnAsync(activity.PersonId);
        var ids = predecessorIds.Distinct().ToList();
        if (ids.Contains(activityId)) throw new ArgumentException("Una actividad no puede depender de sí misma.");
        if (ids.Count > 0 && await db.Activities.CountAsync(x => ids.Contains(x.Id)) != ids.Count)
            throw new ArgumentException("Selecciona actividades existentes.");
        if (!await access.IsAdminAsync() && ids.Count > 0 &&
            await db.Activities.CountAsync(x => ids.Contains(x.Id) && x.PersonId == activity.PersonId) != ids.Count)
            throw new UnauthorizedAccessException("Selecciona predecesoras asignadas a ti.");

        var allIds = await db.Activities.Select(x => x.Id).ToListAsync();
        var edges = await db.ActivityDependencies
            .Select(x => new { x.PredecessorId, x.SuccessorId }).ToListAsync();
        var candidate = edges.Where(x => x.SuccessorId != activityId)
            .Select(x => (PredecessorId: x.PredecessorId, SuccessorId: x.SuccessorId)).ToList();
        candidate.AddRange(ids.Select(p => (PredecessorId: p, SuccessorId: activityId)));
        if (PlanningRules.HasCycle(allIds, candidate))
            throw new ArgumentException("Las dependencias generan un ciclo.");

        var existing = await db.ActivityDependencies.Where(x => x.SuccessorId == activityId).ToListAsync();
        db.ActivityDependencies.RemoveRange(existing);
        foreach (var pred in ids)
            db.ActivityDependencies.Add(new ActivityDependency { PredecessorId = pred, SuccessorId = activityId });
        await db.SaveChangesAsync();
    }

    public async Task<Activity> ContinueActivityAsync(int activityId)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        var original = await db.Activities.SingleOrDefaultAsync(x => x.Id == activityId)
            ?? throw new ArgumentException("La actividad no existe.");
        await access.RequireOwnAsync(original.PersonId);
        var recorded = await db.WorkRecords.Where(x => x.ActivityId == activityId).SumAsync(x => (decimal?)x.Hours) ?? 0;
        var remaining = Math.Max(0m, original.CurrentEstimateHours - recorded);
        var entity = new Activity
        {
            Title = $"{original.Title} (continuación)",
            ProjectId = original.ProjectId,
            PersonId = original.PersonId,
            CurrentEstimateHours = remaining,
            Status = ActivityStatus.Pending
        };
        db.Activities.Add(entity);
        db.Continuations.Add(new Continuation { FromActivityId = activityId, ToActivity = entity });
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<List<Continuation>> ListContinuationsAsync(int activityId)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Continuations.AsNoTracking()
            .Include(x => x.FromActivity).Include(x => x.ToActivity)
            .Where(x => x.FromActivityId == activityId || x.ToActivityId == activityId)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }
}
