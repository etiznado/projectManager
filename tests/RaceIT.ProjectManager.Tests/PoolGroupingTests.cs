using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class PoolGroupingTests
{
    private static ActivityService.ActivityRow Row(int id, string title, int? projectId, string? projectName)
    {
        var activity = new Activity
        {
            Id = id,
            Title = title,
            ProjectId = projectId,
            CurrentEstimateHours = 8m,
            Project = projectId is null ? null : new Project { Id = projectId.Value, Name = projectName ?? "" }
        };
        return new ActivityService.ActivityRow(activity, 0m);
    }

    [Fact]
    public void Empty_pool_has_no_groups() => Assert.Empty(PoolGrouping.Group([]));

    [Fact]
    public void Groups_activities_by_project()
    {
        var groups = PoolGrouping.Group([Row(1, "A", 10, "PCB"), Row(2, "B", 20, "Cajones"), Row(3, "C", 10, "PCB")]);
        Assert.Equal(2, groups.Count);
        var pcb = groups.Single(g => g.ProjectId == 10);
        Assert.Equal("PCB", pcb.Name);
        Assert.Equal(2, pcb.Count);
        Assert.Equal(1, groups.Single(g => g.ProjectId == 20).Count);
    }

    [Fact]
    public void No_project_group_comes_first()
    {
        var groups = PoolGrouping.Group([Row(1, "A", 10, "PCB"), Row(2, "Suelta", null, null)]);
        Assert.Null(groups[0].ProjectId);
        Assert.Equal(PoolGrouping.NoProjectName, groups[0].Name);
        Assert.Equal(1, groups[0].Count);
    }

    [Fact]
    public void Groups_are_sorted_alphabetically()
    {
        var groups = PoolGrouping.Group([Row(1, "A", 10, "Zeta"), Row(2, "B", 20, "alfa"), Row(3, "C", 30, "Beta")]);
        Assert.Equal(new[] { "alfa", "Beta", "Zeta" }, groups.Select(g => g.Name).ToArray());
    }

    [Fact]
    public void Keeps_creation_order_inside_a_group()
    {
        var groups = PoolGrouping.Group([Row(1, "Primera", 10, "PCB"), Row(2, "Segunda", 10, "PCB"), Row(3, "Tercera", 10, "PCB")]);
        Assert.Equal(new[] { "Primera", "Segunda", "Tercera" }, groups[0].Rows.Select(r => r.Activity.Title).ToArray());
    }

    [Fact]
    public void Activities_of_different_projects_do_not_merge()
    {
        var groups = PoolGrouping.Group([Row(1, "A", 10, "PCB"), Row(2, "B", 11, "PCB")]);
        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(1, g.Count));
    }
}
