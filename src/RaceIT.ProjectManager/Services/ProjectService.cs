using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class ProjectService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task<List<Initiative>> ListInitiativesAsync()
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Initiatives.AsNoTracking().Include(x => x.Projects).OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<Project>> ListProjectsAsync()
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Projects.AsNoTracking().Include(x => x.Initiative).Include(x => x.Person).OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<Initiative> CreateInitiativeAsync(InitiativeInput input)
    {
        await access.RequireAdminAsync();
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Captura el nombre.");
        await using var db = await factory.CreateDbContextAsync();
        var entity = new Initiative
        {
            Name = input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            TargetDate = input.TargetDate
        };
        db.Initiatives.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<Project> CreateProjectAsync(ProjectInput input)
    {
        await access.RequireAuthenticatedAsync();
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Captura el nombre.");
        var isAdmin = await access.IsAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        if (input.InitiativeId is int initiativeId && !await db.Initiatives.AnyAsync(x => x.Id == initiativeId))
            throw new ArgumentException("Selecciona una iniciativa válida.");
        int? personId = isAdmin
            ? input.PersonId
            : await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        if (personId is int pid && !await db.People.AnyAsync(x => x.Id == pid))
            throw new ArgumentException("Selecciona un responsable válido.");
        var entity = new Project
        {
            Name = input.Name.Trim(),
            InitiativeId = input.InitiativeId,
            PersonId = personId,
            TargetDate = input.TargetDate
        };
        db.Projects.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }
}
