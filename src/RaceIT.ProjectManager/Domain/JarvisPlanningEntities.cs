namespace RaceIT.ProjectManager.Domain;

// API metadata only. Existing domain rows remain the source of planning data.
public sealed class JarvisPlanningState
{
    public string EntityKind { get; set; } = "";
    public int EntityId { get; set; }
    public int PersonId { get; set; }
    public string? CreatedInPlanId { get; set; }
    public long Revision { get; set; }
    public bool IsClosed { get; set; }
    public string LastAdvance { get; set; } = "";
}

public sealed class JarvisPlanningChange
{
    public long Id { get; set; }
    public string EntityKind { get; set; } = "";
    public int EntityId { get; set; }
    public int PersonId { get; set; }
    public string PlanId { get; set; } = "";
    public string PlanApprovalReference { get; set; } = "";
    public long Revision { get; set; }
    public string Operation { get; set; } = "";
    public string ConnectorId { get; set; } = "";
    public DateTimeOffset AtUtc { get; set; }
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public string? ApprovalId { get; set; }
    public string? ApprovalReference { get; set; }
}
