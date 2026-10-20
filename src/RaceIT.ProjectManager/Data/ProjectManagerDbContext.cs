using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Data;

public sealed class ProjectManagerDbContext(DbContextOptions<ProjectManagerDbContext> options)
    : DbContext(options)
{
    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectActivity> Activities => Set<ProjectActivity>();
    public DbSet<ActivityDependency> ActivityDependencies => Set<ActivityDependency>();
    public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();
    public DbSet<WorkCalendarDay> WorkCalendarDays => Set<WorkCalendarDay>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<MilestoneRequirement> MilestoneRequirements => Set<MilestoneRequirement>();
    public DbSet<Responsible> Responsibles => Set<Responsible>();
    public DbSet<Blocker> Blockers => Set<Blocker>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<WorkCalendarDay>().HasKey(x => new { x.WorkCalendarId, x.DayOfWeek });
        model.Entity<ActivityDependency>().HasKey(x => new { x.PredecessorId, x.SuccessorId });
        model.Entity<ActivityDependency>().HasOne(x => x.Predecessor).WithMany().HasForeignKey(x => x.PredecessorId);
        model.Entity<ActivityDependency>().HasOne(x => x.Successor).WithMany().HasForeignKey(x => x.SuccessorId);
        model.Entity<MilestoneRequirement>().HasKey(x => new { x.MilestoneId, x.ActivityId });
        model.Entity<MilestoneRequirement>().HasOne(x => x.ApprovedBy).WithMany().HasForeignKey(x => x.ApprovedById);

        model.Entity<WorkCalendarDay>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_CalendarDay_Hours", "[WorkingHours] >= 0 AND [WorkingHours] <= 24");
            t.HasCheckConstraint("CK_CalendarDay_Day", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
        });
        model.Entity<ActivityDependency>().ToTable(t => t.HasCheckConstraint("CK_Dependency_NotSelf", "[PredecessorId] <> [SuccessorId]"));
        model.Entity<ProjectActivity>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Activity_Duration", "(([PlannedDurationType] = 2 AND [PlannedDurationHours] = 0) OR ([PlannedDurationType] <> 2 AND [PlannedDurationHours] > 0)) AND (([EstimatedDurationType] = 2 AND [EstimatedDurationHours] = 0) OR ([EstimatedDurationType] <> 2 AND [EstimatedDurationHours] > 0))");
            t.HasCheckConstraint("CK_Activity_PlanDates", "[PlannedFinish] >= [PlannedStart]");
            t.HasCheckConstraint("CK_Activity_EstimatedDates", "[EstimatedFinish] >= [EstimatedStart]");
            t.HasCheckConstraint("CK_Activity_ActualDates", "[ActualFinish] IS NULL OR ([ActualStart] IS NOT NULL AND [ActualFinish] >= [ActualStart])");
            t.HasCheckConstraint("CK_Activity_DurationType", "[PlannedDurationType] IN (0,1,2) AND [EstimatedDurationType] IN (0,1,2)");
            t.HasCheckConstraint("CK_Activity_Status", "[Status] IN (0,1,2)");
        });
        model.Entity<Blocker>().ToTable(t => t.HasCheckConstraint("CK_Blocker_Dates", "[ResolvedAt] IS NULL OR [ResolvedAt] >= [CreatedAt]"));

        model.Entity<WorkCalendar>().HasData(
            new WorkCalendar { Id = 1, Name = "Lunes a viernes - 40 horas" },
            new WorkCalendar { Id = 2, Name = "Lunes a sabado - 45 horas" },
            new WorkCalendar { Id = 3, Name = "Flexible / sin horario", IsFlexible = true });
        var calendarDays = new List<WorkCalendarDay>();
        for (var calendarId = 1; calendarId <= 2; calendarId++)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                calendarDays.Add(new WorkCalendarDay
                {
                    WorkCalendarId = calendarId,
                    DayOfWeek = day,
                    WorkingHours = day == DayOfWeek.Sunday ? 0
                        : day == DayOfWeek.Saturday ? (calendarId == 2 ? 5 : 0) : 8
                });
            }
        }
        model.Entity<WorkCalendarDay>().HasData(calendarDays);

        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(string))
                    property.SetMaxLength(property.Name == "Description" ? 2000 : 200);
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



