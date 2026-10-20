namespace RaceIT.ProjectManager.Domain;

public enum ActivityStatus { Pending, InProgress, Completed }
public enum DurationType { WorkingTime, ElapsedTime, ManualDates }

public sealed class Responsible
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class WorkCalendar
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsFlexible { get; set; }
    public ICollection<WorkCalendarDay> Days { get; set; } = new List<WorkCalendarDay>();
}

public sealed class WorkCalendarDay
{
    public int WorkCalendarId { get; set; }
    public WorkCalendar WorkCalendar { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public decimal WorkingHours { get; set; }
}

public sealed class Initiative
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly TargetDate { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public sealed class Project
{
    public int Id { get; set; }
    public int InitiativeId { get; set; }
    public Initiative Initiative { get; set; } = null!;
    public string Name { get; set; } = "";
    public int WorkCalendarId { get; set; }
    public WorkCalendar WorkCalendar { get; set; } = null!;
    public int? ResponsibleId { get; set; }
    public Responsible? Responsible { get; set; }
    public ICollection<ProjectActivity> Activities { get; set; } = new List<ProjectActivity>();
}

public sealed class ProjectActivity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Name { get; set; } = "";
    public int? ResponsibleId { get; set; }
    public Responsible? Responsible { get; set; }
    public ActivityStatus Status { get; set; }
    public bool IsPrincipal { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DurationType PlannedDurationType { get; set; }
    public DurationType EstimatedDurationType { get; set; }
    public decimal PlannedDurationHours { get; set; }
    public decimal EstimatedDurationHours { get; set; }
    public DateOnly PlannedStart { get; set; }
    public DateOnly PlannedFinish { get; set; }
    public DateOnly RequestedStart { get; set; }
    public DateOnly? RequestedFinish { get; set; }
    public decimal EstimatedStartHour { get; set; }
    public decimal EstimatedFinishHour { get; set; }
    public DateOnly EstimatedStart { get; set; }
    public DateOnly EstimatedFinish { get; set; }
    public DateOnly? ActualStart { get; set; }
    public DateOnly? ActualFinish { get; set; }
}

// Finish-to-start dependencies; predecessor may belong to another project.
public sealed class ActivityDependency
{
    public int PredecessorId { get; set; }
    public ProjectActivity Predecessor { get; set; } = null!;
    public int SuccessorId { get; set; }
    public ProjectActivity Successor { get; set; } = null!;
}

public sealed class Milestone
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Name { get; set; } = "";
    public DateOnly TargetDate { get; set; }
    public DateOnly EstimatedDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public ICollection<MilestoneRequirement> Requirements { get; set; } = new List<MilestoneRequirement>();
}

public sealed class MilestoneRequirement
{
    public int MilestoneId { get; set; }
    public Milestone Milestone { get; set; } = null!;
    public int ActivityId { get; set; }
    public ProjectActivity Activity { get; set; } = null!;
    public DateOnly? ApprovedDate { get; set; }
    public int? ApprovedById { get; set; }
    public Responsible? ApprovedBy { get; set; }
}

public sealed class Blocker
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public ProjectActivity Activity { get; set; } = null!;
    public string Description { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
    public int? ResponsibleId { get; set; }
    public Responsible? Responsible { get; set; }
}



