using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

// Datos de prueba para desarrollo. Idempotente: solo siembra negocio cuando no hay iniciativas.
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        // 1. El administrador sembrado pasa a llamarse Edgar.
        var edgar = await db.People.SingleOrDefaultAsync(x => x.Id == 1);
        if (edgar is not null)
        {
            edgar.Name = "Edgar";
            edgar.Email = "edgar@raceit.local";
            edgar.Role = PersonRole.Admin;
            edgar.WeeklyCapacityHours = 45;
        }

        // 2. Empleado Pablo.
        var pablo = await db.People.SingleOrDefaultAsync(x => x.Name == "Pablo");
        if (pablo is null)
        {
            pablo = new Person
            {
                Name = "Pablo",
                Email = "pablo@raceit.local",
                Role = PersonRole.User,
                PlanningMode = PlanningMode.Weekly,
                WeeklyCapacityHours = 45,
                IsActive = true,
                Workdays = PersonService.DefaultWorkdays(45)
            };
            db.People.Add(pablo);
        }
        await db.SaveChangesAsync();

        // 3. Solo sembrar negocio si no hay iniciativas.
        if (await db.Initiatives.AnyAsync()) return;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var dataLogger = new Initiative { Name = "Data Logger", Description = "Registro de datos del auto de carreras.", TargetDate = today.AddDays(42) };
        var pcb = new Project { Name = "PCB", Initiative = dataLogger, Person = pablo, TargetDate = today.AddDays(21) };
        var caseInterno = new Project { Name = "Case interno", Initiative = dataLogger, Person = pablo, TargetDate = today.AddDays(28) };
        var bateria = new Project { Name = "Selección de batería", Initiative = dataLogger, Person = pablo, TargetDate = today.AddDays(14) };
        var cajones = new Project { Name = "Cajones de herramientas", Person = pablo, TargetDate = today.AddDays(42) };

        var disenarEsquematico = new Activity { Title = "Diseñar esquemático", Project = pcb, Person = pablo, CurrentEstimateHours = 8, Status = ActivityStatus.Completed };
        var layoutPcb = new Activity { Title = "Layout PCB", Project = pcb, Person = pablo, CurrentEstimateHours = 6 };
        var enviarFabricar = new Activity { Title = "Enviar a fabricar", Project = pcb, Person = pablo, CurrentEstimateHours = 1 };
        var recibirPcb = new Activity { Title = "Recibir e inspeccionar", Project = pcb, Person = pablo, CurrentEstimateHours = 4 };

        var disenarCase = new Activity { Title = "Diseñar case", Project = caseInterno, Person = pablo, CurrentEstimateHours = 6 };
        var imprimirCase = new Activity { Title = "Imprimir prototipo", Project = caseInterno, Person = pablo, CurrentEstimateHours = 4 };

        var evaluarCeldas = new Activity { Title = "Evaluar celdas", Project = bateria, Person = pablo, CurrentEstimateHours = 6, Status = ActivityStatus.InProgress };

        var disenarCajones = new Activity { Title = "Diseñar cajones", Project = cajones, Person = pablo, CurrentEstimateHours = 5, Status = ActivityStatus.InProgress };
        var comprarMaterial = new Activity { Title = "Comprar material", Project = cajones, Person = pablo, CurrentEstimateHours = 2 };
        var fabricarCajones = new Activity { Title = "Fabricar cajones", Project = cajones, Person = pablo, CurrentEstimateHours = 8 };
        var instalarCierre = new Activity { Title = "Instalar cierre", Project = cajones, Person = pablo, CurrentEstimateHours = 3 };

        var aceite = new Activity { Title = "Cambio de aceite al Tundra", Person = pablo, CurrentEstimateHours = 2 };
        var impresora = new Activity { Title = "Instalar base de impresora", Person = pablo, CurrentEstimateHours = 1.5m };

        db.AddRange(dataLogger, pcb, caseInterno, bateria, cajones,
            disenarEsquematico, layoutPcb, enviarFabricar, recibirPcb,
            disenarCase, imprimirCase, evaluarCeldas,
            disenarCajones, comprarMaterial, fabricarCajones, instalarCierre,
            aceite, impresora);
        await db.SaveChangesAsync();

        // Dependencias (fin → inicio).
        db.ActivityDependencies.AddRange(
            new ActivityDependency { Predecessor = disenarEsquematico, Successor = layoutPcb },
            new ActivityDependency { Predecessor = layoutPcb, Successor = enviarFabricar },
            new ActivityDependency { Predecessor = enviarFabricar, Successor = recibirPcb },
            new ActivityDependency { Predecessor = disenarCase, Successor = imprimirCase },
            new ActivityDependency { Predecessor = disenarCajones, Successor = comprarMaterial },
            new ActivityDependency { Predecessor = comprarMaterial, Successor = fabricarCajones },
            new ActivityDependency { Predecessor = fabricarCajones, Successor = instalarCierre });
        await db.SaveChangesAsync();

        // Plan semanal de Pablo (semana actual).
        var plan = new WeeklyPlan { Person = pablo, WeekStart = StartOfWeek(today) };
        plan.Blocks.Add(new PlanBlock { Activity = layoutPcb, Position = 1, PlannedHours = 7.5m });
        plan.Blocks.Add(new PlanBlock { Activity = fabricarCajones, Position = 2, PlannedHours = 7.5m });
        plan.Blocks.Add(new PlanBlock { Activity = instalarCierre, Position = 3, PlannedHours = 3 });
        plan.Blocks.Add(new PlanBlock { Activity = aceite, Position = 4, PlannedHours = 2 });
        db.WeeklyPlans.Add(plan);
        await db.SaveChangesAsync();

        // Registros de trabajo.
        var now = DateTimeOffset.Now;
        db.WorkRecords.AddRange(
            new WorkRecord { Person = pablo, Activity = disenarEsquematico, Kind = WorkRecordKind.Session, Start = now.AddDays(-3), End = now.AddDays(-3).AddHours(4), Hours = 4 },
            new WorkRecord { Person = pablo, Activity = disenarEsquematico, Kind = WorkRecordKind.Session, Start = now.AddDays(-2), End = now.AddDays(-2).AddHours(4), Hours = 4 },
            new WorkRecord { Person = pablo, Activity = evaluarCeldas, Kind = WorkRecordKind.Session, Start = now.AddDays(-1), End = now.AddDays(-1).AddHours(4), Hours = 4 },
            new WorkRecord { Person = pablo, Activity = disenarCajones, Kind = WorkRecordKind.Session, Start = now.AddHours(-1), End = null, Hours = 0 });
        await db.SaveChangesAsync();

        // Continuación: Diseñar cajones → Diseñar cajones (continuación).
        var continuacion = new Activity { Title = "Diseñar cajones (continuación)", Project = cajones, Person = pablo, CurrentEstimateHours = 2 };
        db.Activities.Add(continuacion);
        db.Continuations.Add(new Continuation { FromActivity = disenarCajones, ToActivity = continuacion });
        await db.SaveChangesAsync();
    }

    private static DateOnly StartOfWeek(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

}
