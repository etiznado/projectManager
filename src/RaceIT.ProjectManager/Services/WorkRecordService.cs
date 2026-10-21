using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class WorkRecordService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public sealed record ActivityDetail(
        Activity Activity,
        IReadOnlyList<WorkRecord> Records,
        WorkRecord? OpenSession,
        decimal RecordedHours)
    {
        public decimal RemainingHours => Activity.CurrentEstimateHours - RecordedHours;
        public bool NeedsReestimate => PlanningRules.NeedsReestimate(Activity.Status, RemainingHours);
    }

    public async Task<ActivityDetail> GetDetailAsync(int activityId)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        var activity = await db.Activities.AsNoTracking()
            .Include(x => x.Project).Include(x => x.Person)
            .SingleOrDefaultAsync(x => x.Id == activityId)
            ?? throw new ArgumentException("La actividad no existe.");
        await access.RequireOwnAsync(activity.PersonId);
        var total = await db.WorkRecords.Where(x => x.ActivityId == activityId).SumAsync(x => (decimal?)x.Hours) ?? 0;
        var records = await db.WorkRecords.AsNoTracking()
            .Where(x => x.ActivityId == activityId)
            .OrderByDescending(x => x.Id)
            .Take(20)
            .ToListAsync();
        var open = await db.WorkRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ActivityId == activityId && x.Kind == WorkRecordKind.Session && x.End == null);
        return new ActivityDetail(activity, records, open, total);
    }

    public async Task StartSessionAsync(int activityId)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        var (activity, personId) = await ResolveOwnedAsync(db, activityId);
        var now = DateTimeOffset.Now;
        var openSessions = await db.WorkRecords
            .Where(x => x.PersonId == personId && x.Kind == WorkRecordKind.Session && x.End == null)
            .ToListAsync();
        foreach (var open in openSessions)
        {
            var start = open.Start ?? now;
            open.End = now;
            open.Hours = PlanningRules.SessionHours(start, now);
        }
        db.WorkRecords.Add(new WorkRecord
        {
            PersonId = personId,
            ActivityId = activityId,
            Kind = WorkRecordKind.Session,
            Start = now
        });
        if (activity.Status == ActivityStatus.Pending) activity.Status = ActivityStatus.InProgress;
        await db.SaveChangesAsync();
    }

    public async Task PauseSessionAsync(int activityId)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        var (_, personId) = await ResolveOwnedAsync(db, activityId);
        var open = await db.WorkRecords.SingleOrDefaultAsync(x =>
            x.PersonId == personId && x.ActivityId == activityId && x.Kind == WorkRecordKind.Session && x.End == null);
        if (open is null) throw new ArgumentException("No hay una sesión abierta para esta actividad.");
        var now = DateTimeOffset.Now;
        var start = open.Start ?? now;
        open.End = now;
        open.Hours = PlanningRules.SessionHours(start, now);
        await db.SaveChangesAsync();
    }

    public async Task RecordManualAsync(int activityId, decimal hours, string? note)
    {
        await access.RequireAuthenticatedAsync();
        if (hours <= 0) throw new ArgumentException("Registra una cantidad mayor a cero.");
        await using var db = await factory.CreateDbContextAsync();
        var (_, personId) = await ResolveOwnedAsync(db, activityId);
        db.WorkRecords.Add(new WorkRecord
        {
            PersonId = personId,
            ActivityId = activityId,
            Kind = WorkRecordKind.Manual,
            Hours = hours,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
        await db.SaveChangesAsync();
    }

    public async Task CompleteAsync(int activityId)
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        var (activity, _) = await ResolveOwnedAsync(db, activityId);
        var now = DateTimeOffset.Now;
        var openSessions = await db.WorkRecords
            .Where(x => x.ActivityId == activityId && x.Kind == WorkRecordKind.Session && x.End == null)
            .ToListAsync();
        foreach (var open in openSessions)
        {
            var start = open.Start ?? now;
            open.End = now;
            open.Hours = PlanningRules.SessionHours(start, now);
        }
        activity.Status = ActivityStatus.Completed;
        await db.SaveChangesAsync();
    }

    private async Task<(Activity Activity, int PersonId)> ResolveOwnedAsync(ProjectManagerDbContext db, int activityId)
    {
        var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == activityId)
            ?? throw new ArgumentException("La actividad no existe.");
        await access.RequireOwnAsync(activity.PersonId);
        var personId = activity.PersonId ?? throw new ArgumentException("La actividad no tiene responsable asignado.");
        return (activity, personId);
    }
}
