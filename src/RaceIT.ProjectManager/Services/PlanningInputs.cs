using System.ComponentModel.DataAnnotations;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class PersonInput : IValidatableObject
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    [EmailAddress, StringLength(200)] public string? Email { get; set; }
    public PersonRole Role { get; set; } = PersonRole.User;
    public PlanningMode PlanningMode { get; set; } = PlanningMode.Weekly;
    [Range(typeof(decimal), "0", "100000", ErrorMessage = "La bolsa semanal no puede ser negativa.")]
    public decimal WeeklyCapacityHours { get; set; } = 45;
    public List<WorkdayInput> Workdays { get; set; } = Enum.GetValues<DayOfWeek>()
        .OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day)
        .Select(day => new WorkdayInput
        {
            DayOfWeek = day,
            WorkingHours = day == DayOfWeek.Sunday ? 0m : day == DayOfWeek.Saturday ? 5m : 8m
        }).ToList();

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Enum.IsDefined(Role)) yield return new ValidationResult("Selecciona un rol válido.");
        if (!Enum.IsDefined(PlanningMode)) yield return new ValidationResult("Selecciona una forma de planear válida.");
        if (Workdays is null || Workdays.Count != 7 ||
            Workdays.Any(x => !Enum.IsDefined(x.DayOfWeek)) ||
            Workdays.Select(x => x.DayOfWeek).Distinct().Count() != 7)
            yield return new ValidationResult("Captura los siete días de la jornada.", [nameof(Workdays)]);
        else if (Workdays.Any(x => x.WorkingHours < 0 || x.WorkingHours > 24 || decimal.Round(x.WorkingHours, 2) != x.WorkingHours))
            yield return new ValidationResult("Cada día debe tener entre 0 y 24 horas, con hasta dos decimales.", [nameof(Workdays)]);
    }
}

public sealed class WorkdayInput
{
    public DayOfWeek DayOfWeek { get; set; }
    public decimal WorkingHours { get; set; }
}

public sealed class InitiativeInput
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
    public DateOnly? TargetDate { get; set; }
}

public sealed class ProjectInput
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    public int? InitiativeId { get; set; }
    public int? PersonId { get; set; }
    public DateOnly? TargetDate { get; set; }
}

public sealed class ActivityInput : IValidatableObject
{
    [Required(ErrorMessage = "Captura el título."), StringLength(200)]
    public string Title { get; set; } = "";
    public int? ProjectId { get; set; }
    public int? PersonId { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "La estimación debe ser mayor a cero.")]
    public decimal CurrentEstimateHours { get; set; } = 8;
    public List<int> PredecessorIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        yield break;
    }
}

public sealed class TemplateEditorInput : IValidatableObject
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    public TemplateCategory Category { get; set; } = TemplateCategory.Before;
    public List<TemplateItemEditInput> Items { get; set; } = [new()];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Captura el nombre.", [nameof(Name)]);
        if (!Enum.IsDefined(Category))
            yield return new ValidationResult("Selecciona una categoría válida.", [nameof(Category)]);
        if (Items.Count == 0)
            yield return new ValidationResult("La plantilla debe conservar al menos un elemento.", [nameof(Items)]);
        if (Items.Any(x => string.IsNullOrWhiteSpace(x.Title) || x.Title.Length > 200 || x.EstimatedHours <= 0 || x.EstimatedHours > 1000000))
            yield return new ValidationResult("Cada elemento necesita un título de hasta 200 caracteres y horas mayores a cero.", [nameof(Items)]);
    }
}

public sealed class TemplateItemEditInput
{
    public int? Id { get; set; }
    public string Title { get; set; } = "";
    public decimal EstimatedHours { get; set; } = 1;
}

public sealed class ManualTimeInput : IValidatableObject
{
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Captura una cantidad mayor a cero.")]
    public decimal Hours { get; set; }
    [StringLength(2000)] public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        yield break;
    }
}

// Horas planeadas de un bloque del plan semanal. El máximo es dinámico: las horas
// pendientes de la actividad (estimación − registradas).
public sealed class BlockHoursInput : IValidatableObject
{
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Captura una cantidad mayor a cero.")]
    public decimal Hours { get; set; }

    public decimal MaxHours { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Hours > MaxHours)
            yield return new ValidationResult(
                $"Las horas planeadas no pueden superar las pendientes ({MaxHours:0.##} h).",
                new[] { nameof(Hours) });
    }
}
