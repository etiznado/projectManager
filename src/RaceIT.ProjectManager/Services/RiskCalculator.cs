using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed record ProjectRisk(int ProjectId, decimal PendingHours, DateOnly? TargetDate, decimal CapacityHours, bool IndividuallyAtRisk, bool SharedConflict);
public sealed record RiskResult(
    DateOnly Today,
    decimal WeeklyCapacityHours,
    DateOnly? MaxDeadline,
    decimal TotalPending,
    decimal CapacityToMax,
    decimal ExcessHours,
    bool SharedConflict,
    IReadOnlyList<ProjectRisk> Projects);

// Regla pura: compara trabajo pendiente por fecha meta contra la capacidad de referencia de una sola bolsa semanal.
public static class RiskCalculator
{
    public static RiskResult Evaluate(
        DateOnly today,
        decimal weeklyCapacityHours,
        IReadOnlyDictionary<DayOfWeek, decimal> workdays,
        IReadOnlyList<(int ProjectId, decimal PendingHours, DateOnly? TargetDate)> projects)
    {
        var workDaysPerWeek = workdays.Values.Count(x => x > 0);
        var daily = workDaysPerWeek == 0 ? 0m : weeklyCapacityHours / workDaysPerWeek;

        var withDeadline = projects.Where(p => p.TargetDate.HasValue).ToList();
        var maxDeadline = withDeadline.Count == 0 ? (DateOnly?)null : withDeadline.Max(p => p.TargetDate!.Value);
        var totalPending = withDeadline.Sum(p => p.PendingHours);
        var capacityToMax = maxDeadline is DateOnly d ? CapacityTo(today, d, workdays, daily) : 0m;
        var sharedConflict = withDeadline.Count > 0 && totalPending > capacityToMax;
        var excess = Math.Max(0m, totalPending - capacityToMax);

        var rows = projects.Select(p =>
        {
            var capacity = p.TargetDate is DateOnly t ? CapacityTo(today, t, workdays, daily) : 0m;
            var individually = p.TargetDate.HasValue && p.PendingHours > capacity;
            return new ProjectRisk(p.ProjectId, p.PendingHours, p.TargetDate, capacity, individually, sharedConflict && p.TargetDate.HasValue);
        }).ToList();

        return new RiskResult(today, weeklyCapacityHours, maxDeadline, totalPending, capacityToMax, excess, sharedConflict, rows);
    }

    private static decimal CapacityTo(DateOnly today, DateOnly deadline, IReadOnlyDictionary<DayOfWeek, decimal> workdays, decimal daily)
    {
        if (deadline <= today) return 0m;
        var days = 0;
        for (var d = today; d <= deadline; d = d.AddDays(1))
            if (workdays.GetValueOrDefault(d.DayOfWeek) > 0) days++;
        return days * daily;
    }
}
