using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

// Reglas puras, sin acceso a SQL: horas pendientes y validación de dependencias.
public static class PlanningRules
{
    public static decimal RemainingHours(decimal estimateHours, decimal recordedHours) => estimateHours - recordedHours;

    public static bool NeedsReestimate(ActivityStatus status, decimal remainingHours) =>
        status != ActivityStatus.Completed && remainingHours <= 0;

    public static bool HasCycle(IReadOnlyCollection<int> activityIds, IReadOnlyCollection<(int PredecessorId, int SuccessorId)> edges)
    {
        var nodes = activityIds.ToHashSet();
        var incoming = nodes.ToDictionary(x => x, _ => new List<int>());
        var outgoing = nodes.ToDictionary(x => x, _ => new List<int>());
        foreach (var (predecessor, successor) in edges)
        {
            if (!nodes.Contains(predecessor) || !nodes.Contains(successor))
                throw new ArgumentException("La dependencia referencia una actividad fuera del alcance.");
            if (predecessor == successor) return true;
            incoming[successor].Add(predecessor);
            outgoing[predecessor].Add(successor);
        }
        var degree = incoming.ToDictionary(x => x.Key, x => x.Value.Count);
        var queue = new Queue<int>(degree.Where(x => x.Value == 0).Select(x => x.Key));
        var visited = 0;
        while (queue.TryDequeue(out var id))
        {
            visited++;
            foreach (var next in outgoing[id]) if (--degree[next] == 0) queue.Enqueue(next);
        }
        return visited != nodes.Count;
    }

    public static decimal SessionHours(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start) throw new ArgumentException("El fin de la sesión debe ser posterior a su inicio.");
        return (decimal)(end - start).TotalHours;
    }
}
