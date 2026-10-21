using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Data;

public sealed class ProjectManagerDbContext(DbContextOptions<ProjectManagerDbContext> options)
    : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<PersonWorkday> PersonWorkdays => Set<PersonWorkday>();
    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityDependency> ActivityDependencies => Set<ActivityDependency>();
    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();
    public DbSet<PlanBlock> PlanBlocks => Set<PlanBlock>();
    public DbSet<WorkRecord> WorkRecords => Set<WorkRecord>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateItem> TemplateItems => Set<TemplateItem>();
    public DbSet<Continuation> Continuations => Set<Continuation>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<PersonWorkday>().HasKey(x => new { x.PersonId, x.DayOfWeek });
        model.Entity<ActivityDependency>().HasKey(x => new { x.PredecessorId, x.SuccessorId });
        model.Entity<ActivityDependency>()
            .HasOne(x => x.Predecessor).WithMany(a => a.Outgoing).HasForeignKey(x => x.PredecessorId);
        model.Entity<ActivityDependency>()
            .HasOne(x => x.Successor).WithMany(a => a.Incoming).HasForeignKey(x => x.SuccessorId);

        model.Entity<Continuation>()
            .HasOne(x => x.FromActivity).WithMany(a => a.ContinuationsFrom).HasForeignKey(x => x.FromActivityId);
        model.Entity<Continuation>()
            .HasOne(x => x.ToActivity).WithMany(a => a.ContinuationsTo).HasForeignKey(x => x.ToActivityId);

        model.Entity<WeeklyPlan>().HasIndex(x => new { x.PersonId, x.WeekStart }).IsUnique();

        model.Entity<PersonWorkday>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_PersonWorkday_Hours", "[WorkingHours] >= 0 AND [WorkingHours] <= 24");
            t.HasCheckConstraint("CK_PersonWorkday_Day", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
        });
        model.Entity<Person>().ToTable(t => t.HasCheckConstraint("CK_Person_Capacity", "[WeeklyCapacityHours] >= 0"));
        model.Entity<Activity>().ToTable(t => t.HasCheckConstraint("CK_Activity_Estimate", "[CurrentEstimateHours] >= 0"));
        model.Entity<ActivityDependency>().ToTable(t => t.HasCheckConstraint("CK_Dependency_NotSelf", "[PredecessorId] <> [SuccessorId]"));
        model.Entity<PlanBlock>().ToTable(t => t.HasCheckConstraint("CK_PlanBlock_Hours", "[PlannedHours] > 0"));
        model.Entity<WorkRecord>().ToTable(t => t.HasCheckConstraint("CK_WorkRecord_Hours", "[Hours] >= 0"));

        // Cuenta administradora inicial; jornada por defecto repartida de lunes a sábado.
        model.Entity<Person>().HasData(new Person
        {
            Id = 1,
            Name = "Administrador",
            Role = PersonRole.Admin,
            PlanningMode = PlanningMode.ByDeliverables,
            WeeklyCapacityHours = 45,
            IsActive = true
        });
        var workdays = new List<PersonWorkday>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            workdays.Add(new PersonWorkday
            {
                PersonId = 1,
                DayOfWeek = day,
                WorkingHours = day == DayOfWeek.Sunday ? 0 : day == DayOfWeek.Saturday ? 5m : 8m
            });
        }
        model.Entity<PersonWorkday>().HasData(workdays);

        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(string))
                    property.SetMaxLength(property.Name is "Description" or "Note" ? 2000 : 200);
                if (property.ClrType == typeof(decimal))
                {
                    property.SetPrecision(10);
                    property.SetScale(2);
                }
            }
            foreach (var foreignKey in entity.GetForeignKeys())
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }
}
