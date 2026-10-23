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
        IQueryable<Project> query = db.Projects.AsNoTracking().Include(x => x.Initiative).Include(x => x.Person);
        if (!await access.IsAdminAsync())
        {
            var personId = await access.PersonIdAsync() ?? throw new UnauthorizedAccessException("Inicia sesión para continuar.");
            query = query.Where(x => x.PersonId == personId);
        }
        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<Project>> ListOwnProjectsAsync()
    {
        await access.RequireAuthenticatedAsync();
        var personId = await access.PersonIdAsync() ?? throw new UnauthorizedAccessException("Inicia sesión para continuar.");
        await using var db = await factory.CreateDbContextAsync();
        return await db.Projects.AsNoTracking()
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.Name)
            .ToListAsync();
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

    public async Task<Initiative> UpdateInitiativeAsync(int initiativeId, InitiativeInput input)
    {
        await access.RequireAdminAsync();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true) || string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException(errors.Count > 0 ? string.Join(" ", errors.Select(x => x.ErrorMessage)) : "Captura el nombre.");
        await using var db = await factory.CreateDbContextAsync();
        var initiative = await db.Initiatives.SingleOrDefaultAsync(x => x.Id == initiativeId)
            ?? throw new ArgumentException("La iniciativa no existe.");
        initiative.Name = input.Name.Trim();
        initiative.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        initiative.TargetDate = input.TargetDate;
        await db.SaveChangesAsync();
        return initiative;
    }

    public async Task<Project> CreateProjectAsync(ProjectInput input)
    {
        await access.RequireAuthenticatedAsync();
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Captura el nombre.");
        var isAdmin = await access.IsAdminAsync();
        if (!isAdmin && input.InitiativeId is not null)
            throw new UnauthorizedAccessException("Solo el administrador puede asociar proyectos a iniciativas.");
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

    public async Task<Project> UpdateProjectAsync(int projectId, ProjectInput input)
    {
        await access.RequireAuthenticatedAsync();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true) || string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException(errors.Count > 0 ? string.Join(" ", errors.Select(x => x.ErrorMessage)) : "Captura el nombre.");

        await using var db = await factory.CreateDbContextAsync();
        var project = await db.Projects.SingleOrDefaultAsync(x => x.Id == projectId)
            ?? throw new ArgumentException("El proyecto no existe.");
        var isAdmin = await access.IsAdminAsync();
        var currentPersonId = await access.PersonIdAsync();
        if (!WorkAccess.CanEditProject(isAdmin, currentPersonId, project.PersonId))
            throw new UnauthorizedAccessException("El proyecto no está asignado a ti.");
        if (!isAdmin)
        {
            if (input.InitiativeId != project.InitiativeId || input.PersonId != project.PersonId)
                throw new UnauthorizedAccessException("Solo el administrador puede cambiar la iniciativa o el responsable.");
        }
        else
        {
            if (input.InitiativeId is int initiativeId && !await db.Initiatives.AnyAsync(x => x.Id == initiativeId))
                throw new ArgumentException("Selecciona una iniciativa válida.");
            if (input.PersonId is int personId && !await db.People.AnyAsync(x => x.Id == personId))
                throw new ArgumentException("Selecciona un responsable válido.");
            project.InitiativeId = input.InitiativeId;
            project.PersonId = input.PersonId;
        }
        project.Name = input.Name.Trim();
        project.TargetDate = input.TargetDate;
        await db.SaveChangesAsync();
        return project;
    }
}
