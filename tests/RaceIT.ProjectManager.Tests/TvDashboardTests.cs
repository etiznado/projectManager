using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class TvDashboardTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);
    private static TvDashboard Dashboard(params ProjectActivity[] activities)
    {
        var project = new Project { Id = 1, Name = "PCB" };
        foreach (var activity in activities) { activity.ProjectId = 1; activity.Project = project; }
        return new(new Initiative { Id = 1, Name = "Data Logger", TargetDate = Today.AddDays(10) }, [project], activities, [], [], [], DateTimeOffset.UtcNow);
    }
    private static ProjectActivity Activity(int id, int finishDays = 2) => new()
    {
        Id = id, Name = $"Actividad {id}", IsPrincipal = true,
        PlannedStart = Today, PlannedFinish = Today.AddDays(2), EstimatedStart = Today, EstimatedFinish = Today.AddDays(finishDays)
    };
    [Fact] public void EmptyDashboardHasNoFabricatedForecast()
    { var data = Dashboard(); Assert.Null(data.Forecast); Assert.Equal("SIN ACTIVIDADES", data.Health(Today)); }
    [Fact] public void HiddenActivityStillDeterminesDelivery()
    {
        var hidden = Activity(2, 20); hidden.IsPrincipal = false;
        var data = Dashboard(Activity(1), hidden);
        Assert.Equal(Today.AddDays(20), data.Forecast); Assert.Single(data.Rows);
        Assert.Equal("ATRASADO", data.Health(Today));
    }
    [Fact] public void OpenBlockerMarksRiskWithoutInventingDelay()
    {
        var activity = Activity(1); var data = Dashboard(activity);
        data = data with { Blockers = [new Blocker { Activity = activity, ActivityId = 1 }] };
        Assert.Equal("EN RIESGO", data.Health(Today)); Assert.Equal(Today.AddDays(2), data.Forecast);
    }
    [Fact] public void DelayHasPriorityOverOpenBlocker()
    {
        var activity = Activity(1, 20); var data = Dashboard(activity);
        data = data with { Blockers = [new Blocker { Activity = activity, ActivityId = 1 }] };
        Assert.Equal("ATRASADO", data.Health(Today));
    }
    [Fact] public void MilestoneAcceptanceIsIncludedInDelivery()
    {
        var data = Dashboard(Activity(1)) with { Milestones = [new Milestone { EstimatedDate = Today.AddDays(5), ActualDate = Today.AddDays(7) }] };
        Assert.Equal(Today.AddDays(7), data.Forecast);
    }
    [Fact] public void CompletedInitiativeUsesActualFinishAndRequiresMilestoneConfirmation()
    {
        var activity = Activity(1, 20); activity.Status = ActivityStatus.Completed; activity.ActualFinish = Today;
        var data = Dashboard(activity);
        Assert.Equal(Today, data.Forecast); Assert.True(data.Complete); Assert.Equal("COMPLETADA", data.Health(Today));
        data = data with { Milestones = [new Milestone { EstimatedDate = Today }] };
        Assert.False(data.Complete);
    }
    [Fact] public void PaginationNeverLosesOrDuplicatesVisibleRows()
    {
        var data = Dashboard(Enumerable.Range(1, 19).Select(x => Activity(x)).ToArray());
        Assert.Equal(3, TvTimeline.PageCount(data));
        var ids = Enumerable.Range(0, 3).SelectMany(x => TvTimeline.Page(data, x)).Select(x => x.Activity!.Id).ToList();
        Assert.Equal(19, ids.Count); Assert.Equal(19, ids.Distinct().Count());
    }
    [Fact] public void ProjectWithNoVisibleActivitiesGetsSummaryRow()
    {
        var activity = Activity(1); activity.IsPrincipal = false; var data = Dashboard(activity);
        Assert.Single(data.Rows); Assert.Null(data.Rows[0].Activity);
    }
    [Fact] public void RangeIncludesTodayPlanTargetAndForecast()
    {
        var activity = Activity(1, 80); activity.PlannedStart = Today.AddDays(-80); var data = Dashboard(activity);
        var range = TvTimeline.Range(data, Today);
        Assert.True(range.Start <= activity.PlannedStart); Assert.True(range.End > activity.EstimatedFinish);
        Assert.Equal(DayOfWeek.Monday, range.Start.DayOfWeek);
    }
    [Fact] public void OverdueUnfinishedForecastNeedsAttentionEvenBeforeInitiativeDeadline()
    {
        var data = Dashboard(Activity(1, -1)); Assert.Equal("EN RIESGO", data.Health(Today));
    }
    [Fact] public void DelayColorHasPriorityOverInProgress()
    {
        var activity = Activity(1, 3); activity.Status = ActivityStatus.InProgress;
        Assert.Equal("#ff747b", TvTimeline.Color(activity, false, Today));
    }
}
