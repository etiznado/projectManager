using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public enum DeliveryState { OnTime, AtRisk, Overdue, Undated }

public static class InitiativeDeliveryRules
{
    public static bool IsInProgress(IReadOnlyCollection<ActivityStatus> statuses, bool hasRecordedWork) =>
        statuses.Any(status => status != ActivityStatus.Completed) &&
        (hasRecordedWork || statuses.Any(status => status is ActivityStatus.InProgress or ActivityStatus.Completed));

    public static int? DelayDays(DateOnly? agreed, DateOnly? estimated) =>
        agreed is DateOnly a && estimated is DateOnly e && e > a ? e.DayNumber - a.DayNumber : null;

    public static DeliveryState Evaluate(DateOnly today, DateOnly? initiativeDate,
        IReadOnlyCollection<(DateOnly? Agreed, DateOnly? Estimated)> projects)
    {
        if (initiativeDate < today || projects.Any(project => project.Agreed < today))
            return DeliveryState.Overdue;
        if (projects.Any(project => DelayDays(project.Agreed, project.Estimated) is not null ||
            initiativeDate is DateOnly date && project.Estimated > date))
            return DeliveryState.AtRisk;
        return initiativeDate is null && projects.All(project => project.Agreed is null)
            ? DeliveryState.Undated : DeliveryState.OnTime;
    }

    public static string Label(DeliveryState state) => state switch
    {
        DeliveryState.OnTime => "En fecha",
        DeliveryState.AtRisk => "En riesgo",
        DeliveryState.Overdue => "Fecha incumplida",
        _ => "Sin fecha"
    };
}
