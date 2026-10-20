using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed record TvRow(Project Project, ProjectActivity? Activity);
public sealed record TvDashboard(Initiative Initiative, IReadOnlyList<Project> Projects,
    IReadOnlyList<ProjectActivity> Activities, IReadOnlyList<ActivityDependency> Dependencies,
    IReadOnlyList<Milestone> Milestones, IReadOnlyList<Blocker> Blockers, DateTimeOffset UpdatedAt)
{
    public DateOnly? Forecast => Activities.Select(x => (DateOnly?)(x.Status == ActivityStatus.Completed && x.ActualFinish.HasValue ? x.ActualFinish.Value : x.EstimatedFinish))
        .Concat(Milestones.Select(x => (DateOnly?)(x.ActualDate ?? x.EstimatedDate))).Max();
    public bool Complete => Activities.Count > 0 && Activities.All(x => x.Status == ActivityStatus.Completed) && Milestones.All(x => x.ActualDate.HasValue);
    public string Health(DateOnly today)
    {
        if (Complete) return "COMPLETADA";
        if (Forecast is null) return "SIN ACTIVIDADES";
        if (Forecast > Initiative.TargetDate || today > Initiative.TargetDate) return "ATRASADO";
        if (Blockers.Count > 0 || Activities.Any(x => x.Status != ActivityStatus.Completed && x.EstimatedFinish < today)) return "EN RIESGO";
        return "EN TIEMPO";
    }
    public IReadOnlyList<TvRow> Rows => Projects.SelectMany(project =>
    {
        var rows = Activities.Where(x => x.ProjectId == project.Id && x.IsPrincipal).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => new TvRow(project, x)).ToList();
        return rows.Count > 0 ? rows : [new TvRow(project, null)];
    }).ToList();
}

public static class TvTimeline
{
    public const int PageSize = 8;
    public static IReadOnlyList<TvRow> Page(TvDashboard data, int page) => data.Rows.Skip(Math.Max(0, page) * PageSize).Take(PageSize).ToList();
    public static int PageCount(TvDashboard data) => Math.Max(1, (data.Rows.Count + PageSize - 1) / PageSize);
    public static (DateOnly Start, DateOnly End) Range(TvDashboard data, DateOnly today)
    {
        var dates = data.Activities.SelectMany(x => new[] { x.PlannedStart, x.PlannedFinish, x.EstimatedStart, x.EstimatedFinish, x.ActualStart ?? x.EstimatedStart, x.ActualFinish ?? x.EstimatedFinish })
            .Concat(data.Milestones.SelectMany(x => new[] { x.TargetDate, x.ActualDate ?? x.EstimatedDate })).Append(today).Append(data.Initiative.TargetDate).ToList();
        var min = dates.Min().AddDays(-2); var max = dates.Max().AddDays(3);
        var start = min.AddDays(-((7 + (int)min.DayOfWeek - (int)DayOfWeek.Monday) % 7));
        if (max.DayNumber - start.DayNumber < 42) max = start.AddDays(42);
        return (start, max);
    }
    public static DateOnly Finish(ProjectActivity activity) => activity.Status == ActivityStatus.Completed && activity.ActualFinish.HasValue ? activity.ActualFinish.Value : activity.EstimatedFinish;
    public static string Color(ProjectActivity activity, bool blocked, DateOnly today) => BlockerRules.Health(activity, blocked, today) switch
    {
        "Completada" => "#26d2b5", "Atrasada" => "#ff747b", "En riesgo" => "#ffc24d",
        _ => activity.Status == ActivityStatus.InProgress ? "#31aaff" : "#7c95b5"
    };
    public static string ProjectHealth(TvDashboard data, int projectId, DateOnly today)
    {
        var activities = data.Activities.Where(x => x.ProjectId == projectId).ToList();
        if (activities.Count == 0) return "Sin actividades";
        if (activities.All(x => x.Status == ActivityStatus.Completed))
            return data.Milestones.Any(x => x.ProjectId == projectId && !x.ActualDate.HasValue) ? "Por confirmar" : "Completado";
        if (activities.Max(Finish) > activities.Max(x => x.PlannedFinish) || activities.Max(Finish) < today) return "Atrasado";
        return data.Blockers.Any(x => x.Activity.ProjectId == projectId) ? "En riesgo" : "En tiempo";
    }
}
