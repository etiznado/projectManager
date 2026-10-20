using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public static class ProjectRules
{
    public static void ConvertToFlexible(IEnumerable<ProjectActivity> activities, IReadOnlyDictionary<DayOfWeek, decimal> oldDays)
    {
        foreach (var activity in activities.Where(x => x.EstimatedDurationType == DurationType.WorkingTime))
        {
            var original = WorkCalendarCalculator.Schedule(new(activity.RequestedStart, 0), activity.EstimatedDurationHours, oldDays);
            activity.RequestedFinish = original.Finish.Date;
            activity.EstimatedDurationType = DurationType.ManualDates;
            activity.EstimatedDurationHours = 0;
        }
    }
}

public sealed class ProjectService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task SaveAsync(int initiativeId, int projectId, ProjectInput input)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true) || string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException("Captura un nombre válido y selecciona un calendario.");
        await access.RequireAsync(WorkPermission.ManageProjects);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var project = await db.Projects.Include(x => x.WorkCalendar).ThenInclude(x => x.Days)
            .SingleOrDefaultAsync(x => x.Id == projectId && x.InitiativeId == initiativeId)
            ?? throw new ArgumentException("El proyecto no pertenece a la iniciativa seleccionada.");
        var calendar = await db.WorkCalendars.Include(x => x.Days).SingleOrDefaultAsync(x => x.Id == input.WorkCalendarId)
            ?? throw new ArgumentException("Selecciona un calendario válido.");
        var oldDays = project.WorkCalendar.Days.ToDictionary(x => x.DayOfWeek, x => x.WorkingHours);
        var activities = await db.Activities.Include(x => x.Project).ThenInclude(x => x.WorkCalendar).ThenInclude(x => x.Days)
            .Where(x => x.Project.InitiativeId == initiativeId).ToListAsync();
        if (calendar.IsFlexible) ProjectRules.ConvertToFlexible(activities.Where(x => x.ProjectId == projectId), oldDays);
        project.Name = input.Name.Trim(); project.WorkCalendar = calendar;
        var responsible = input.ResponsibleName?.Trim();
        if (string.IsNullOrWhiteSpace(responsible)) { project.Responsible = null; project.ResponsibleId = null; }
        else project.Responsible = await db.Responsibles.FirstOrDefaultAsync(x => x.Name == responsible) ?? new Responsible { Name = responsible };
        var ids = activities.Select(x => x.Id).ToList();
        var links = await db.ActivityDependencies.Where(x => ids.Contains(x.PredecessorId) || ids.Contains(x.SuccessorId)).ToListAsync();
        var result = ScheduleCalculator.Calculate(activities, links);
        foreach (var activity in activities.Where(x => x.Status != ActivityStatus.Completed))
        {
            var forecast = result[activity.Id];
            activity.EstimatedStart = forecast.Start.Date; activity.EstimatedFinish = forecast.Finish.Date;
            activity.EstimatedStartHour = forecast.Start.Hour; activity.EstimatedFinishHour = forecast.Finish.Hour;
        }
        await MilestoneService.RefreshInitiativeAsync(db, initiativeId);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}

