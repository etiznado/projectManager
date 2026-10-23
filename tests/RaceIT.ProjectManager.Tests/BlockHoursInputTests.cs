using System.ComponentModel.DataAnnotations;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class BlockHoursInputTests
{
    private static IReadOnlyList<ValidationResult> Validate(BlockHoursInput input) =>
        input.Validate(new ValidationContext(input)).ToList();

    [Fact]
    public void Accepts_hours_within_pending_limit()
    {
        var input = new BlockHoursInput { Hours = 2m, MaxHours = 3.79m };
        Assert.Empty(Validate(input));
    }

    [Fact]
    public void Accepts_hours_equal_to_pending_limit()
    {
        var input = new BlockHoursInput { Hours = 3.79m, MaxHours = 3.79m };
        Assert.Empty(Validate(input));
    }

    [Fact]
    public void Rejects_hours_above_pending_limit()
    {
        var input = new BlockHoursInput { Hours = 4m, MaxHours = 3.79m };
        var results = Validate(input);
        Assert.Single(results);
        Assert.Contains("pendientes", results[0].ErrorMessage);
    }

    [Fact]
    public void Rejects_any_hours_when_there_is_no_pending()
    {
        var input = new BlockHoursInput { Hours = 1m, MaxHours = 0m };
        Assert.Single(Validate(input));
    }
}
