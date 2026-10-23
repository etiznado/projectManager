using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class WeeklyPlanService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public sealed record WeekView(int PlanId, WeekLayout Layout);

    public async Task<WeekView> GetWeekAsync(DateOnly weekStart)
    {
        await access.RequireAuthenticatedAsync();
        var personId = await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        await using var db = await factory.CreateDbContextAsync();
        var workdays = await LoadWorkdaysAsync(db, personId);
        var person = await db.People.AsNoTracking().SingleAsync(x => x.Id == personId);
        var plan = await db.WeeklyPlans.AsNoTracking()
            .Include(x => x.Blocks).ThenInclude(x => x.Activity).ThenInclude(x => x.Project)
            .SingleOrDefaultAsync(x => x.PersonId == personId && x.WeekStart == weekStart);
        List<(int BlockId, int ActivityId, string Title, string? ProjectName, decimal PlannedHours)> blocks = [];
        if (plan is not null)
        {
            blocks = plan.Blocks.OrderBy(x => x.Position).ThenBy(x => x.Id)
                .Select(x => (x.Id, x.ActivityId, x.Activity.Title, x.Activity.Project?.Name, x.PlannedHours))
                .ToList();
        }
        var layout = WeekLayoutCalculator.Calculate(weekStart, person.WeeklyCapacityHours, workdays, blocks);
        return new WeekView(plan?.Id ?? 0, layout);
    }

    public async Task AddBlockAsync(int activityId, decimal hours, DateOnly weekStart, int? beforeBlockId = null)
    {
        await access.RequireAuthenticatedAsync();
        if (hours <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero.");
        var personId = await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        await using var db = await factory.CreateDbContextAsync();
        var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == activityId)
            ?? throw new ArgumentException("La actividad no existe.");
        await access.RequireOwnAsync(activity.PersonId);
        var plan = await GetOrCreatePlanAsync(db, personId, weekStart);
        var ordered = plan.Blocks.OrderBy(x => x.Position).ThenBy(x => x.Id).ToList();
        var insertion = beforeBlockId is int targetId ? ordered.FindIndex(x => x.Id == targetId) : ordered.Count;
        if (insertion < 0) throw new ArgumentException("El bloque de destino ya no existe en este plan.");
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Position = i < insertion ? i + 1 : i + 2;
        var next = insertion + 1;
        db.PlanBlocks.Add(new PlanBlock { WeeklyPlan = plan, ActivityId = activityId, Position = next, PlannedHours = hours });
        await db.SaveChangesAsync();
    }

    public async Task RemoveBlockAsync(int blockId)
    {
        await access.RequireAuthenticatedAsync();
        var personId = await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        await using var db = await factory.CreateDbContextAsync();
        var block = await db.PlanBlocks.Include(x => x.WeeklyPlan).SingleOrDefaultAsync(x => x.Id == blockId)
            ?? throw new ArgumentException("El bloque no existe.");
        if (block.WeeklyPlan.PersonId != personId && !await access.IsAdminAsync())
            throw new UnauthorizedAccessException("El plan no es tuyo.");
        db.PlanBlocks.Remove(block);
        await db.SaveChangesAsync();
    }

    public async Task SetBlockHoursAsync(int blockId, decimal hours)
    {
        await access.RequireAuthenticatedAsync();
        if (hours <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero.");
        var personId = await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        await using var db = await factory.CreateDbContextAsync();
        var block = await db.PlanBlocks.Include(x => x.WeeklyPlan).Include(x => x.Activity)
            .SingleOrDefaultAsync(x => x.Id == blockId)
            ?? throw new ArgumentException("El bloque no existe.");
        if (block.WeeklyPlan.PersonId != personId && !await access.IsAdminAsync())
            throw new UnauthorizedAccessException("El plan no es tuyo.");
        var recorded = await db.WorkRecords.Where(x => x.ActivityId == block.ActivityId).SumAsync(x => (decimal?)x.Hours) ?? 0;
        var pending = PlanningRules.RemainingHours(block.Activity.CurrentEstimateHours, recorded);
        if (hours > pending)
            throw new ArgumentException($"Las horas planeadas no pueden superar las horas pendientes ({pending:0.##} h).");
        block.PlannedHours = hours;
        await db.SaveChangesAsync();
    }

    public async Task ReorderAsync(int planId, IReadOnlyList<int> orderedBlockIds)
    {
        await access.RequireAuthenticatedAsync();
        var personId = await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        await using var db = await factory.CreateDbContextAsync();
        var plan = await db.WeeklyPlans.Include(x => x.Blocks).SingleOrDefaultAsync(x => x.Id == planId)
            ?? throw new ArgumentException("El plan no existe.");
        if (plan.PersonId != personId && !await access.IsAdminAsync())
            throw new UnauthorizedAccessException("El plan no es tuyo.");
        var existingIds = plan.Blocks.Select(x => x.Id).ToHashSet();
        if (orderedBlockIds.Count != plan.Blocks.Count || orderedBlockIds.Distinct().Count() != orderedBlockIds.Count || orderedBlockIds.Any(x => !existingIds.Contains(x)))
            throw new ArgumentException("El orden no coincide con los bloques del plan.");
        var map = plan.Blocks.ToDictionary(x => x.Id);
        for (var i = 0; i < orderedBlockIds.Count; i++)
            map[orderedBlockIds[i]].Position = i + 1;
        await db.SaveChangesAsync();
    }

    private static async Task<WeeklyPlan> GetOrCreatePlanAsync(ProjectManagerDbContext db, int personId, DateOnly weekStart)
    {
        var plan = await db.WeeklyPlans.Include(x => x.Blocks).SingleOrDefaultAsync(x => x.PersonId == personId && x.WeekStart == weekStart);
        if (plan is null)
        {
            plan = new WeeklyPlan { PersonId = personId, WeekStart = weekStart };
            db.WeeklyPlans.Add(plan);
        }
        return plan;
    }

    private static async Task<Dictionary<DayOfWeek, decimal>> LoadWorkdaysAsync(ProjectManagerDbContext db, int personId)
    {
        var rows = await db.PersonWorkdays.AsNoTracking().Where(x => x.PersonId == personId).ToListAsync();
        return rows.ToDictionary(x => x.DayOfWeek, x => x.WorkingHours);
    }
}
