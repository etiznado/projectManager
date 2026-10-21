namespace RaceIT.ProjectManager.Domain;

public enum PersonRole { User, Admin }
public enum PlanningMode { Weekly, ByDeliverables }
public enum ActivityStatus { Pending, InProgress, Completed }
public enum WorkRecordKind { Session, Manual }
public enum TemplateCategory { Before, During, After }

public sealed class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public PersonRole Role { get; set; }
    public PlanningMode PlanningMode { get; set; }
    public decimal WeeklyCapacityHours { get; set; } = 45;
    public bool IsActive { get; set; } = true;
    public ICollection<PersonWorkday> Workdays { get; set; } = new List<PersonWorkday>();
}

public sealed class PersonWorkday
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public decimal WorkingHours { get; set; }
}

public sealed class Initiative
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly? TargetDate { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public sealed class Project
{
    public int Id { get; set; }
    public int? InitiativeId { get; set; }
    public Initiative? Initiative { get; set; }
    public string Name { get; set; } = "";
    public DateOnly? TargetDate { get; set; }
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

public sealed class Activity
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
    public string Title { get; set; } = "";
    public decimal CurrentEstimateHours { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ActivityDependency> Incoming { get; set; } = new List<ActivityDependency>();
    public ICollection<ActivityDependency> Outgoing { get; set; } = new List<ActivityDependency>();
    public ICollection<Continuation> ContinuationsFrom { get; set; } = new List<Continuation>();
    public ICollection<Continuation> ContinuationsTo { get; set; } = new List<Continuation>();
}

// Finish-to-start dependency; a predecessor may belong to another project.
public sealed class ActivityDependency
{
    public int PredecessorId { get; set; }
    public Activity Predecessor { get; set; } = null!;
    public int SuccessorId { get; set; }
    public Activity Successor { get; set; } = null!;
}

public sealed class WeeklyPlan
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public DateOnly WeekStart { get; set; }
    public ICollection<PlanBlock> Blocks { get; set; } = new List<PlanBlock>();
}

public sealed class PlanBlock
{
    public int Id { get; set; }
    public int WeeklyPlanId { get; set; }
    public WeeklyPlan WeeklyPlan { get; set; } = null!;
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public int Position { get; set; }
    public decimal PlannedHours { get; set; }
}

public sealed class WorkRecord
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public WorkRecordKind Kind { get; set; }
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
    public decimal Hours { get; set; }
    public string? Note { get; set; }
}

public sealed class Template
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TemplateCategory Category { get; set; }
    public ICollection<TemplateItem> Items { get; set; } = new List<TemplateItem>();
}

public sealed class TemplateItem
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public Template Template { get; set; } = null!;
    public string Title { get; set; } = "";
    public decimal EstimatedHours { get; set; }
    public int Order { get; set; }
}

// Vínculo de continuación manual entre una actividad anterior y su continuación.
public sealed class Continuation
{
    public int Id { get; set; }
    public int FromActivityId { get; set; }
    public Activity FromActivity { get; set; } = null!;
    public int ToActivityId { get; set; }
    public Activity ToActivity { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
