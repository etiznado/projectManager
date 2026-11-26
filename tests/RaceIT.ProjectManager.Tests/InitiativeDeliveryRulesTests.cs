using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class InitiativeDeliveryRulesTests
{
    [Theory]
    [InlineData("2027-01-20", "2027-01-23", 3)]
    [InlineData("2027-01-22", "2027-01-25", 3)]
    [InlineData("2027-01-20", "2027-01-20", null)]
    [InlineData("2027-01-20", "2027-01-19", null)]
    [InlineData(null, "2027-01-23", null)]
    [InlineData("2027-01-20", null, null)]
    [InlineData(null, null, null)]
    public void Delay_is_positive_calendar_days_only(string? agreed, string? estimated, int? days) =>
        Assert.Equal(days, InitiativeDeliveryRules.DelayDays(Parse(agreed), Parse(estimated)));

    [Theory]
    [InlineData(null, null, null, DeliveryState.Undated)]
    [InlineData(null, null, "2027-01-23", DeliveryState.Undated)]
    [InlineData(null, "2027-01-20", "2027-01-20", DeliveryState.OnTime)]
    [InlineData(null, "2027-01-20", null, DeliveryState.OnTime)]
    [InlineData(null, "2027-01-20", "2027-01-23", DeliveryState.AtRisk)]
    [InlineData("2027-01-21", null, "2027-01-23", DeliveryState.AtRisk)]
    [InlineData("2027-01-19", "2027-01-25", "2027-01-26", DeliveryState.Overdue)]
    [InlineData(null, "2027-01-19", "2027-01-23", DeliveryState.Overdue)]
    public void Traffic_light_uses_commitments_with_overdue_precedence(string? initiative, string? agreed, string? estimated, DeliveryState state) =>
        Assert.Equal(state, InitiativeDeliveryRules.Evaluate(new DateOnly(2027, 1, 20), Parse(initiative), [(Parse(agreed), Parse(estimated))]));

    [Fact]
    public void Traffic_light_is_one_state_not_sum_of_project_delays()
    {
        Assert.Equal(DeliveryState.AtRisk, InitiativeDeliveryRules.Evaluate(new DateOnly(2027, 1, 1), null,
            [(new DateOnly(2027, 1, 20), new DateOnly(2027, 1, 23)), (new DateOnly(2027, 1, 22), new DateOnly(2027, 1, 25))]));
        Assert.Equal("En riesgo", InitiativeDeliveryRules.Label(DeliveryState.AtRisk));
    }

    [Fact]
    public void Active_requires_unfinished_and_started_work_without_completing_by_consumption()
    {
        Assert.False(InitiativeDeliveryRules.IsInProgress([], false));
        Assert.False(InitiativeDeliveryRules.IsInProgress([ActivityStatus.Pending], false));
        Assert.False(InitiativeDeliveryRules.IsInProgress([ActivityStatus.Completed], true));
        Assert.True(InitiativeDeliveryRules.IsInProgress([ActivityStatus.InProgress], false));
        Assert.True(InitiativeDeliveryRules.IsInProgress([ActivityStatus.Completed, ActivityStatus.Pending], false));
        Assert.True(InitiativeDeliveryRules.IsInProgress([ActivityStatus.Pending], true));
    }

    private static DateOnly? Parse(string? value) => value is null ? null : DateOnly.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
