using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed record WeekDayInfo(DayOfWeek DayOfWeek, DateOnly Date, decimal WorkingHours, decimal CumulativeStart);
public sealed record WeekBlockInfo(int BlockId, int ActivityId, string Title, string? ProjectName, decimal PlannedHours, decimal StartOffset, decimal EndOffset);

// Tramo de un bloque dentro de un carril de día: fracciones dentro del día y horas del bloque que cubre.
public sealed record WeekBarSegment(int DayIndex, decimal FromFraction, decimal ToFraction, decimal FromHours, decimal ToHours);

public sealed record WeekLayout(decimal WeeklyCapacityHours, decimal PlannedHours, IReadOnlyList<WeekDayInfo> Days, IReadOnlyList<WeekBlockInfo> Blocks)
{
    public decimal FreeHours => WeeklyCapacityHours - PlannedHours;

    // Días con horas de jornada: cada uno recibe un carril de igual ancho en el timeline.
    public IReadOnlyList<WeekDayInfo> WorkingDays => WeekLayoutCalculator.WorkingDays(Days);

    // Horas que caben en los carriles; lo planeado por encima queda fuera de jornada.
    public decimal TrackHours => WorkingDays.Sum(d => d.WorkingHours);

    public decimal OverflowHours => Math.Max(0m, PlannedHours - TrackHours);

    // Posición 0-100 de una hora del eje lineal sobre la pista de carriles uniformes.
    public decimal PositionPercent(decimal hourOffset) => WeekLayoutCalculator.TrackPercent(WorkingDays, hourOffset);

    // Reparte un bloque entre los carriles de los días que toca.
    public IReadOnlyList<WeekBarSegment> Segments(decimal startHour, decimal hours) =>
        WeekLayoutCalculator.SplitIntoDays(WorkingDays, startHour, hours);
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

    // Carriles: solo los días con jornada mayor a cero, en orden cronológico.
    public static IReadOnlyList<WeekDayInfo> WorkingDays(IReadOnlyList<WeekDayInfo> days) =>
        days.Where(d => d.WorkingHours > 0m).ToList();

    // Posición 0-100 de una hora del eje lineal sobre la pista de carriles uniformes.
    // Un día sin jornada no tiene carril: su hora cae en la frontera con el día siguiente.
    public static decimal TrackPercent(IReadOnlyList<WeekDayInfo> workingDays, decimal hourOffset)
    {
        if (workingDays.Count == 0) return 0m;
        var trackHours = workingDays.Sum(d => d.WorkingHours);
        var offset = Math.Clamp(hourOffset, 0m, trackHours);
        if (offset >= trackHours) return 100m;
        for (var i = workingDays.Count - 1; i >= 0; i--)
        {
            var day = workingDays[i];
            if (offset < day.CumulativeStart) continue;
            var fraction = Math.Clamp((offset - day.CumulativeStart) / day.WorkingHours, 0m, 1m);
            return (i + fraction) * 100m / workingDays.Count;
        }
        return 0m;
    }

    // Divide el bloque en tramos, uno por carril tocado, recortado a las horas que caben en la jornada.
    // Las horas de cada tramo son relativas al inicio del bloque.
    public static IReadOnlyList<WeekBarSegment> SplitIntoDays(IReadOnlyList<WeekDayInfo> workingDays, decimal startHour, decimal hours)
    {
        var segments = new List<WeekBarSegment>();
        if (workingDays.Count == 0 || hours <= 0m) return segments;
        var trackHours = workingDays.Sum(d => d.WorkingHours);
        var start = Math.Clamp(startHour, 0m, trackHours);
        var end = Math.Clamp(startHour + hours, 0m, trackHours);
        if (end <= start) return segments;
        for (var i = 0; i < workingDays.Count; i++)
        {
            var day = workingDays[i];
            var dayStart = day.CumulativeStart;
            var dayEnd = dayStart + day.WorkingHours;
            var from = Math.Max(start, dayStart);
            var to = Math.Min(end, dayEnd);
            if (to <= from) continue;
            segments.Add(new WeekBarSegment(
                i,
                (from - dayStart) / day.WorkingHours,
                (to - dayStart) / day.WorkingHours,
                from - start,
                to - start));
        }
        return segments;
    }

    // Relleno verde de consumo repartido de izquierda a derecha a lo largo del bloque.
    public static decimal SegmentFillFraction(decimal progressHours, WeekBarSegment segment)
    {
        var span = segment.ToHours - segment.FromHours;
        if (span <= 0m) return 0m;
        return Math.Clamp((progressHours - segment.FromHours) / span, 0m, 1m);
    }
}
