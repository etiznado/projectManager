namespace RaceIT.ProjectManager.Domain;

public enum OpenFrontStatus { InProgress = 0, NeedsDecision = 1, Waiting = 2 }

public sealed class OpenFront
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public string Title { get; set; } = "";
    public string? Context { get; set; }
    public OpenFrontStatus Status { get; set; }
    public string LastAdvance { get; set; } = "";
    public string NextStep { get; set; } = "";
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public long Revision { get; set; }
}
