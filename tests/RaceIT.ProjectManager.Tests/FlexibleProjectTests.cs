using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class FlexibleProjectTests
{
    private static readonly DateOnly Saturday = new(2026, 10, 17);
    private static readonly Dictionary<DayOfWeek,decimal> Week = Enum.GetValues<DayOfWeek>().ToDictionary(x=>x,x=>x is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0m : 8m);
    private static ProjectActivity Manual(int id,DateOnly? start=null,DateOnly? finish=null) => new()
    {
        Id=id,RequestedStart=start??Saturday,RequestedFinish=finish??Saturday,EstimatedStart=start??Saturday,EstimatedFinish=finish??Saturday,
        EstimatedDurationType=DurationType.ManualDates,EstimatedDurationHours=0,
        Project=new Project{WorkCalendar=new WorkCalendar{IsFlexible=true}}
    };
    [Fact] public void FlexibleSaturdayRequiresNoWorkingHours()
    {
        var result=ScheduleCalculator.Calculate([Manual(1)],[])[1];
        Assert.Equal(Saturday,result.Start.Date);Assert.Equal(Saturday,result.Finish.Date);Assert.Equal(24,result.Finish.Hour);
    }
    [Fact] public void DependenciesShiftManualDatesPreservingTheirSpan()
    {
        var first=Manual(1,Saturday,Saturday.AddDays(2));var second=Manual(2,Saturday,Saturday.AddDays(1));
        var result=ScheduleCalculator.Calculate([first,second],[new(){PredecessorId=1,SuccessorId=2}]);
        Assert.Equal(Saturday.AddDays(3),result[2].Start.Date);Assert.Equal(Saturday.AddDays(4),result[2].Finish.Date);
    }
    [Fact] public void RemovingLinkReturnsToRequestedDates()
    {
        var first=Manual(1,Saturday,Saturday.AddDays(5));var second=Manual(2);
        var late=ScheduleCalculator.Calculate([first,second],[new(){PredecessorId=1,SuccessorId=2}]);second.EstimatedStart=late[2].Start.Date;
        Assert.Equal(Saturday,ScheduleCalculator.Calculate([first,second],[])[2].Start.Date);
    }
    [Fact] public void ConvertingProjectPreservesBaselineAndRealDates()
    {
        var item=Manual(1);item.EstimatedDurationType=DurationType.WorkingTime;item.EstimatedDurationHours=16;item.RequestedStart=Saturday.AddDays(2);
        item.PlannedDurationType=DurationType.WorkingTime;item.PlannedDurationHours=16;item.PlannedStart=item.RequestedStart;item.PlannedFinish=item.RequestedStart.AddDays(1);item.ActualStart=item.RequestedStart;
        ProjectRules.ConvertToFlexible([item],Week);
        Assert.Equal(DurationType.ManualDates,item.EstimatedDurationType);Assert.Equal(0,item.EstimatedDurationHours);Assert.Equal(item.RequestedStart.AddDays(1),item.RequestedFinish);
        Assert.Equal(16,item.PlannedDurationHours);Assert.Equal(DurationType.WorkingTime,item.PlannedDurationType);Assert.Equal(item.RequestedStart,item.ActualStart);
    }
    [Fact] public void ConvertingDoesNotChangeProviderWaiting()
    {
        var item=Manual(1);item.EstimatedDurationType=DurationType.ElapsedTime;item.EstimatedDurationHours=240;
        ProjectRules.ConvertToFlexible([item],Week);Assert.Equal(DurationType.ElapsedTime,item.EstimatedDurationType);Assert.Equal(240,item.EstimatedDurationHours);
    }
    [Fact] public void EditorAcceptsActualSaturdayWithoutCalendarHours()
    {
        var item=Manual(1);var input=new ActivityEditInput{Name="Enviar orden",EstimatedStart=Saturday,RequestedFinish=Saturday,EstimatedDurationType=DurationType.ManualDates,Status=ActivityStatus.Completed,ActualStart=Saturday,ActualFinish=Saturday};
        ActivityEditor.Apply(item,input,new Dictionary<DayOfWeek,decimal>());Assert.Equal(Saturday,item.ActualFinish);Assert.Equal(0,item.EstimatedDurationHours);
    }
    [Fact] public void ManualFinishBeforeStartIsRejected()
    {
        var input=new ActivityEditInput{Name="Orden",EstimatedStart=Saturday,RequestedFinish=Saturday.AddDays(-1),EstimatedDurationType=DurationType.ManualDates};
        Assert.Throws<ArgumentException>(()=>ActivityEditor.Apply(Manual(1),input,Week));
    }
    [Fact] public void ManualMissingFinishIsRejectedByScheduler()
    {
        var item=Manual(1);item.RequestedFinish=null;Assert.Throws<ArgumentException>(()=>ScheduleCalculator.Calculate([item],[]));
    }
    [Fact] public void FlexibleProjectCannotInventWorkingCapacity()
    {
        var item=Manual(1);item.EstimatedDurationType=DurationType.WorkingTime;item.EstimatedDurationHours=8;
        Assert.Throws<ArgumentException>(()=>ScheduleCalculator.Calculate([item],[]));
    }
    [Fact] public void WaitingRemainsContinuousInsideFlexibleProject()
    {
        var first=Manual(1);var wait=Manual(2);wait.EstimatedDurationType=DurationType.ElapsedTime;wait.EstimatedDurationHours=240;
        var result=ScheduleCalculator.Calculate([first,wait],[new(){PredecessorId=1,SuccessorId=2}]);
        Assert.Equal(Saturday.AddDays(1),result[2].Start.Date);Assert.Equal(Saturday.AddDays(11),result[2].Finish.Date);
    }
}
