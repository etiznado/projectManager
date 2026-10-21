using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class WeekLayoutCalculatorTests
{
    private static readonly Dictionary<DayOfWeek, decimal> MonSat75 = new()
    {
        [DayOfWeek.Monday] = 7.5m, [DayOfWeek.Tuesday] = 7.5m, [DayOfWeek.Wednesday] = 7.5m,
        [DayOfWeek.Thursday] = 7.5m, [DayOfWeek.Friday] = 7.5m, [DayOfWeek.Saturday] = 7.5m,
        [DayOfWeek.Sunday] = 0m
    };

    [Fact]
    public void Places_blocks_consecutively()
    {
        var weekStart = new DateOnly(2026, 10, 19); // lunes
        var blocks = new (int, int, string, string?, decimal)[]
        {
            (1, 10, "A", null, 15m),
            (2, 20, "B", null, 15m),
            (3, 30, "C", null, 10m)
        };
        var layout = WeekLayoutCalculator.Calculate(weekStart, 45, MonSat75, blocks);
        Assert.Equal(40m, layout.PlannedHours);
        Assert.Equal(5m, layout.FreeHours);
        Assert.Equal(3, layout.Blocks.Count);
        Assert.Equal(0m, layout.Blocks[0].StartOffset);
        Assert.Equal(15m, layout.Blocks[0].EndOffset);
        Assert.Equal(15m, layout.Blocks[1].StartOffset);
        Assert.Equal(30m, layout.Blocks[1].EndOffset);
        Assert.Equal(30m, layout.Blocks[2].StartOffset);
        Assert.Equal(40m, layout.Blocks[2].EndOffset);
    }

    [Fact]
    public void Empty_plan_has_full_free_hours()
    {
        var layout = WeekLayoutCalculator.Calculate(new DateOnly(2026, 10, 19), 45, MonSat75, []);
        Assert.Equal(0m, layout.PlannedHours);
        Assert.Equal(45m, layout.FreeHours);
    }

    [Fact]
    public void Days_have_cumulative_starts()
    {
        var layout = WeekLayoutCalculator.Calculate(new DateOnly(2026, 10, 19), 45, MonSat75, []);
        Assert.Equal(7, layout.Days.Count);
        Assert.Equal(0m, layout.Days[0].CumulativeStart);
        Assert.Equal(7.5m, layout.Days[1].CumulativeStart);
        Assert.Equal(37.5m, layout.Days[5].CumulativeStart);
        Assert.Equal(45m, layout.Days[6].CumulativeStart);
    }
}
