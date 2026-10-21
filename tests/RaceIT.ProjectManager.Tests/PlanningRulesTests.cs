using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class PlanningRulesTests
{
    [Fact]
    public void RemainingHours_subtracts_recorded_from_estimate()
    {
        Assert.Equal(6m, PlanningRules.RemainingHours(11, 5));
    }

    [Theory]
    [InlineData(ActivityStatus.Pending, 0, true)]
    [InlineData(ActivityStatus.Pending, -2, true)]
    [InlineData(ActivityStatus.Pending, 1, false)]
    [InlineData(ActivityStatus.InProgress, 0, true)]
    [InlineData(ActivityStatus.Completed, 0, false)]
    [InlineData(ActivityStatus.Completed, -3, false)]
    public void NeedsReestimate_flags_open_work_when_exhausted(ActivityStatus status, decimal remaining, bool expected)
    {
        Assert.Equal(expected, PlanningRules.NeedsReestimate(status, remaining));
    }

    [Fact]
    public void HasCycle_detects_cycle()
    {
        var ids = new[] { 1, 2, 3 };
        var edges = new[] { (1, 2), (2, 3), (3, 1) };
        Assert.True(PlanningRules.HasCycle(ids, edges));
    }

    [Fact]
    public void HasCycle_returns_false_for_acyclic()
    {
        var ids = new[] { 1, 2, 3 };
        var edges = new[] { (1, 2), (2, 3) };
        Assert.False(PlanningRules.HasCycle(ids, edges));
    }

    [Fact]
    public void HasCycle_self_reference_is_a_cycle()
    {
        Assert.True(PlanningRules.HasCycle(new[] { 1 }, new[] { (1, 1) }));
    }

    [Fact]
    public void HasCycle_rejects_edges_outside_scope()
    {
        Assert.Throws<ArgumentException>(() => PlanningRules.HasCycle(new[] { 1 }, new[] { (1, 2) }));
    }

    [Fact]
    public void SessionHours_computes_duration()
    {
        var start = new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 10, 20, 12, 30, 0, TimeSpan.Zero);
        Assert.Equal(3.5m, PlanningRules.SessionHours(start, end));
    }

    [Fact]
    public void SessionHours_rejects_end_before_start()
    {
        var start = new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
        Assert.Throws<ArgumentException>(() => PlanningRules.SessionHours(start, end));
    }
}
