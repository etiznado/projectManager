using System.Data;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class ActivityPlanningService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task SaveAsync(int id, ActivityEditInput input, IReadOnlyCollection<int> predecessorIds)
    {
        await using var db = await factory.CreateDbContextAsync();
        // Serialize graph reads/writes so concurrent edits cannot jointly introduce a cycle.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var initiativeId = await db.Activities.Where(x => x.Id == id).Select(x => (int?)x.Project.InitiativeId)
            .SingleOrDefaultAsync() ?? throw new ArgumentException("La actividad ya no existe.");
        var activities = await db.Activities.Include(x => x.Project).ThenInclude(x => x.WorkCalendar).ThenInclude(x => x.Days)
            .Where(x => x.Project.InitiativeId == initiativeId).ToListAsync();
        var activity = activities.Single(x => x.Id == id);
        await access.RequireActivityAsync(activity.ResponsibleId);
        if (!await access.IsAdminAsync()) {
            var oldName = await db.Responsibles.Where(x => x.Id == activity.ResponsibleId).Select(x => x.Name).SingleOrDefaultAsync();
            if (input.ResponsibleName != oldName || input.IsPrincipal != activity.IsPrincipal) throw new ArgumentException("No puedes reasignar trabajo ni cambiar su visibilidad en TV.");
            var existing = await db.ActivityDependencies.Where(x => x.SuccessorId == id).Select(x => x.PredecessorId).ToListAsync();
            var changed = existing.Except(predecessorIds).Concat(predecessorIds.Except(existing)).ToHashSet();
            if (activities.Any(x => changed.Contains(x.Id) && x.ResponsibleId != activity.ResponsibleId)) throw new ArgumentException("Solo el administrador puede cambiar dependencias con actividades ajenas.");
        }
        var ids = activities.Select(x => x.Id).ToHashSet();
        if (predecessorIds.Contains(id)) throw new ArgumentException("Una actividad no puede depender de sí misma.");
        if (predecessorIds.Any(x => !ids.Contains(x))) throw new ArgumentException("Selecciona actividades de la misma iniciativa.");
        var edges = await db.ActivityDependencies.Where(x => ids.Contains(x.SuccessorId) || ids.Contains(x.PredecessorId)).ToListAsync();
        var desired = predecessorIds.ToHashSet();
        var removed = edges.Where(x => x.SuccessorId == id && !desired.Contains(x.PredecessorId)).ToList();
        foreach (var edge in removed) { edges.Remove(edge); db.ActivityDependencies.Remove(edge); }
        foreach (var predecessorId in desired)
        {
            if (edges.Any(x => x.SuccessorId == id && x.PredecessorId == predecessorId)) continue;
            var edge = new ActivityDependency { PredecessorId = predecessorId, SuccessorId = id };
            edges.Add(edge); db.ActivityDependencies.Add(edge);
        }
        if (input.Status == ActivityStatus.Completed && await db.Blockers.AnyAsync(x => x.ActivityId == id && x.ResolvedAt == null))
            throw new ArgumentException("Resuelve los bloqueos abiertos antes de completar la actividad.");
        if (activity.Project.WorkCalendar.IsFlexible && input.EstimatedDurationType == DurationType.WorkingTime)
            throw new ArgumentException("Este proyecto es flexible. Usa fechas manuales o días naturales.");
        ActivityEditor.Apply(activity, input, activity.Project.WorkCalendar.Days.ToDictionary(x => x.DayOfWeek, x => x.WorkingHours));
        activity.RequestedStart = input.EstimatedStart;
        var forecasts = ScheduleCalculator.Calculate(activities, edges);
        foreach (var item in activities.Where(x => x.Status != ActivityStatus.Completed))
        {
            var forecast = forecasts[item.Id];
            item.EstimatedStart = forecast.Start.Date; item.EstimatedStartHour = forecast.Start.Hour;
            item.EstimatedFinish = forecast.Finish.Date; item.EstimatedFinishHour = forecast.Finish.Hour;
        }
        // Employee edits preserve the assigned ID, including when names are duplicated.
        if (await access.IsAdminAsync())
        {
            var name = input.ResponsibleName?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { activity.ResponsibleId = null; activity.Responsible = null; }
            else activity.Responsible = await db.Responsibles.FirstOrDefaultAsync(x => x.Name == name) ?? new Responsible { Name = name };
        }
        await MilestoneService.RefreshInitiativeAsync(db, initiativeId);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}



