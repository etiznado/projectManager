using System.ComponentModel.DataAnnotations;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public static class ActivityEditor
{
    public static void Apply(ProjectActivity activity, ActivityEditInput input, IReadOnlyDictionary<DayOfWeek, decimal> days)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
            throw new ArgumentException(string.Join(" ", errors.Select(x => x.ErrorMessage)));
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Captura el nombre.");
        var finish = input.EstimatedDurationType == DurationType.ManualDates ? input.RequestedFinish!.Value
            : WorkCalendarCalculator.Finish(input.EstimatedStart, input.EstimatedDurationHours, days, input.EstimatedDurationType);
        activity.Name = input.Name.Trim();
        activity.Status = input.Status;
        activity.IsPrincipal = input.IsPrincipal;
        activity.EstimatedStart = input.EstimatedStart;
        activity.EstimatedDurationHours = input.EstimatedDurationType == DurationType.ManualDates ? 0 : input.EstimatedDurationHours;
        activity.RequestedFinish = input.EstimatedDurationType == DurationType.ManualDates ? input.RequestedFinish : null;
        activity.EstimatedDurationType = input.EstimatedDurationType;
        activity.EstimatedFinish = finish;
        activity.ActualStart = input.ActualStart;
        activity.ActualFinish = input.ActualFinish;
        // PlannedStart, PlannedFinish and PlannedDurationHours are deliberately preserved.
    }
}


