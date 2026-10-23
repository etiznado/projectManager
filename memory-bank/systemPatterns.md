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

- Crear una actividad no exige fechas de inicio/fin: guarda estimación total (esfuerzo) antes de programarla. El formulario de creación no ofrece dependencias por ahora; el modelo y servicio las conservan.
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
- Formularios de creación `Components/Forms/ProjectForm.razor` y `ActivityForm.razor` reutilizados en Administración y en las pantallas del empleado. El padre recibe `OnCreated` y refresca su vista. Mi semana presenta `ActivityForm` en modal; `/mis-proyectos` presenta `ProjectForm` en página.
- `ProjectService.ListProjectsAsync` limita Usuario a sus proyectos, y `CreateProjectAsync` rechaza iniciativas para ese rol. `ActivityService.CreateAsync` valida proyecto propio para Usuario y permite gestión global para Administrador; mantiene validación y transacción para dependencias si se retoman después. `ActivityForm` no las envía.
- Al usar `ActivityForm` en Mi semana (`AssignToCurrentPerson`), incluso Administrador ve únicamente `ProjectService.ListOwnProjectsAsync` y guarda con `ActivityService.CreateOwnAsync`; este método fija el responsable actual y verifica la propiedad del proyecto en servidor. Administración utiliza `CreateAsync` para la gestión global. `WorkAccess.CanCreateActivityInProject` expresa y prueba esa diferencia.
- `ProjectForm` recibe opcionalmente `ExistingProject` para edición y emite `OnSaved`. `ProjectService.UpdateProjectAsync` valida propiedad mediante `WorkAccess.CanEditProject`; Usuario solo puede cambiar nombre y fecha meta. Administrador puede cambiar además iniciativa y responsable. Las actividades conservan sus propios responsables.
- `PersonForm`, `InitiativeForm`, `ProjectForm`, `ActivityForm` y `TemplateForm` usan el mismo componente para crear/editar desde Administración; la entidad existente es un parámetro opcional. Cada sección presenta formulario arriba y lista abajo. `TemplateService.UpdateAsync` valida nombre, categoría y elementos, conserva los IDs existentes, aplica altas/bajas y reordena en un `SaveChanges`. Requiere al menos un elemento; las actividades generadas previamente no dependen de la plantilla.
- Las fechas meta editables de `InitiativeForm` y `ProjectForm` usan `InputDate` para entrada manual y calendario nativo; `.calendar-date` muestra el icono del navegador dentro del campo en el tema oscuro.
- `PersonForm` integra `WorkdayEditor` en alta y edición. `PersonInput` valida siete días únicos y 0–24 h por día (dos decimales). `PersonService.UpdateAsync` guarda explícitamente la jornada enviada; cambiar solo la bolsa semanal conserva los valores diarios, incluso si la suma difiere. No permite cambiar el propio rol en la sesión actual. `ProjectService.UpdateInitiativeAsync` y `ActivityService.UpdateAsync` cambian solo los campos expuestos del formulario; la actividad conserva estado, sesiones, bloques y dependencias.
