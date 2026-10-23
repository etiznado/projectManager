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

    // Jornada mixta del caso reportado: 2/2/2/2/0/5/5 = 18 h con bolsa de 45 h.
    private static readonly Dictionary<DayOfWeek, decimal> MixedWorkdays = new()
    {
        [DayOfWeek.Monday] = 2m, [DayOfWeek.Tuesday] = 2m, [DayOfWeek.Wednesday] = 2m,
        [DayOfWeek.Thursday] = 2m, [DayOfWeek.Friday] = 0m, [DayOfWeek.Saturday] = 5m,
        [DayOfWeek.Sunday] = 5m
    };

    private static WeekLayout MixedLayout(params (int, int, string, string?, decimal)[] blocks) =>
        WeekLayoutCalculator.Calculate(new DateOnly(2026, 10, 19), 45m, MixedWorkdays, blocks);

    [Fact]
    public void Zero_hour_days_get_no_lane()
    {
        var layout = MixedLayout();
        Assert.Equal(7, layout.Days.Count);
        Assert.Equal(6, layout.WorkingDays.Count);
        Assert.Equal(18m, layout.TrackHours);
        Assert.DoesNotContain(layout.WorkingDays, d => d.DayOfWeek == DayOfWeek.Friday);
    }

    [Fact]
    public void Working_days_get_equal_widths()
    {
        var layout = MixedLayout();
        var days = layout.WorkingDays;
        for (var i = 0; i < days.Count; i++)
            Assert.Equal(i * 100m / 6m, layout.PositionPercent(days[i].CumulativeStart));
        Assert.Equal(100m, layout.PositionPercent(18m));
    }

    [Fact]
    public void Block_inside_one_day_fills_its_lane()
    {
        var layout = MixedLayout();
        var segment = Assert.Single(layout.Segments(0m, 2m));
        Assert.Equal(0, segment.DayIndex);
        Assert.Equal(0m, segment.FromFraction);
        Assert.Equal(1m, segment.ToFraction);
        Assert.Equal(0m, segment.FromHours);
        Assert.Equal(2m, segment.ToHours);
    }

    [Fact]
    public void Block_spanning_days_is_split_into_segments()
    {
        var layout = MixedLayout();
        var segments = layout.Segments(0m, 8m);
        Assert.Equal(4, segments.Count);
        Assert.Equal(new[] { 0, 1, 2, 3 }, segments.Select(s => s.DayIndex).ToArray());
        Assert.Equal(new[] { 0m, 2m, 4m, 6m }, segments.Select(s => s.FromHours).ToArray());
        Assert.Equal(8m, segments[^1].ToHours);
        Assert.Equal(1m, segments[^1].ToFraction);
    }

    [Fact]
    public void Same_hours_get_different_widths()
    {
        var layout = MixedLayout();
        var monday = Assert.Single(layout.Segments(0m, 2m));   // día de 2 h
        var saturday = Assert.Single(layout.Segments(8m, 2m)); // día de 5 h
        Assert.Equal(1m, monday.ToFraction - monday.FromFraction);
        Assert.Equal(0.4m, saturday.ToFraction - saturday.FromFraction);
        Assert.Equal(2.5m, (monday.ToFraction - monday.FromFraction) / (saturday.ToFraction - saturday.FromFraction));
    }

    [Fact]
    public void Zero_hour_day_maps_to_the_lane_boundary()
    {
        var layout = MixedLayout();
        // El viernes (0 h) no tiene carril: su hora cae en la frontera entre jueves y sábado.
        Assert.Equal(4 * 100m / 6m, layout.PositionPercent(8m));
    }

    [Fact]
    public void Overflow_beyond_workdays_is_clamped_and_reported()
    {
        var layout = MixedLayout((1, 10, "A", null, 20m));
        Assert.Equal(20m, layout.PlannedHours);
        Assert.Equal(2m, layout.OverflowHours);
        var segments = layout.Segments(0m, 20m);
        Assert.Equal(6, segments.Count);
        Assert.Equal(18m, segments[^1].ToHours);
        Assert.Equal(1m, segments[^1].ToFraction);
    }

    [Fact]
    public void Block_fully_beyond_workdays_has_no_segment()
    {
        var layout = MixedLayout((1, 10, "A", null, 10m), (2, 20, "B", null, 10m), (3, 30, "C", null, 10m));
        Assert.Equal(30m, layout.PlannedHours);
        Assert.Equal(12m, layout.OverflowHours);
        Assert.Empty(layout.Segments(20m, 10m));
    }

    [Fact]
    public void Segment_fill_fraction_distributes_progress()
    {
        var layout = MixedLayout();
        var segments = layout.Segments(0m, 8m);
        Assert.Equal(1m, WeekLayoutCalculator.SegmentFillFraction(3m, segments[0]));
        Assert.Equal(0.5m, WeekLayoutCalculator.SegmentFillFraction(3m, segments[1]));
        Assert.Equal(0m, WeekLayoutCalculator.SegmentFillFraction(3m, segments[2]));
        Assert.Equal(0m, WeekLayoutCalculator.SegmentFillFraction(3m, segments[3]));
    }

    [Fact]
    public void Full_week_jornada_tiles_the_whole_track()
    {
        var workdays = new Dictionary<DayOfWeek, decimal>
        {
            [DayOfWeek.Monday] = 8m, [DayOfWeek.Tuesday] = 8m, [DayOfWeek.Wednesday] = 8m,
            [DayOfWeek.Thursday] = 8m, [DayOfWeek.Friday] = 8m, [DayOfWeek.Saturday] = 5m,
            [DayOfWeek.Sunday] = 0m
        };
        var layout = WeekLayoutCalculator.Calculate(new DateOnly(2026, 10, 19), 45m, workdays, []);
        // El domingo de 0 h no tiene carril: la semana estándar ocupa 6 carriles y 45 h.
        Assert.Equal(6, layout.WorkingDays.Count);
        Assert.Equal(45m, layout.TrackHours);
        Assert.Equal(0m, layout.OverflowHours);
        Assert.Equal(100m, layout.PositionPercent(45m));
    }

}
