using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class TemplateService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task<List<Template>> ListAsync()
    {
        await access.RequireAuthenticatedAsync();
        await using var db = await factory.CreateDbContextAsync();
        await EnsureDefaultsAsync(db);
        return await db.Templates.AsNoTracking()
            .Include(x => x.Items.OrderBy(i => i.Order).ThenBy(i => i.Id))
            .OrderBy(x => x.Category).ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<Template> CreateAsync(TemplateInput input)
    {
        await access.RequireAdminAsync();
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Captura el nombre.");
        if (input.Items.Count == 0 || input.Items.Any(x => string.IsNullOrWhiteSpace(x.Title) || x.EstimatedHours <= 0))
            throw new ArgumentException("La plantilla requiere al menos un elemento válido.");
        await using var db = await factory.CreateDbContextAsync();
        var template = new Template { Name = input.Name.Trim(), Category = input.Category };
        var order = 1;
        foreach (var item in input.Items)
            template.Items.Add(new TemplateItem { Title = item.Title.Trim(), EstimatedHours = item.EstimatedHours, Order = order++ });
        db.Templates.Add(template);
        await db.SaveChangesAsync();
        return template;
    }

    public async Task DeleteAsync(int id)
    {
        await access.RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        var template = await db.Templates.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new ArgumentException("La plantilla no existe.");
        db.TemplateItems.RemoveRange(template.Items);
        db.Templates.Remove(template);
        await db.SaveChangesAsync();
    }

    public async Task<List<Activity>> GenerateAsync(int templateId, string raceName, int year)
    {
        await access.RequireAuthenticatedAsync();
        var personId = await access.PersonIdAsync() ?? throw new ArgumentException("Selecciona un usuario válido.");
        if (string.IsNullOrWhiteSpace(raceName)) throw new ArgumentException("Captura el nombre de la carrera.");
        await using var db = await factory.CreateDbContextAsync();
        var template = await db.Templates.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == templateId)
            ?? throw new ArgumentException("Selecciona una plantilla válida.");
        var items = template.Items.OrderBy(x => x.Order).ThenBy(x => x.Id).ToList();
        if (items.Count == 0) throw new ArgumentException("La plantilla no tiene actividades.");
        var activities = items.Select(item => new Activity
        {
            Title = TemplateRules.GenerateTitle(item.Title, raceName, year),
            PersonId = personId,
            ProjectId = null,
            CurrentEstimateHours = item.EstimatedHours,
            Status = ActivityStatus.Pending
        }).ToList();
        db.Activities.AddRange(activities);
        await db.SaveChangesAsync();
        return activities;
    }

    private static async Task EnsureDefaultsAsync(ProjectManagerDbContext db)
    {
        if (await db.Templates.AnyAsync()) return;
        db.Templates.Add(new Template
        {
            Name = "Antes de carrera", Category = TemplateCategory.Before,
            Items = new List<TemplateItem>
            {
                new() { Title = "Revisar loggers", EstimatedHours = 2, Order = 1 },
                new() { Title = "Cargar equipo", EstimatedHours = 3, Order = 2 },
                new() { Title = "Revisar radios", EstimatedHours = 1, Order = 3 }
            }
        });
        db.Templates.Add(new Template
        {
            Name = "Durante carrera", Category = TemplateCategory.During,
            Items = new List<TemplateItem>
            {
                new() { Title = "Preparar auto", EstimatedHours = 4, Order = 1 },
                new() { Title = "Registrar datos", EstimatedHours = 6, Order = 2 }
            }
        });
        db.Templates.Add(new Template
        {
            Name = "Después de carrera", Category = TemplateCategory.After,
            Items = new List<TemplateItem>
            {
                new() { Title = "Descargar datos", EstimatedHours = 2, Order = 1 },
                new() { Title = "Limpiar equipo", EstimatedHours = 3, Order = 2 },
                new() { Title = "Reporte de carrera", EstimatedHours = 4, Order = 3 }
            }
        });
        await db.SaveChangesAsync();
    }
}
