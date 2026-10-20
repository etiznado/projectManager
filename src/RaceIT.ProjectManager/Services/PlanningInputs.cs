using System.ComponentModel.DataAnnotations;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class InitiativeInput
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
    public DateOnly TargetDate { get; set; } = DateOnly.FromDateTime(DateTime.Today).AddMonths(1);
}

public sealed class ProjectInput
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un calendario.")]
    public int WorkCalendarId { get; set; }
    [StringLength(200)] public string? ResponsibleName { get; set; }
}

public sealed class ActivityInput : IValidatableObject
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    [Range(typeof(decimal), "0", "100000", ErrorMessage = "La duración no puede ser negativa ni superar 100000 horas.")]
    public decimal DurationHours { get; set; } = 8;
    public DurationType DurationType { get; set; }
    public decimal DurationDays { get => DurationHours / 24m; set => DurationHours = value * 24m; }
    public DateOnly Finish { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly Start { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool IsPrincipal { get; set; } = true;
    [StringLength(200)] public string? ResponsibleName { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Enum.IsDefined(DurationType)) yield return new ValidationResult("Selecciona un tipo de duración válido.");
        if (DurationType == DurationType.ElapsedTime && DurationDays < 0.01m) yield return new ValidationResult("Captura al menos 0.01 días naturales.", [nameof(DurationDays)]);
        if (DurationType != DurationType.ManualDates && DurationHours <= 0) yield return new ValidationResult("Captura una duración mayor a cero.");
        if (DurationType == DurationType.ManualDates && (Finish < Start || Finish.Year > 2090)) yield return new ValidationResult("El fin debe ser igual o posterior al inicio y no superar 2090.", [nameof(Finish)]);
        if (Start.Year < 2000 || Start.Year > 2090)
            yield return new ValidationResult("Selecciona un inicio entre 2000 y 2090.", [nameof(Start)]);
    }
}

public sealed class ActivityEditInput : IValidatableObject
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    [StringLength(200)] public string? ResponsibleName { get; set; }
    public ActivityStatus Status { get; set; }
    public bool IsPrincipal { get; set; }
    public DateOnly? RequestedFinish { get; set; }
    public DateOnly EstimatedStart { get; set; }
    [Range(typeof(decimal), "0", "100000", ErrorMessage = "La duración no puede ser negativa ni superar 100000 horas.")]
    public decimal EstimatedDurationHours { get; set; }
    public DurationType EstimatedDurationType { get; set; }
    public decimal EstimatedDurationDays { get => EstimatedDurationHours / 24m; set => EstimatedDurationHours = value * 24m; }
    public DateOnly? ActualStart { get; set; }
    public DateOnly? ActualFinish { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Enum.IsDefined(EstimatedDurationType)) yield return new ValidationResult("Selecciona un tipo de duración válido.");
        if (EstimatedDurationType == DurationType.ElapsedTime && EstimatedDurationDays < 0.01m) yield return new ValidationResult("Captura al menos 0.01 días naturales.", [nameof(EstimatedDurationDays)]);
        if (EstimatedDurationType != DurationType.ManualDates && EstimatedDurationHours <= 0) yield return new ValidationResult("Captura una duración mayor a cero.");
        if (EstimatedDurationType == DurationType.ManualDates && (!RequestedFinish.HasValue || RequestedFinish < EstimatedStart || RequestedFinish.Value.Year > 2090)) yield return new ValidationResult("Captura un fin solicitado igual o posterior al inicio, sin superar 2090.", [nameof(RequestedFinish)]);
        if (!Enum.IsDefined(Status))
            yield return new ValidationResult("Selecciona un estado válido.", [nameof(Status)]);
        if (EstimatedStart.Year < 2000 || EstimatedStart.Year > 2090)
            yield return new ValidationResult("El inicio estimado debe estar entre 2000 y 2090.", [nameof(EstimatedStart)]);
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (ActualStart > today || ActualFinish > today)
            yield return new ValidationResult("Las fechas reales no pueden estar en el futuro.");
        if (ActualFinish.HasValue && (!ActualStart.HasValue || ActualFinish < ActualStart))
            yield return new ValidationResult("El fin real requiere un inicio real y no puede ser anterior a él.", [nameof(ActualFinish)]);
        if (Status == ActivityStatus.Pending && (ActualStart.HasValue || ActualFinish.HasValue))
            yield return new ValidationResult("Una actividad pendiente no debe tener fechas reales.");
        if (Status == ActivityStatus.InProgress && (!ActualStart.HasValue || ActualFinish.HasValue))
            yield return new ValidationResult("Una actividad en curso requiere inicio real y debe dejar el fin real vacío.");
        if (Status == ActivityStatus.Completed && (!ActualStart.HasValue || !ActualFinish.HasValue))
            yield return new ValidationResult("Una actividad completada requiere inicio y fin reales.");
    }
}


