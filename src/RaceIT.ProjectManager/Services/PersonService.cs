using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed class PersonService(IDbContextFactory<ProjectManagerDbContext> factory, WorkAccess access)
{
    public async Task<List<Person>> ListActiveAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.People.AsNoTracking().Include(x => x.Workdays).Where(x => x.IsActive).OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync();
    }

    // Sin autorización: la página de login es anónima.
    public async Task<List<Person>> ListForLoginAsync() => await ListActiveAsync();

    public async Task<Person> CreateAsync(PersonInput input)
    {
        await access.RequireAdminAsync();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true) || string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException(string.Join(" ", errors.Select(x => x.ErrorMessage)));
        await using var db = await factory.CreateDbContextAsync();
        var person = new Person
        {
            Name = input.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim(),
            Role = input.Role,
            PlanningMode = input.PlanningMode,
            WeeklyCapacityHours = input.WeeklyCapacityHours,
            IsActive = true,
            Workdays = input.Workdays.Select(x => new PersonWorkday { DayOfWeek = x.DayOfWeek, WorkingHours = x.WorkingHours }).ToList()
        };
        db.People.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    public async Task<Person> UpdateAsync(int personId, PersonInput input)
    {
        await access.RequireAdminAsync();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true) || string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException(errors.Count > 0 ? string.Join(" ", errors.Select(x => x.ErrorMessage)) : "Captura el nombre.");

        await using var db = await factory.CreateDbContextAsync();
        var person = await db.People.Include(x => x.Workdays).SingleOrDefaultAsync(x => x.Id == personId)
            ?? throw new ArgumentException("La persona no existe.");
        if (person.Id == await access.PersonIdAsync() && person.Role != input.Role)
            throw new ArgumentException("No puedes cambiar tu propio rol durante esta sesión.");

        foreach (var day in input.Workdays)
        {
            var existing = person.Workdays.SingleOrDefault(x => x.DayOfWeek == day.DayOfWeek);
            if (existing is null)
                person.Workdays.Add(new PersonWorkday { DayOfWeek = day.DayOfWeek, WorkingHours = day.WorkingHours });
            else
                existing.WorkingHours = day.WorkingHours;
        }
        person.Name = input.Name.Trim();
        person.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        person.Role = input.Role;
        person.PlanningMode = input.PlanningMode;
        person.WeeklyCapacityHours = input.WeeklyCapacityHours;
        await db.SaveChangesAsync();
        return person;
    }

    internal static List<PersonWorkday> DefaultWorkdays(decimal weeklyCapacityHours)
    {
        var daily = weeklyCapacityHours / 6m;
        var days = new List<PersonWorkday>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            days.Add(new PersonWorkday { DayOfWeek = day, WorkingHours = day == DayOfWeek.Sunday ? 0 : weeklyCapacityHours == 45m ? (day == DayOfWeek.Saturday ? 5m : 8m) : daily });
        return days;
    }
}
