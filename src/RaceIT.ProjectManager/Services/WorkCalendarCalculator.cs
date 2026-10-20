using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public static class WorkCalendarCalculator
{
    public static string DurationText(decimal hours, DurationType type) => type == DurationType.ManualDates ? "Fechas manuales" : type == DurationType.ElapsedTime
        ? $"{hours / 24m:0.##} días naturales" : $"{hours:0.##} h laborables";

    public static DateOnly Finish(DateOnly start, decimal hours, IReadOnlyDictionary<DayOfWeek, decimal> days, DurationType type = DurationType.WorkingTime)
    {
        if (type == DurationType.WorkingTime && days.GetValueOrDefault(start.DayOfWeek) <= 0)
            throw new ArgumentException("El inicio debe ser un día laborable del proyecto.");
        return Schedule(new(start, 0), hours, days, type).Finish.Date;
    }

    public static (WorkPoint Start, WorkPoint Finish) Schedule(WorkPoint start, decimal hours,
        IReadOnlyDictionary<DayOfWeek, decimal> days, DurationType type = DurationType.WorkingTime)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentException("Selecciona un tipo de duración válido.");
        if (type == DurationType.ManualDates) throw new ArgumentException("Las fechas manuales requieren inicio y fin explícitos.");
        if (hours <= 0 || hours > 100000) throw new ArgumentException("La duración debe ser positiva y no superar 100000 horas (4166.66 días naturales).");
        var calendar = type == DurationType.ElapsedTime
            ? Enum.GetValues<DayOfWeek>().ToDictionary(x => x, _ => 24m) : days;
        if (!calendar.Values.Any(x => x > 0) || calendar.Values.Any(x => x < 0 || x > 24))
            throw new ArgumentException("El calendario no tiene una jornada válida.");
        WorkPoint Normalize(WorkPoint point)
        {
            while (calendar.GetValueOrDefault(point.Date.DayOfWeek) <= point.Hour)
                point = new(point.Date.AddDays(1), 0);
            return point;
        }
        start = Normalize(start);
        if (type == DurationType.ElapsedTime)
        {
            // Continuous duration: weekends count and exact midnight belongs to the next date.
            var total = start.Hour + hours;
            var fullDays = (int)decimal.Floor(total / 24m);
            return (start, new(start.Date.AddDays(fullDays), total % 24m));
        }
        var cursor = start;
        while (true)
        {
            var available = calendar.GetValueOrDefault(cursor.Date.DayOfWeek) - cursor.Hour;
            if (hours <= available) return (start, new(cursor.Date, cursor.Hour + hours));
            hours -= available;
            cursor = Normalize(new(cursor.Date.AddDays(1), 0));
        }
    }
}

