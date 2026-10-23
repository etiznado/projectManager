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

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Enum.IsDefined(Role)) yield return new ValidationResult("Selecciona un rol válido.");
        if (!Enum.IsDefined(PlanningMode)) yield return new ValidationResult("Selecciona una forma de planear válida.");
    }
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

public sealed class TemplateItemInput : IValidatableObject
{
    [Required(ErrorMessage = "Captura el título."), StringLength(200)]
    public string Title { get; set; } = "";
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Las horas deben ser mayores a cero.")]
    public decimal EstimatedHours { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        yield break;
    }
}

public sealed class TemplateInput : IValidatableObject
{
    [Required(ErrorMessage = "Captura el nombre."), StringLength(200)]
    public string Name { get; set; } = "";
    public TemplateCategory Category { get; set; } = TemplateCategory.Before;
    public List<TemplateItemInput> Items { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Enum.IsDefined(Category)) yield return new ValidationResult("Selecciona una categoría válida.");
    }
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
