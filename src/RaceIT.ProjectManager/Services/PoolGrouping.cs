namespace RaceIT.ProjectManager.Services;

// Grupo del pool de Mi semana: las actividades abiertas de un mismo proyecto.
public sealed record ActivityPoolGroup(int? ProjectId, string Name, IReadOnlyList<ActivityService.ActivityRow> Rows)
{
    public int Count => Rows.Count;
}

// Regla pura: agrupa las actividades del pool por proyecto.
// "Sin proyecto" va al inicio; el resto se ordena alfabéticamente por nombre de proyecto.
public static class PoolGrouping
{
    public const string NoProjectName = "Sin proyecto";

    public static IReadOnlyList<ActivityPoolGroup> Group(IEnumerable<ActivityService.ActivityRow> rows)
    {
        var list = rows.ToList();
        var groups = new List<ActivityPoolGroup>();
        var withoutProject = list.Where(r => r.Activity.ProjectId is null).ToList();
        if (withoutProject.Count > 0) groups.Add(new ActivityPoolGroup(null, NoProjectName, withoutProject));
        groups.AddRange(list
            .Where(r => r.Activity.ProjectId is not null)
            .GroupBy(r => r.Activity.ProjectId!.Value)
            .OrderBy(g => g.First().Activity.Project?.Name ?? NoProjectName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ActivityPoolGroup(g.Key, g.First().Activity.Project?.Name ?? NoProjectName, g.ToList())));
        return groups;
    }
}
