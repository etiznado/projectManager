# System patterns

## Sesión provisional y permisos

`Program.cs` configura cookies, autorización y estado de autenticación en Blazor. `/session/login` y `/session/logout` son POST con antiforgery; el login acepta un `PersonId` existente y activo (sin contraseña). Claims: NameIdentifier, PersonId, Name y Role (Administrador/Usuario). `WorkAccess` centraliza IsAdmin/PersonId/RequireOwn/RequireAdmin y expone métodos estáticos puros para pruebas. Login real y modelo de cuentas siguen pendientes.

## Arquitectura actual

Una Blazor Web App en `src/RaceIT.ProjectManager`, modo Interactive Server. Sin microservicios ni API separada. Componentes y servicios usan `IDbContextFactory<ProjectManagerDbContext>` con un contexto corto por operación. EF Core con SQL Server.

## Dominio (nuevo, sin fechas obligatorias)

`Person` (+`PersonWorkday`), `Initiative`, `Project`, `Activity`, `ActivityDependency`, `WeeklyPlan`, `PlanBlock`, `WorkRecord`, `Template`, `TemplateItem`, `Continuation`. Claves compuestas en `PersonWorkday` (PersonId, DayOfWeek) y `ActivityDependency` (PredecessorId, SuccessorId); `WeeklyPlan` único por (PersonId, WeekStart).

## Archivos principales

- `Components/Pages/Login.razor`, `MyWeek.razor`, `Initiatives.razor`, `Administration.razor`, `Home.razor`.
- `Services/PlanningRules.cs`: reglas puras (horas pendientes, "necesita reestimar", ciclo de dependencias por orden topológico).
- `Services/WorkAccess.cs`: autorización.
- `Services/PersonService.cs`, `ProjectService.cs` (iniciativas y proyectos), `ActivityService.cs` (actividades, dependencias y continuaciones).
- `Services/WorkRecordService.cs` (sesiones, registro manual, contador único, completar y `ListOpenSessionActivityIdsAsync`), `WeeklyPlanService.cs` (plan semanal y bloques), `PanoramaService.cs` (panorama y riesgo), `TemplateService.cs` (plantillas y generación).
- `Services/WeekLayoutCalculator.cs`, `RiskCalculator.cs`, `TemplateRules.cs` (reglas puras) y `Services/PlanningInputs.cs` (incluye `BlockHoursInput` para las horas del bloque).
- `Domain/ProjectEntities.cs`: entidades y enumeraciones.
- `Data/ProjectManagerDbContext.cs`: mapeo, restricciones CHECK y semilla.
- `Migrations/`: migraciones `InitialModel`, `Continuations` y `WorkweekEightFive` más snapshot.

## Reglas vigentes

- Crear una actividad no exige fechas de inicio/fin: guarda estimación total (esfuerzo) y dependencias antes de programarla.
- Horas pendientes = estimación − tiempo registrado. Agotar la estimación sin terminar no completa ni marca avance; se señala "revisar estimación".
- Las horas planeadas de un bloque no pueden superar las horas pendientes de la actividad; se editan desde el modal de la actividad.
- Terminar es una acción explícita, independiente de horas.
- Dependencias se validan contra ciclos (grafo completo); no desplazan fechas automáticamente.
- Roles: Administrador (todo) y Usuario (lo suyo y lo asignado).
- Jornada confirmada: 8 h de lunes a viernes, 5 h el sábado y 0 el domingo (45 h/sem); configurable por persona.

Mi semana usa eventos HTML de drag/drop en Blazor con IDs del gesto local; WeeklyPlanService valida permisos y destino, inserta antes de un bloque o al final y persiste posiciones. El arrastre solo modifica el plan: al traer una actividad del pool, las horas planeadas del bloque se inicializan con las horas pendientes; reordenar solo cambia la posición. La interfaz usa una cuadrícula de dos columnas compartidas (actividad/contexto y eje semanal) para alinear etiquetas y barras; se retiró la tabla separada y su editor de horas (el editor de horas del bloque vive ahora en el modal de la actividad).

## Patrones de UI

- Detalle de la actividad en **modal**: `.modal-backdrop` fijo con fondo oscurecido y `.modal`; cierra con la ✕, con clic en el fondo o con Escape. El enfoque inicial usa `ElementReference.FocusAsync` (sin JS propio) y `@onclick:stopPropagation` evita cerrar al hacer clic dentro. Los errores de las acciones se muestran dentro del modal.
- Barras del timeline por estado de trabajo: `schedule-bar.running` (amarillo brillante `--running`, con brillo) si la actividad tiene una sesión abierta (contador activo) y `schedule-bar.in-progress` (amarillo pálido `--paused`) si está iniciada pero pausada. El relleno verde indica consumo de esfuerzo (registradas / estimación).
- Edición de horas del bloque desde el modal (`BlockHoursInput` + `WeeklyPlanService.SetBlockHoursAsync`), con máximo = horas pendientes.
