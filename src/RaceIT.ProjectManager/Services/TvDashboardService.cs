using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;

namespace RaceIT.ProjectManager.Services;

public sealed class TvDashboardService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task<TvDashboard?> LoadAsync(int initiativeId, CancellationToken token = default)
    {
        await access.RequireAsync(WorkPermission.ReadAll);
        await using var db = await factory.CreateDbContextAsync(token);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, token);
        var initiative = await db.Initiatives.AsNoTracking().SingleOrDefaultAsync(x => x.Id == initiativeId, token);
        if (initiative is null) return null;
        var projects = await db.Projects.AsNoTracking().Where(x => x.InitiativeId == initiativeId).OrderBy(x => x.Id).ToListAsync(token);
        var activities = await db.Activities.AsNoTracking().Include(x => x.Project).ThenInclude(x => x.WorkCalendar).ThenInclude(x => x.Days)
            .Where(x => x.Project.InitiativeId == initiativeId).ToListAsync(token);
        var dependencies = await db.ActivityDependencies.AsNoTracking().Where(x => x.Successor.Project.InitiativeId == initiativeId && x.Predecessor.Project.InitiativeId == initiativeId).ToListAsync(token);
        var milestones = await db.Milestones.AsNoTracking().Include(x => x.Project).Where(x => x.Project.InitiativeId == initiativeId).OrderBy(x => x.EstimatedDate).ToListAsync(token);
        var blockers = await db.Blockers.AsNoTracking().Include(x => x.Activity).ThenInclude(x => x.Project).Include(x => x.Responsible)
            .Where(x => x.Activity.Project.InitiativeId == initiativeId && x.ResolvedAt == null).OrderBy(x => x.CreatedAt).ToListAsync(token);
        await tx.CommitAsync(token);
        return new(initiative, projects, activities, dependencies, milestones, blockers, DateTimeOffset.Now);
    }
}
