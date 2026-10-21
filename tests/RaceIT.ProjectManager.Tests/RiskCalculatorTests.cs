using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class RiskCalculatorTests
{
    private static readonly Dictionary<DayOfWeek, decimal> EveryDay1h = new()
    {
        [DayOfWeek.Monday] = 1, [DayOfWeek.Tuesday] = 1, [DayOfWeek.Wednesday] = 1,
        [DayOfWeek.Thursday] = 1, [DayOfWeek.Friday] = 1, [DayOfWeek.Saturday] = 1, [DayOfWeek.Sunday] = 1
    };

    [Fact]
    public void Flags_shared_conflict_and_individual_risk()
    {
        var today = new DateOnly(2026, 10, 20);
        var projects = new (int, decimal, DateOnly?)[]
        {
            (1, 6m, new DateOnly(2026, 10, 27)),
            (2, 30m, new DateOnly(2026, 11, 3))
        };
        var result = RiskCalculator.Evaluate(today, 7m, EveryDay1h, projects);
        Assert.True(result.SharedConflict);
        Assert.Equal(36m, result.TotalPending);
        Assert.Equal(15m, result.CapacityToMax);
        Assert.Equal(21m, result.ExcessHours);
        Assert.Equal(2, result.Projects.Count);
        Assert.True(result.Projects[0].SharedConflict);
        Assert.False(result.Projects[0].IndividuallyAtRisk);
        Assert.True(result.Projects[1].SharedConflict);
        Assert.True(result.Projects[1].IndividuallyAtRisk);
    }

    [Fact]
    public void No_conflict_when_capacity_is_enough()
    {
        var today = new DateOnly(2026, 10, 20);
        var projects = new (int, decimal, DateOnly?)[] { (1, 5m, new DateOnly(2026, 10, 27)) };
        var result = RiskCalculator.Evaluate(today, 7m, EveryDay1h, projects);
        Assert.False(result.SharedConflict);
        Assert.Equal(0m, result.ExcessHours);
        Assert.False(result.Projects[0].IndividuallyAtRisk);
    }

    [Fact]
    public void Projects_without_deadline_are_not_flagged()
    {
        var today = new DateOnly(2026, 10, 20);
        var projects = new (int, decimal, DateOnly?)[] { (1, 100m, null) };
        var result = RiskCalculator.Evaluate(today, 7m, EveryDay1h, projects);
        Assert.False(result.SharedConflict);
        Assert.Null(result.MaxDeadline);
        Assert.Equal(0m, result.TotalPending);
        Assert.False(result.Projects[0].IndividuallyAtRisk);
        Assert.False(result.Projects[0].SharedConflict);
    }
}
