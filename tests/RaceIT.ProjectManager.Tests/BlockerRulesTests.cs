using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class BlockerRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    private static ProjectActivity Activity() => new() { Status = ActivityStatus.Pending, PlannedFinish = Today.AddDays(2), EstimatedFinish = Today.AddDays(2) };
    [Fact] public void BlockerMarksUnfinishedOnTimeActivityAsAtRisk()
    { Assert.Equal("En riesgo", BlockerRules.Health(Activity(), true, Today)); }
    [Fact] public void NoBlockerKeepsActivityOnTime()
    { Assert.Equal("En tiempo", BlockerRules.Health(Activity(), false, Today)); }
    [Fact] public void DelayedForecastTakesPriorityOverRisk()
    {
        var activity = Activity(); activity.EstimatedFinish = Today.AddDays(3);
        Assert.Equal("Atrasada", BlockerRules.Health(activity, true, Today));
    }
    [Fact] public void UnfinishedActivityWithExpiredForecastIsDelayed()
    {
        var activity = Activity(); activity.EstimatedFinish = Today.AddDays(-1);
        Assert.Equal("Atrasada", BlockerRules.Health(activity, false, Today));
    }
    [Fact] public void CompletedActivityCannotHaveNewBlocker()
    {
        var activity = Activity(); activity.Status = ActivityStatus.Completed;
        Assert.Throws<ArgumentException>(() => BlockerRules.RequireUnfinished(activity));
    }
    [Fact] public void ResolveRecordsTimestampAndPreservesCreation()
    {
        var blocker = new Blocker { CreatedAt = Now.AddDays(-1) };
        BlockerRules.Resolve(blocker, Now);
        Assert.Equal(Now, blocker.ResolvedAt); Assert.Equal(Now.AddDays(-1), blocker.CreatedAt);
    }
    [Fact] public void ResolvingAgainPreservesOriginalResolution()
    {
        var blocker = new Blocker { CreatedAt = Now.AddDays(-1), ResolvedAt = Now };
        BlockerRules.Resolve(blocker, Now.AddHours(1)); Assert.Equal(Now, blocker.ResolvedAt);
    }
    [Fact] public void ResolutionBeforeCreationIsRejected()
    { Assert.Throws<ArgumentException>(() => BlockerRules.Resolve(new Blocker { CreatedAt = Now }, Now.AddHours(-1))); }
    [Fact] public void ReopenClearsResolutionWithoutChangingActivityDates()
    {
        var activity = Activity(); var blocker = new Blocker { Activity = activity, CreatedAt = Now.AddDays(-1), ResolvedAt = Now };
        BlockerRules.Reopen(blocker);
        Assert.Null(blocker.ResolvedAt); Assert.Equal(Today.AddDays(2), activity.EstimatedFinish);
    }
    [Fact] public void ReopeningIsRejectedAfterActivityCompletion()
    {
        var activity = Activity(); activity.Status = ActivityStatus.Completed;
        Assert.Throws<ArgumentException>(() => BlockerRules.Reopen(new Blocker { Activity = activity }));
    }
}
