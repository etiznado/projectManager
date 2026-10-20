using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class ElapsedDurationTests
{
    private static readonly DateOnly Friday = new(2026,10,23);
    private static readonly Dictionary<DayOfWeek,decimal> Week = Enum.GetValues<DayOfWeek>().ToDictionary(x=>x,x=>x is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0m : 8m);
    private static ProjectActivity Activity(int id,decimal hours,DurationType type) => new()
    {
        Id=id,Name=$"Actividad {id}",RequestedStart=Friday,PlannedStart=Friday,PlannedFinish=Friday,PlannedDurationHours=hours,
        PlannedDurationType=type,EstimatedDurationType=type,EstimatedStart=Friday,EstimatedFinish=Friday,EstimatedDurationHours=hours,
        Project=new Project{WorkCalendar=new WorkCalendar{Days=Week.Select(x=>new WorkCalendarDay{DayOfWeek=x.Key,WorkingHours=x.Value}).ToList()}}
    };
    [Fact] public void TenNaturalDaysIncludeBothWeekends()
    {
        Assert.Equal(Friday.AddDays(10),WorkCalendarCalculator.Finish(Friday,240,Week,DurationType.ElapsedTime));
    }
    [Fact] public void WaitingCanStartOnSunday()
    {
        Assert.Equal(Friday.AddDays(3),WorkCalendarCalculator.Finish(Friday.AddDays(2),24,Week,DurationType.ElapsedTime));
    }
    [Fact] public void WaitingDoesNotRequireWorkingDays()
    {
        var result=WorkCalendarCalculator.Schedule(new(Friday,0),24,new Dictionary<DayOfWeek,decimal>(),DurationType.ElapsedTime);
        Assert.Equal(new WorkPoint(Friday.AddDays(1),0),result.Finish);
    }
    [Fact] public void ExactMidnightHasNoExtraDay()
    {
        var result=WorkCalendarCalculator.Schedule(new(Friday,0),48,Week,DurationType.ElapsedTime);
        Assert.Equal(new WorkPoint(Friday.AddDays(2),0),result.Finish);
    }
    [Fact] public void OrderOneHourThenTenDayWaitThenInspection()
    {
        var result=ScheduleCalculator.Calculate([Activity(1,1,DurationType.WorkingTime),Activity(2,240,DurationType.ElapsedTime),Activity(3,2,DurationType.WorkingTime)],
            [new(){PredecessorId=1,SuccessorId=2},new(){PredecessorId=2,SuccessorId=3}]);
        Assert.Equal(new WorkPoint(Friday,1),result[2].Start);
        Assert.Equal(new WorkPoint(Friday.AddDays(10),1),result[2].Finish);
        Assert.Equal(new WorkPoint(Friday.AddDays(10),1),result[3].Start);
        Assert.Equal(new WorkPoint(Friday.AddDays(10),3),result[3].Finish);
    }
    [Fact] public void WeekendArrivalWaitsForReceivingWorkCalendar()
    {
        var result=ScheduleCalculator.Calculate([Activity(1,24,DurationType.ElapsedTime),Activity(2,2,DurationType.WorkingTime)], [new(){PredecessorId=1,SuccessorId=2}]);
        Assert.Equal(Friday.AddDays(1),result[1].Finish.Date);
        Assert.Equal(new WorkPoint(Friday.AddDays(3),0),result[2].Start);
    }
    [Fact] public void WorkingDayEndDoesNotPauseContinuousWait()
    {
        var result=ScheduleCalculator.Calculate([Activity(1,8,DurationType.WorkingTime),Activity(2,48,DurationType.ElapsedTime)], [new(){PredecessorId=1,SuccessorId=2}]);
        Assert.Equal(new WorkPoint(Friday,8),result[2].Start);
        Assert.Equal(new WorkPoint(Friday.AddDays(2),8),result[2].Finish);
    }
    [Fact] public void DecimalDaysMapToCanonicalHours()
    {
        var input=new ActivityInput{DurationType=DurationType.ElapsedTime,DurationDays=1.25m};
        Assert.Equal(30,input.DurationHours);
        Assert.Contains("1.25",WorkCalendarCalculator.DurationText(input.DurationHours,input.DurationType));
    }
    [Fact] public void EditorChangesForecastTypeWithoutChangingBaselineType()
    {
        var activity=Activity(1,8,DurationType.WorkingTime);
        var input=new ActivityEditInput{Name="Espera",EstimatedStart=Friday,EstimatedDurationDays=10,EstimatedDurationType=DurationType.ElapsedTime};
        ActivityEditor.Apply(activity,input,Week);
        Assert.Equal(DurationType.WorkingTime,activity.PlannedDurationType);Assert.Equal(8,activity.PlannedDurationHours);
        Assert.Equal(DurationType.ElapsedTime,activity.EstimatedDurationType);Assert.Equal(240,activity.EstimatedDurationHours);
        Assert.Equal(Friday.AddDays(10),activity.EstimatedFinish);
    }
    [Fact] public void InvalidDurationTypeIsRejected()
    {
        Assert.Throws<ArgumentException>(()=>WorkCalendarCalculator.Schedule(new(Friday,0),8,Week,(DurationType)99));
    }
}
