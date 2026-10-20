using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

// Hour is an offset from a common day origin. Work uses the project window; elapsed time uses all 24 hours.
public readonly record struct WorkPoint(DateOnly Date, decimal Hour) : IComparable<WorkPoint>
{
    public int CompareTo(WorkPoint other) => Date != other.Date ? Date.CompareTo(other.Date) : Hour.CompareTo(other.Hour);
}
public sealed record Forecast(WorkPoint Start, WorkPoint Finish, WorkPoint Release);

public static class ScheduleCalculator
{
    public static IReadOnlyDictionary<int, Forecast> Calculate(IReadOnlyCollection<ProjectActivity> activities,
        IReadOnlyCollection<ActivityDependency> dependencies)
    {
        var nodes = activities.ToDictionary(x => x.Id);
        var incoming = nodes.Keys.ToDictionary(x => x, _ => new List<int>());
        var outgoing = nodes.Keys.ToDictionary(x => x, _ => new List<int>());
        foreach (var edge in dependencies)
        {
            if (edge.PredecessorId == edge.SuccessorId) throw new ArgumentException("Una actividad no puede depender de sí misma.");
            if (!nodes.ContainsKey(edge.PredecessorId) || !nodes.ContainsKey(edge.SuccessorId))
                throw new ArgumentException("Las dependencias deben pertenecer a la misma iniciativa.");
            if (incoming[edge.SuccessorId].Contains(edge.PredecessorId)) throw new ArgumentException("La dependencia está repetida.");
            incoming[edge.SuccessorId].Add(edge.PredecessorId);
            outgoing[edge.PredecessorId].Add(edge.SuccessorId);
        }
        var degree = incoming.ToDictionary(x => x.Key, x => x.Value.Count);
        var ready = new Queue<int>(degree.Where(x => x.Value == 0).Select(x => x.Key));
        var result = new Dictionary<int, Forecast>();
        while (ready.TryDequeue(out var id))
        {
            var activity = nodes[id];
            var days = activity.Project.WorkCalendar.Days.ToDictionary(x => x.DayOfWeek, x => x.WorkingHours);

            if (activity.Status == ActivityStatus.Completed)
            {
                if (activity.ActualFinish is null) throw new ArgumentException("Una actividad completada requiere fin real.");
                // Only the actual date is known: conservatively release the following day.
                result[id] = new(new(activity.EstimatedStart, activity.EstimatedStartHour),
                    new(activity.EstimatedFinish, activity.EstimatedFinishHour), new(activity.ActualFinish.Value, 24));
            }
            else
            {
                var start = new WorkPoint(activity.Status == ActivityStatus.InProgress
                    ? activity.ActualStart ?? throw new ArgumentException("Una actividad en curso requiere inicio real.")
                    : activity.RequestedStart, 0);
                if (activity.Status == ActivityStatus.Pending)
                    foreach (var predecessor in incoming[id])
                        if (result[predecessor].Release.CompareTo(start) > 0) start = result[predecessor].Release;
                // Actual starts are historical facts and are never pushed by dependencies.
                if (activity.Project.WorkCalendar.IsFlexible && activity.EstimatedDurationType == DurationType.WorkingTime)
                    throw new ArgumentException("El proyecto es flexible. Usa fechas manuales o días naturales.");
                var schedule = activity.EstimatedDurationType == DurationType.ManualDates
                    ? ManualDateSchedule(activity, start)
                    : WorkCalendarCalculator.Schedule(start, activity.EstimatedDurationHours, days, activity.EstimatedDurationType);
                start = schedule.Start;
                var finish = schedule.Finish;
                result[id] = new(start, finish, finish);
            }
            foreach (var successor in outgoing[id]) if (--degree[successor] == 0) ready.Enqueue(successor);
        }
        if (result.Count != nodes.Count) throw new ArgumentException("La dependencia genera un ciclo. Una actividad no puede depender de sus sucesoras.");
        return result;
    }

    private static (WorkPoint Start, WorkPoint Finish) ManualDateSchedule(ProjectActivity activity, WorkPoint start)
    {
        if (activity.RequestedFinish is not DateOnly finish || finish < activity.RequestedStart)
            throw new ArgumentException("Las actividades con fechas manuales requieren un fin igual o posterior al inicio solicitado.");
        if (start.Hour >= 24) start = new(start.Date.AddDays(1), 0);
        var span = finish.DayNumber - activity.RequestedStart.DayNumber;
        return (start, new(start.Date.AddDays(span), 24));
    }
}


