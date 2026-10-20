using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class MilestoneService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task SaveAsync(int projectId, int? milestoneId, MilestoneInput input, IReadOnlyCollection<int> requirementIds)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true) || string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException("Captura un nombre de hasta 200 caracteres.");
        if (input.TargetDate.Year < 2000 || input.TargetDate.Year > 2090) throw new ArgumentException("La fecha objetivo debe estar entre 2000 y 2090.");
        await access.RequireAsync(WorkPermission.ManageProjects);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var project = await db.Projects.SingleOrDefaultAsync(x => x.Id == projectId) ?? throw new ArgumentException("Selecciona un proyecto válido.");
        var ids = requirementIds.ToHashSet();
        var activities = await db.Activities.Where(x => ids.Contains(x.Id) && x.Project.InitiativeId == project.InitiativeId).ToListAsync();
        if (activities.Count != ids.Count) throw new ArgumentException("Las actividades requeridas deben pertenecer a la misma iniciativa.");
        var estimate = MilestoneRules.Estimate(activities);
        var milestone = milestoneId.HasValue
            ? await db.Milestones.Include(x => x.Requirements).SingleOrDefaultAsync(x => x.Id == milestoneId && x.ProjectId == projectId)
                ?? throw new ArgumentException("El milestone ya no existe en este proyecto.")
            : new Milestone { ProjectId = projectId };
        if (milestone.ActualDate.HasValue) throw new ArgumentException("Reabre el milestone antes de cambiar su definición.");
        milestone.Name = input.Name.Trim(); milestone.TargetDate = input.TargetDate; milestone.EstimatedDate = estimate;
        foreach (var old in milestone.Requirements.Where(x => !ids.Contains(x.ActivityId)).ToList())
        { milestone.Requirements.Remove(old); db.MilestoneRequirements.Remove(old); }
        foreach (var activity in activities.Where(x => !milestone.Requirements.Any(r => r.ActivityId == x.Id)))
            milestone.Requirements.Add(new MilestoneRequirement { ActivityId = activity.Id, Activity = activity });
        if (!milestoneId.HasValue) db.Milestones.Add(milestone);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task ConfirmAsync(int projectId, int id, DateOnly date, string approverName)
    {
        if (string.IsNullOrWhiteSpace(approverName) || approverName.Trim().Length > 200)
            throw new ArgumentException("Indica quién confirma el milestone (hasta 200 caracteres).");
        await access.RequireAsync(WorkPermission.ManageProjects);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var milestone = await db.Milestones.Include(x => x.Requirements).ThenInclude(x => x.Activity)
            .SingleOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId) ?? throw new ArgumentException("El milestone no existe en este proyecto.");
        var requiredIds = milestone.Requirements.Select(x => x.ActivityId).ToList();
        if (await db.Blockers.AnyAsync(x => requiredIds.Contains(x.ActivityId) && x.ResolvedAt == null))
            throw new ArgumentException("Resuelve los bloqueos de las actividades requeridas antes de confirmar el milestone.");
        MilestoneRules.ValidateConfirmation(milestone, date, DateOnly.FromDateTime(DateTime.Today));
        var name = approverName.Trim();
        var responsible = await db.Responsibles.FirstOrDefaultAsync(x => x.Name == name) ?? new Responsible { Name = name };
        milestone.ActualDate = date;
        foreach (var requirement in milestone.Requirements) { requirement.ApprovedDate = date; requirement.ApprovedBy = responsible; }
        MilestoneRules.Refresh(milestone);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task ReopenAsync(int projectId, int id)
    {
        await access.RequireAsync(WorkPermission.ManageProjects);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var milestone = await db.Milestones.Include(x => x.Requirements).ThenInclude(x => x.Activity)
            .SingleOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId) ?? throw new ArgumentException("El milestone no existe en este proyecto.");
        MilestoneRules.Reopen(milestone); MilestoneRules.Refresh(milestone);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    // The caller owns the context and transaction, so activity and milestone changes commit together.
    public static async Task RefreshInitiativeAsync(ProjectManagerDbContext db, int initiativeId)
    {
        var milestones = await db.Milestones.Include(x => x.Requirements).ThenInclude(x => x.Activity)
            .Where(x => x.Project.InitiativeId == initiativeId).ToListAsync();
        foreach (var milestone in milestones) MilestoneRules.Refresh(milestone);
    }
}


