using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class BlockerService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task SaveAsync(int projectId, int? blockerId, BlockerInput input)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
            throw new ArgumentException(string.Join(" ", errors.Select(x => x.ErrorMessage)));
        if (string.IsNullOrWhiteSpace(input.Description)) throw new ArgumentException("Describe qué impide avanzar.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var activity = await db.Activities.SingleOrDefaultAsync(x => x.Id == input.ActivityId && x.ProjectId == projectId)
            ?? throw new ArgumentException("Selecciona una actividad del proyecto actual.");
        await access.RequireActivityAsync(activity.ResponsibleId);
        if (!await access.IsAdminAsync() && !string.IsNullOrWhiteSpace(input.ResponsibleName)) {
            var ownName = await db.Responsibles.Where(x => x.Id == activity.ResponsibleId).Select(x => x.Name).SingleAsync();
            if (input.ResponsibleName.Trim() != ownName) throw new ArgumentException("Solo puedes asignarte el bloqueo a ti mismo.");
        }
        BlockerRules.RequireUnfinished(activity);
        var blocker = blockerId.HasValue
            ? await db.Blockers.SingleOrDefaultAsync(x => x.Id == blockerId && x.ActivityId == input.ActivityId)
                ?? throw new ArgumentException("El bloqueo no existe en esta actividad.")
            : new Blocker { ActivityId = activity.Id, CreatedAt = DateTimeOffset.UtcNow };
        if (blocker.ResolvedAt.HasValue) throw new ArgumentException("Reabre el bloqueo antes de editarlo.");
        blocker.Description = input.Description.Trim();
        var name = input.ResponsibleName?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { blocker.ResponsibleId = null; blocker.Responsible = null; }
        else if (!await access.IsAdminAsync()) { blocker.Responsible = null; blocker.ResponsibleId = activity.ResponsibleId; }
        else blocker.Responsible = await db.Responsibles.FirstOrDefaultAsync(x => x.Name == name) ?? new Responsible { Name = name };
        if (!blockerId.HasValue) db.Blockers.Add(blocker);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task ResolveAsync(int projectId, int id) => await ChangeState(projectId, id, false);
    public async Task ReopenAsync(int projectId, int id) => await ChangeState(projectId, id, true);
    private async Task ChangeState(int projectId, int id, bool reopen)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var blocker = await db.Blockers.Include(x => x.Activity).SingleOrDefaultAsync(x => x.Id == id && x.Activity.ProjectId == projectId)
            ?? throw new ArgumentException("El bloqueo no existe en el proyecto actual.");
        await access.RequireActivityAsync(blocker.Activity.ResponsibleId);
        if (reopen) BlockerRules.Reopen(blocker);
        else BlockerRules.Resolve(blocker, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
