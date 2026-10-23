using System.ComponentModel.DataAnnotations;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class PersonWorkdayTests
{
    [Fact]
    public void Default_jornada_is_eight_hours_weekdays_and_five_saturday()
    {
        var input = new PersonInput { Name = "Persona" };

        Assert.Equal(45m, input.Workdays.Sum(x => x.WorkingHours));
        Assert.All(input.Workdays.Where(x => x.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday),
            x => Assert.Equal(8m, x.WorkingHours));
        Assert.Equal(5m, input.Workdays.Single(x => x.DayOfWeek == DayOfWeek.Saturday).WorkingHours);
        Assert.Equal(0m, input.Workdays.Single(x => x.DayOfWeek == DayOfWeek.Sunday).WorkingHours);
    }

    [Fact]
    public void Weekly_capacity_can_differ_from_unchanged_daily_hours()
    {
        var input = new PersonInput { Name = "Persona", WeeklyCapacityHours = 36m };

        Assert.Equal(45m, input.Workdays.Sum(x => x.WorkingHours));
        Assert.True(IsValid(input));
    }

    [Theory]
    [InlineData(-0.25)]
    [InlineData(24.25)]
    [InlineData(1.234)]
    public void Daily_hours_must_be_within_range_and_two_decimals(decimal hours)
    {
        var input = new PersonInput { Name = "Persona" };
        input.Workdays[0].WorkingHours = hours;

        Assert.False(IsValid(input));
    }

    [Fact]
    public void Jornada_requires_each_day_once()
    {
        var input = new PersonInput { Name = "Persona" };
        input.Workdays[0].DayOfWeek = DayOfWeek.Tuesday;

        Assert.False(IsValid(input));
    }

    private static bool IsValid(PersonInput input) =>
        Validator.TryValidateObject(input, new ValidationContext(input), [], true);
}
