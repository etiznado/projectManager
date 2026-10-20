using System.ComponentModel.DataAnnotations;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class MilestoneInput
{
    [Required(ErrorMessage = "Captura el nombre del milestone."), StringLength(200)]
    public string Name { get; set; } = "";
    public DateOnly TargetDate { get; set; } = DateOnly.FromDateTime(DateTime.Today).AddMonths(1);
}

public static class MilestoneRules
{
    public static DateOnly Estimate(IEnumerable<ProjectActivity> activities)
    {
        var items = activities.ToList();
        if (items.Count == 0) throw new ArgumentException("Selecciona al menos una actividad requerida.");
        return items.Max(x => x.Status == ActivityStatus.Completed && x.ActualFinish.HasValue ? x.ActualFinish.Value : x.EstimatedFinish);
    }

    public static void ValidateConfirmation(Milestone milestone, DateOnly date, DateOnly today)
    {
        if (milestone.ActualDate.HasValue) throw new ArgumentException("El milestone ya está confirmado.");
        if (date > today || date.Year < 2000) throw new ArgumentException("La fecha de cumplimiento no puede estar en el futuro ni ser anterior a 2000.");
        if (milestone.Requirements.Count == 0 || milestone.Requirements.Any(x => x.Activity.Status != ActivityStatus.Completed || x.Activity.ActualFinish is null))
            throw new ArgumentException("Completa todas las actividades requeridas antes de confirmar el milestone.");
        if (milestone.Requirements.Any(x => x.Activity.ActualFinish > date))
            throw new ArgumentException("La fecha de cumplimiento no puede ser anterior al fin real de sus actividades.");
    }

    public static void Refresh(Milestone milestone)
    {
        milestone.EstimatedDate = Estimate(milestone.Requirements.Select(x => x.Activity));
        if (milestone.ActualDate is DateOnly confirmed && milestone.Requirements.Any(x =>
            x.Activity.Status != ActivityStatus.Completed || x.Activity.ActualFinish is null || x.Activity.ActualFinish > confirmed))
            Reopen(milestone);
    }

    public static void Reopen(Milestone milestone)
    {
        milestone.ActualDate = null;
        foreach (var requirement in milestone.Requirements)
        {
            requirement.ApprovedDate = null;
            requirement.ApprovedById = null;
            requirement.ApprovedBy = null;
        }
    }
}

public sealed class MilestoneConfirmationInput
{
    [Required(ErrorMessage = "Indica quién confirma el milestone."), StringLength(200)]
    public string ApproverName { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
