# System patterns

## Sesión provisional y permisos

`Program.cs` configura cookies, autorización y estado de autenticación en Blazor. `/session/login` y `/session/logout` son POST con antiforgery; el login acepta un `PersonId` existente y activo (sin contraseña). Claims: NameIdentifier, PersonId, Name y Role (Administrador/Usuario). `WorkAccess` centraliza IsAdmin/PersonId/RequireOwn/RequireAdmin y expone métodos estáticos puros para pruebas. Login real y modelo de cuentas siguen pendientes.

## Arquitectura actual

Una Blazor Web App en `src/RaceIT.ProjectManager`, modo Interactive Server. Sin microservicios ni API separada. Componentes y servicios usan `IDbContextFactory<ProjectManagerDbContext>` con un contexto corto por operación. EF Core con SQL Server.

## Dominio (nuevo, sin fechas obligatorias)

`Person` (+`PersonWorkday`), `Initiative`, `Project`, `Activity`, `ActivityDependency`, `WeeklyPlan`, `PlanBlock`, `WorkRecord`, `Template`, `TemplateItem`. Claves compuestas en `PersonWorkday` (PersonId, DayOfWeek) y `ActivityDependency` (PredecessorId, SuccessorId); `WeeklyPlan` único por (PersonId, WeekStart).

## Archivos principales

- `Components/Pages/Login.razor`, `MyWeek.razor`, `Initiatives.razor`, `Administration.razor`, `Home.razor`.
- `Services/PlanningRules.cs`: reglas puras (horas pendientes, "necesita reestimar", ciclo de dependencias por orden topológico).
- `Services/WorkAccess.cs`: autorización.
- `Services/PersonService.cs`, `ProjectService.cs` (iniciativas y proyectos), `ActivityService.cs` (actividades y dependencias).
- `Domain/ProjectEntities.cs`: entidades y enumeraciones.
- `Data/ProjectManagerDbContext.cs`: mapeo, restricciones CHECK y semilla.
- `Migrations/`: migraciones `InitialModel`, `Continuations` y `WorkweekEightFive` más snapshot.

## Reglas vigentes

- Crear una actividad no exige fechas de inicio/fin: guarda estimación total (esfuerzo) y dependencias antes de programarla.
- Horas pendientes = estimación − tiempo registrado. Agotar la estimación sin terminar no completa ni marca avance; se señala "revisar estimación".
- Terminar es una acción explícita, independiente de horas.
- Dependencias se validan contra ciclos (grafo completo); no desplazan fechas automáticamente.
- Roles: Administrador (todo) y Usuario (lo suyo y lo asignado).
- Jornada confirmada: 8 h de lunes a viernes, 5 h el sábado y 0 el domingo (45 h/sem); configurable por persona.

Mi semana usa eventos HTML de drag/drop en Blazor con IDs del gesto local; WeeklyPlanService valida permisos y destino, inserta antes de un bloque o al final y persiste posiciones. El arrastre solo modifica el plan. La interfaz usa una cuadrícula de dos columnas compartidas (actividad/contexto y eje semanal) para alinear etiquetas y barras; se retiró la tabla separada y su editor de horas.
