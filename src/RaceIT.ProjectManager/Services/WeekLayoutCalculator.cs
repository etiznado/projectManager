using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed record WeekDayInfo(DayOfWeek DayOfWeek, DateOnly Date, decimal WorkingHours, decimal CumulativeStart);
public sealed record WeekBlockInfo(int BlockId, int ActivityId, string Title, string? ProjectName, decimal PlannedHours, decimal StartOffset, decimal EndOffset);
public sealed record WeekLayout(decimal WeeklyCapacityHours, decimal PlannedHours, IReadOnlyList<WeekDayInfo> Days, IReadOnlyList<WeekBlockInfo> Blocks)
{
    public decimal FreeHours => WeeklyCapacityHours - PlannedHours;
    public decimal AxisTotal
    {
        get
        {
            var dayEnd = Days.Count == 0 ? 0m : Days[^1].CumulativeStart + Days[^1].WorkingHours;
            return Math.Max(WeeklyCapacityHours, Math.Max(PlannedHours, dayEnd));
        }
    }
}

// Regla pura: coloca los bloques consecutivamente sobre el eje de horas laborables de la semana.
public static class WeekLayoutCalculator
{
    public static WeekLayout Calculate(
        DateOnly weekStart,
        decimal weeklyCapacityHours,
        IReadOnlyDictionary<DayOfWeek, decimal> workdays,
        IReadOnlyList<(int BlockId, int ActivityId, string Title, string? ProjectName, decimal PlannedHours)> blocks)
    {
        var days = new List<WeekDayInfo>();
        var acc = 0m;
        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            var hours = workdays.GetValueOrDefault(date.DayOfWeek);
            days.Add(new WeekDayInfo(date.DayOfWeek, date, hours, acc));
            acc += hours;
        }
        var result = new List<WeekBlockInfo>();
        var cursor = 0m;
        foreach (var block in blocks)
        {
            var start = cursor;
            var end = cursor + block.PlannedHours;
            result.Add(new WeekBlockInfo(block.BlockId, block.ActivityId, block.Title, block.ProjectName, block.PlannedHours, start, end));
            cursor = end;
        }
        return new WeekLayout(weeklyCapacityHours, cursor, days, result);
    }
}
