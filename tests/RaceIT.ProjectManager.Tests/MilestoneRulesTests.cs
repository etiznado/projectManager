using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class MilestoneRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);
    private static ProjectActivity Activity(int id, int days, bool complete = false) => new()
    {
        Id = id, EstimatedFinish = Today.AddDays(days), Status = complete ? ActivityStatus.Completed : ActivityStatus.Pending,
        ActualFinish = complete ? Today.AddDays(days) : null
    };
    private static Milestone Milestone(params ProjectActivity[] activities) => new()
    {
        Requirements = activities.Select(x => new MilestoneRequirement { ActivityId = x.Id, Activity = x }).ToList()
    };
    [Fact] public void EstimateUsesLatestRequiredFinish()
    { Assert.Equal(Today.AddDays(5), MilestoneRules.Estimate([Activity(1, 2), Activity(2, 5)])); }
    [Fact] public void CompletedActivityUsesActualInsteadOfOldForecast()
    {
        var activity = Activity(1, -1, true); activity.EstimatedFinish = Today.AddDays(20);
        Assert.Equal(Today.AddDays(-1), MilestoneRules.Estimate([activity]));
    }
    [Fact] public void EmptyRequirementsAreRejected()
    { Assert.Throws<ArgumentException>(() => MilestoneRules.Estimate([])); }
    [Fact] public void PendingRequirementPreventsConfirmation()
    { Assert.Throws<ArgumentException>(() => MilestoneRules.ValidateConfirmation(Milestone(Activity(1, 1)), Today, Today)); }
    [Fact] public void FutureConfirmationIsRejected()
    { Assert.Throws<ArgumentException>(() => MilestoneRules.ValidateConfirmation(Milestone(Activity(1, -1, true)), Today.AddDays(1), Today)); }
    [Fact] public void ConfirmationBeforeActualFinishIsRejected()
    { Assert.Throws<ArgumentException>(() => MilestoneRules.ValidateConfirmation(Milestone(Activity(1, 0, true)), Today.AddDays(-1), Today)); }
    [Fact] public void CompletedRequirementsAllowExplicitConfirmation()
    { MilestoneRules.ValidateConfirmation(Milestone(Activity(1, -2, true), Activity(2, 0, true)), Today, Today); }
    [Fact] public void RefreshDoesNotAutomaticallyConfirmCompletedRequirements()
    {
        var milestone = Milestone(Activity(1, -1, true)); MilestoneRules.Refresh(milestone);
        Assert.Null(milestone.ActualDate);
    }
    [Fact] public void ReopenedActivityInvalidatesMilestoneApproval()
    {
        var milestone = Milestone(Activity(1, -1, true)); milestone.ActualDate = Today;
        milestone.Requirements.First().ApprovedDate = Today; milestone.Requirements.First().ApprovedById = 9;
        milestone.Requirements.First().Activity.Status = ActivityStatus.InProgress;
        MilestoneRules.Refresh(milestone);
        Assert.Null(milestone.ActualDate); Assert.Null(milestone.Requirements.First().ApprovedDate); Assert.Null(milestone.Requirements.First().ApprovedById);
    }
    [Fact] public void LaterActualFinishInvalidatesOldApproval()
    {
        var milestone = Milestone(Activity(1, 0, true)); milestone.ActualDate = Today.AddDays(-1);
        MilestoneRules.Refresh(milestone); Assert.Null(milestone.ActualDate);
    }
    [Fact] public void ValidConfirmationIsPreservedWhenRefreshingForecast()
    {
        var milestone = Milestone(Activity(1, -1, true)); milestone.ActualDate = Today;
        MilestoneRules.Refresh(milestone); Assert.Equal(Today, milestone.ActualDate);
    }
}
