using System.ComponentModel.DataAnnotations;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class BlockerInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una actividad.")]
    public int ActivityId { get; set; }
    [Required(ErrorMessage = "Describe qué impide avanzar."), StringLength(2000)]
    public string Description { get; set; } = "";
    [StringLength(200)] public string? ResponsibleName { get; set; }
}

public static class BlockerRules
{
    public static void RequireUnfinished(ProjectActivity activity)
    {
        if (activity.Status == ActivityStatus.Completed)
            throw new ArgumentException("No se puede abrir un bloqueo en una actividad completada. Reabre la actividad primero.");
    }
    public static void Resolve(Blocker blocker, DateTimeOffset now)
    {
        if (now < blocker.CreatedAt) throw new ArgumentException("La resolución no puede ser anterior a la creación del bloqueo.");
        blocker.ResolvedAt ??= now;
    }
    public static void Reopen(Blocker blocker)
    {
        RequireUnfinished(blocker.Activity);
        blocker.ResolvedAt = null;
    }
    public static string Health(ProjectActivity activity, bool openBlocker, DateOnly today)
    {
        if (activity.Status == ActivityStatus.Completed) return "Completada";
        if (activity.EstimatedFinish > activity.PlannedFinish || activity.EstimatedFinish < today) return "Atrasada";
        return openBlocker ? "En riesgo" : "En tiempo";
    }
}
