using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class ScheduleCalculatorTests
{
    private static readonly DateOnly Friday = new(2026, 10, 23);
    private static ProjectActivity Activity(int id, decimal hours = 8, bool saturday = false, DateOnly? requested = null)
    {
        var date = requested ?? Friday;
        return new ProjectActivity
        {
            Id = id, Name = $"Actividad {id}", RequestedStart = date,
            PlannedStart = date, PlannedFinish = date, PlannedDurationHours = hours,
            EstimatedStart = date, EstimatedFinish = date, EstimatedDurationHours = hours,
            Project = new Project { Id = id, InitiativeId = 1, Name = $"Proyecto {id}", WorkCalendar = new WorkCalendar
            {
                Days = Enum.GetValues<DayOfWeek>().Select(day => new WorkCalendarDay
                {
                    DayOfWeek = day, WorkingHours = day == DayOfWeek.Sunday ? 0 : day == DayOfWeek.Saturday ? (saturday ? 5 : 0) : 8
                }).ToList()
            }}
        };
    }
    private static ActivityDependency Edge(int from, int to) => new() { PredecessorId = from, SuccessorId = to };

    [Fact] public void ParallelActivitiesKeepTheirRequestedStart()
    {
        var result = ScheduleCalculator.Calculate([Activity(1), Activity(2)], []);
        Assert.Equal(new WorkPoint(Friday, 0), result[1].Start);
        Assert.Equal(result[1], result[2]);
    }
    [Fact] public void ChainPreservesPartialWorkingDay()
    {
        var result = ScheduleCalculator.Calculate([Activity(1, 3), Activity(2, 4), Activity(3, 2)], [Edge(1, 2), Edge(2, 3)]);
        Assert.Equal(new WorkPoint(Friday, 3), result[2].Start);
        Assert.Equal(new WorkPoint(Friday, 7), result[3].Start);
        Assert.Equal(new WorkPoint(Friday.AddDays(3), 1), result[3].Finish);
    }
    [Fact] public void ExactDayFinishPushesSuccessorToMonday()
    {
        var result = ScheduleCalculator.Calculate([Activity(1), Activity(2)], [Edge(1, 2)]);
        Assert.Equal(new WorkPoint(Friday.AddDays(3), 0), result[2].Start);
    }
    [Fact] public void ReceivingProjectUsesItsOwnCalendar()
    {
        var result = ScheduleCalculator.Calculate([Activity(1, 13, true), Activity(2)], [Edge(1, 2)]);
        Assert.Equal(Friday.AddDays(1), result[1].Finish.Date);
        Assert.Equal(Friday.AddDays(3), result[2].Start.Date);
    }
    [Fact] public void JoinWaitsForLatestPredecessor()
    {
        var result = ScheduleCalculator.Calculate([Activity(1, 2), Activity(2, 10), Activity(3, 1)], [Edge(1, 3), Edge(2, 3)]);
        Assert.Equal(new WorkPoint(Friday.AddDays(3), 2), result[3].Start);
    }
    [Fact] public void RequestedStartRemainsLowerBound()
    {
        var result = ScheduleCalculator.Calculate([Activity(1, 1), Activity(2, requested: Friday.AddDays(4))], [Edge(1, 2)]);
        Assert.Equal(Friday.AddDays(4), result[2].Start.Date);
    }
    [Fact] public void RemovedDependencyAllowsEarlierStartAndDoesNotChangePlan()
    {
        var a = Activity(1, 40); var b = Activity(2);
        var delayed = ScheduleCalculator.Calculate([a, b], [Edge(1, 2)]);
        b.EstimatedStart = delayed[2].Start.Date;
        var earlier = ScheduleCalculator.Calculate([a, b], []);
        Assert.Equal(Friday, earlier[2].Start.Date);
        Assert.Equal(Friday, b.PlannedStart);
        Assert.Equal(Friday, b.PlannedFinish);
        Assert.Equal(8, b.PlannedDurationHours);
    }
    [Fact] public void CycleIsRejected()
    {
        Assert.Throws<ArgumentException>(() => ScheduleCalculator.Calculate([Activity(1), Activity(2), Activity(3)], [Edge(1, 2), Edge(2, 3), Edge(3, 1)]));
    }
    [Fact] public void SelfDependencyIsRejected()
    {
        Assert.Throws<ArgumentException>(() => ScheduleCalculator.Calculate([Activity(1)], [Edge(1, 1)]));
    }
    [Fact] public void ExternalActivityIsRejected()
    {
        Assert.Throws<ArgumentException>(() => ScheduleCalculator.Calculate([Activity(1)], [Edge(20, 1)]));
    }
    [Fact] public void CompletedActivityReleasesAfterActualFinishDate()
    {
        var a = Activity(1); a.Status = ActivityStatus.Completed; a.ActualStart = Friday; a.ActualFinish = Friday;
        var result = ScheduleCalculator.Calculate([a, Activity(2, 2, true)], [Edge(1, 2)]);
        Assert.Equal(new WorkPoint(Friday.AddDays(1), 0), result[2].Start);
    }
    [Fact] public void InProgressActivityKeepsActualStartEvenWithLatePredecessor()
    {
        var a = Activity(1, 80); var b = Activity(2);
        b.Status = ActivityStatus.InProgress; b.ActualStart = Friday;
        var result = ScheduleCalculator.Calculate([a, b], [Edge(1, 2)]);
        Assert.Equal(new WorkPoint(Friday, 0), result[2].Start);
    }
    [Fact] public void ShorterReceivingDayMovesReleaseToNextWorkingDay()
    {
        var a = Activity(1, 7); var b = Activity(2, 1);
        b.Project.WorkCalendar.Days.Single(x => x.DayOfWeek == DayOfWeek.Friday).WorkingHours = 5;
        var result = ScheduleCalculator.Calculate([a, b], [Edge(1, 2)]);
        Assert.Equal(new WorkPoint(Friday.AddDays(3), 0), result[2].Start);
    }
    [Fact] public void NonWorkingCalendarIsRejected()
    {
        var a = Activity(1);
        foreach (var day in a.Project.WorkCalendar.Days) day.WorkingHours = 0;
        Assert.Throws<ArgumentException>(() => ScheduleCalculator.Calculate([a], []));
    }
}
