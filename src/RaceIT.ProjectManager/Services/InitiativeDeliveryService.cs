using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed record DeliveryActivity(int Id, string Title, ActivityStatus Status);
public sealed record DeliveryProject(int Id, string Name, DateOnly? AgreedDate, DateOnly? EstimatedDate,
    IReadOnlyList<DeliveryActivity> Activities)
{
    public DateOnly? SummaryDate => EstimatedDate ?? AgreedDate;
    public int? DelayDays => InitiativeDeliveryRules.DelayDays(AgreedDate, EstimatedDate);
}
public sealed record DeliveryInitiative(int Id, string Name, DeliveryState State, IReadOnlyList<DeliveryProject> Projects);

public sealed class InitiativeDeliveryService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task<IReadOnlyList<DeliveryInitiative>> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await access.UserAsync();
        var personId = WorkAccess.PersonIdOf(user);
        if (user.Identity?.IsAuthenticated != true || personId is null)
            throw new UnauthorizedAccessException("Inicia sesión para continuar.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        // Preserve the personal panorama, including for an administrator.
        var projects = await db.Projects.AsNoTracking().Include(project => project.Initiative)
            .Where(project => project.PersonId == personId && project.InitiativeId != null)
            .OrderBy(project => project.Name).ThenBy(project => project.Id).ToListAsync(cancellationToken);
        var projectIds = projects.Select(project => project.Id).ToArray();
        var activities = (await db.Activities.AsNoTracking()
            .Where(activity => activity.PersonId == personId && activity.ProjectId != null && projectIds.Contains(activity.ProjectId.Value))
            .ToListAsync(cancellationToken)).OrderBy(activity => activity.CreatedAt).ThenBy(activity => activity.Id).ToList();
        var activityIds = activities.Select(activity => activity.Id).ToArray();
        var workedIds = (await db.WorkRecords.AsNoTracking()
            .Where(record => activityIds.Contains(record.ActivityId) && record.Hours > 0)
            .Select(record => record.ActivityId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        var initiativeIds = projects.Select(project => project.InitiativeId!.Value).Distinct().ToArray();
        var closures = await db.JarvisPlanningStates.AsNoTracking().Where(state => state.IsClosed &&
            (state.EntityKind == "project" && projectIds.Contains(state.EntityId) ||
             state.EntityKind == "initiative" && initiativeIds.Contains(state.EntityId)))
            .Select(state => new { state.EntityKind, state.EntityId }).ToListAsync(cancellationToken);
        var closedProjects = closures.Where(state => state.EntityKind == "project").Select(state => state.EntityId).ToHashSet();
        var closedInitiatives = closures.Where(state => state.EntityKind == "initiative").Select(state => state.EntityId).ToHashSet();
        var byProject = activities.ToLookup(activity => activity.ProjectId!.Value);
        var active = projects.Where(project => !closedProjects.Contains(project.Id) && !closedInitiatives.Contains(project.InitiativeId!.Value) &&
            InitiativeDeliveryRules.IsInProgress(byProject[project.Id].Select(activity => activity.Status).ToArray(),
                byProject[project.Id].Any(activity => workedIds.Contains(activity.Id))));
        var today = DateOnly.FromDateTime(DateTime.Today);
        return active.GroupBy(project => project.InitiativeId!.Value)
            .OrderBy(group => group.First().Initiative!.Name).ThenBy(group => group.Key)
            .Select(group =>
            {
                var initiative = group.First().Initiative!;
                var rows = group.Select(project => new DeliveryProject(project.Id, project.Name, project.TargetDate, project.EstimatedDate,
                    byProject[project.Id].Select(activity => new DeliveryActivity(activity.Id, activity.Title, activity.Status)).ToArray())).ToArray();
                return new DeliveryInitiative(initiative.Id, initiative.Name,
                    InitiativeDeliveryRules.Evaluate(today, initiative.TargetDate, rows.Select(row => (row.AgreedDate, row.EstimatedDate)).ToArray()), rows);
            }).ToArray();
    }
}
