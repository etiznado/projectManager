namespace RaceIT.ProjectManager.Domain;

/// <summary>A successful field mutation, saved atomically with its front.</summary>
public sealed class OpenFrontChange
{
    public long Id { get; set; }
    public int OpenFrontId { get; set; }
    public OpenFront OpenFront { get; set; } = null!;
    public int PersonId { get; set; }
    public string ActorKind { get; set; } = "";
    public string ActorId { get; set; } = "";
    public string Field { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public DateTimeOffset AtUtc { get; set; }
    public long Revision { get; set; }
}
