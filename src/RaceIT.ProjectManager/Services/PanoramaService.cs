using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class PanoramaService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public sealed record ProjectRow(
        int ProjectId, string Name, string? InitiativeName, DateOnly? TargetDate,
        decimal PendingHours, decimal RecordedHours, bool IndividuallyAtRisk, bool SharedConflict);

    public sealed record Panorama(decimal WeeklyCapacityHours, IReadOnlyList<ProjectRow> Projects, RiskResult Risk, decimal TotalPending)
    {
        public int ProjectCount => Projects.Count;
        public int InConflictCount => Risk.Projects.Count(p => p.IndividuallyAtRisk || p.SharedConflict);
    }

    public async Task<Panorama> GetAsync()
    {
        await access.RequireAuthenticatedAsync();
        var personId = await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        await using var db = await factory.CreateDbContextAsync();
        var person = await db.People.AsNoTracking().SingleAsync(x => x.Id == personId);
        var workdays = await db.PersonWorkdays.AsNoTracking().Where(x => x.PersonId == personId).ToDictionaryAsync(x => x.DayOfWeek, x => x.WorkingHours);
        var projects = await db.Projects.AsNoTracking().Include(x => x.Initiative).Where(x => x.PersonId == personId).OrderBy(x => x.Name).ToListAsync();
        var activities = await db.Activities.AsNoTracking().Where(x => x.PersonId == personId).ToListAsync();

        var ids = activities.Select(x => x.Id).ToList();
        var recorded = new Dictionary<int, decimal>();
        if (ids.Count > 0)
        {
            var totals = await db.WorkRecords.Where(x => ids.Contains(x.ActivityId))
                .GroupBy(x => x.ActivityId)
                .Select(g => new { ActivityId = g.Key, Hours = g.Sum(x => x.Hours) })
                .ToListAsync();
            recorded = totals.ToDictionary(x => x.ActivityId, x => x.Hours);
        }

        decimal Remaining(Activity a) => a.Status == ActivityStatus.Completed
            ? 0m
            : Math.Max(0m, a.CurrentEstimateHours - recorded.GetValueOrDefault(a.Id));

        var rows = projects.Select(p => new ProjectRow(
            p.Id, p.Name, p.Initiative?.Name, p.TargetDate,
            activities.Where(a => a.ProjectId == p.Id).Sum(Remaining),
            activities.Where(a => a.ProjectId == p.Id).Sum(a => recorded.GetValueOrDefault(a.Id)),
            false, false)).ToList();

        var independentPending = activities.Where(a => a.ProjectId is null).Sum(Remaining);
        var totalPending = rows.Sum(r => r.PendingHours) + independentPending;

        var risk = RiskCalculator.Evaluate(
            DateOnly.FromDateTime(DateTime.Today),
            person.WeeklyCapacityHours,
            workdays,
            rows.Select(r => (r.ProjectId, r.PendingHours, r.TargetDate)).ToList());

        var riskMap = risk.Projects.ToDictionary(x => x.ProjectId);
        var finalRows = rows.Select(r =>
        {
            riskMap.TryGetValue(r.ProjectId, out var pr);
            return pr is null ? r : r with { IndividuallyAtRisk = pr.IndividuallyAtRisk, SharedConflict = pr.SharedConflict };
        }).ToList();

        return new Panorama(person.WeeklyCapacityHours, finalRows, risk, totalPending);
    }
}
