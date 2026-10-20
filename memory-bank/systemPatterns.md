# System patterns

## Sesión provisional y permisos

Program.cs configura cookies, autorización y estado de autenticación en Blazor. /session/login y /session/logout son POST con validación antiforgery; aceptan solo identidad admin sintética o ResponsibleId existente. Ningún nombre se usa para elegir la identidad de sesión. Login real y modelo de cuentas siguen pendientes.

WorkAccess define los cinco grupos; autorización de servicios obtiene identidad del AuthenticationStateProvider del circuito. Routes usa AuthorizeRouteView. Rutas Home, Administration y TV exigen Administrador; EditActivity exige sesión y valida responsable antes de mostrar datos. MyWork filtra por ResponsibleId y consulta únicamente dependencias adyacentes para contexto. El motor sigue leyendo/recalculando el grafo completo internamente; eso no amplía la consulta del empleado.

No cambia esquema ni snapshot. Falta centralizar altas administrativas y completar los flujos de autoservicio, personas por ID, auditoría y notificaciones.


## Arquitectura actual

Una Blazor Web App en src/RaceIT.ProjectManager, modo Interactive Server. Sin microservicios ni API separada. Componentes usan IDbContextFactory<ProjectManagerDbContext> y un contexto corto por operación, adecuado para circuitos Blazor. EF Core con SQL Server.

## Archivos principales

- Components/Pages/Home.razor: inicio, comprobación de conexión y enlace a administración.
- Components/Pages/Administration.razor: creación y selección de iniciativas/proyectos, creación y lista de actividades; conserva estado al abrir modal.
- Components/Pages/EditActivity.razor: editor reutilizado en modal; también conserva ruta /actividades/{Id}/editar.
- Components/Shared/ActivityEditModal.razor y wwwroot/activity-modal.js: dialog nativo, showModal, Escape y callbacks de cierre.
- Services/PlanningInputs.cs: modelos de formularios y validación.
- Services/WorkCalendarCalculator.cs: fin de una actividad independiente según horas y días.
- Services/ActivityEditor.cs: aplica cambios de edición sin alterar PlannedStart, PlannedFinish ni PlannedDurationHours.
- Domain/ProjectEntities.cs: entidades y ActivityStatus.
- Data/ProjectManagerDbContext.cs: mapeo, restricciones y calendarios iniciales.
- Data/Migrations/: InitialProjectModel y snapshot.
- wwwroot/app.css: estilos y comportamiento adaptable de administración/modal.

## Persistencia

Tablas: Initiatives, Projects, Activities, ActivityDependencies, WorkCalendars, WorkCalendarDays, Milestones, MilestoneRequirements, Responsibles, Blockers. __EFMigrationsHistory controla migraciones.

DateOnly para fechas de planificación; decimal para horas; DateTimeOffset para bloqueos. Claves compuestas en dependencias, días de calendario y requisitos de milestone. Relaciones con DeleteBehavior.NoAction. Restricciones SQL para rangos de horas/días, duraciones positivas, orden de fechas y dependencia no autorreferente.

## Límites que deben respetarse

- Dependencias tienen gestión y motor funcional. Milestones y bloqueos tienen gestión funcional.
- ActivityPlanningService valida ciclos y alcance de iniciativa dentro de una transacción Serializable.
- ScheduleCalculator usa orden topológico y WorkPoint (fecha + horas desde un inicio de jornada común). Conserva consumo parcial y normaliza al calendario receptor. RequestedStart conserva la restricción manual; EstimatedStart/Finish son resultados. Actividades en curso se anclan al inicio real; completadas liberan el día siguiente al fin real, sin modificar sus fechas históricas.
- No guardar salud del proyecto como valor desconectado del cálculo.
- Sin autenticación ni control de concurrencia específico todavía.
- Migraciones explícitas, no automáticas al iniciar. No sobrescribir datos del usuario con ejemplos.

## Dependencias y pruebas

Services/ScheduleCalculator.cs es el motor puro, sin acceso SQL. Services/ActivityPlanningService.cs actualiza el grafo y todos los pronósticos de una iniciativa en una transacción Serializable. El editor delega el guardado a este servicio; la vista previa usa el mismo motor sobre una copia sin tracking. Las predecesoras se reemplazan por el conjunto elegido (casillas vacías eliminan vínculos). El orden de captura no determina la secuencia. Los calendarios asumen un inicio de jornada común porque todavía no existen horarios de entrada/salida.

Migración DependencyScheduling agrega RequestedStart, EstimatedStartHour y EstimatedFinishHour. Los desplazamientos son horas decimales, no horas del reloj. La migración copia EstimatedStart existente a RequestedStart. Nuevas actividades capturan RequestedStart; las horas calculadas se completan al recalcular.

Pruebas persistentes en tests/RaceIT.ProjectManager.Tests. Ejecutar dotnet test RaceIT.ProjectManager.sln. No dependen de SQL Server; la integración SQL se comprobó mediante un programa temporal, no forma parte de esa suite.

## Milestones

Components/Shared/MilestonePanel.razor administra captura/edición/confirmación en dialog nativo; usa activity-modal.js y conserva selección de administración. Recibe ProjectId, RefreshVersion y OnChanged; administración incrementa versión al refrescar actividades y recalcula resumen al guardar milestones.

Services/MilestoneRules.cs contiene MilestoneInput, MilestoneConfirmationInput, cálculo de estimado y reglas puras de confirmación/reapertura. MilestoneService guarda/edita requisitos, confirma y reabre dentro de transacciones Serializable. RefreshInitiativeAsync trabaja con el contexto/transacción del llamador y ActivityPlanningService lo llama antes de SaveChanges. ApprovedDate y ApprovedBy se registran por requisito al confirmar en conjunto; no existe flujo de aprobación individual ni autenticación del nombre capturado.

MilestoneService valida alcance de iniciativa y evita editar una definición confirmada sin reabrir. MilestoneRules.Refresh invalida aprobaciones si un requisito deja de estar completado o su fin real excede la confirmación. No se agregaron columnas ni migraciones.

## Bloqueos

Services/BlockerRules.cs define BlockerInput, reglas de resolver/reabrir y salud de actividad. BlockerService usa contextos cortos y transacciones Serializable para crear/editar/resolver/reabrir en el alcance de un proyecto. No permite crear/reabrir en completadas. La resolución es idempotente y no reemplaza un timestamp de resolución existente.

Components/Shared/BlockerPanel.razor contiene lista, filtro de resueltos y dialog nativo de creación/edición, usando activity-modal.js. Recibe ProjectId/RefreshVersion/OnChanged igual que MilestonePanel. Administración refresca ambos con milestoneVersion (nombre histórico, ahora versión de paneles). Claves de componentes hermanos son tuplas (typeof(componente), ID) para evitar colisiones entre proyecto y actividad.

ActivityPlanningService valida ausencia de bloqueos abiertos antes de completar; MilestoneService hace lo mismo para confirmar requisitos. Contadores abiertos y salud se consultan en administración. El riesgo por bloqueos aún es directo, no se propaga a toda la cadena ni demuestra pertenencia a ruta crítica. Las fechas no se recalculan por abrir/cerrar bloqueos.

Sin nueva migración; campos existentes CreatedAt/ResolvedAt son DateTimeOffset UTC al guardar y se muestran en hora local del servidor (desarrollo local). Reabrir limpia ResolvedAt; no hay auditoría histórica por cada reapertura.

## Dashboard TV

Services/TvDashboardService.cs realiza lectura sin tracking por iniciativa, dentro de una transacción Serializable corta para coherencia entre consultas. Incluye todos los proyectos/actividades, vínculos internos, milestones y solo bloqueos abiertos. No recalcula ni escribe el cronograma al visualizar.

Services/TvDashboard.cs contiene snapshot, cálculo de entrega/estado, filas visibles y rango/paginación puros. Components/Pages/Tv.razor gestiona /tv/{Id}, tarjetas, timer de cinco segundos (consulta cada sexto tick y rotación cada tercero), última lectura y errores. OnAfterRender inicia timer únicamente en modo interactivo; DisposeAsync lo cancela. Components/Shared/TvTimelineChart.razor dibuja SVG; wwwroot/tv.css define pantalla 16:9 adaptable y wwwroot/tv-dashboard.js solicita fullscreen.

Rango de fechas incluye plan, pronóstico, reales, objetivo y hoy, mínimo seis semanas; ticks semanales adaptativos. Barras conservan fracciones de jornada usando EstimatedStartHour/FinishHour, con fallback inclusivo para datos antiguos de hora cero. Conexiones solo entre filas de la misma página. Todos los datos influyen en métricas aunque no estén visibles. Proyectos sin actividades principales permanecen con fila resumen.

La tarjeta de salud global usa orden completada → atrasada → en riesgo → en tiempo; en riesgo incluye bloqueos abiertos y pronósticos vencidos sin afirmar impacto calculado sobre ruta crítica. Sin aislamiento por usuario/autenticación aún.

## Tipos de duración

Domain.DurationType: WorkingTime=0, ElapsedTime=1. ProjectActivity guarda PlannedDurationType y EstimatedDurationType; ambos default laboral. Las propiedades *DurationHours son canónicas para ambos tipos (día natural = 24 h). Los DTO agregan DurationDays/EstimatedDurationDays como adaptadores del mismo número de horas.

WorkCalendarCalculator.Schedule centraliza trabajo por calendario y espera continua; Finish conserva la API previa con tipo opcional y valida que inicio laboral sea hábil. ScheduleCalculator delega ambos tipos a Schedule y utiliza WorkPoint (fecha, hora desde origen común). El tipo continuo ignora calendario laboral y normaliza medianoche sin día adicional.

ActivityEditor cambia solo EstimatedDurationType. Captura inicial asigna ambos tipos y horas parciales. Cambio de unidad en formularios mantiene el número visible y lo expresa en la unidad elegida. Duraciones en lista se formatean con DurationText.

TvTimelineChart usa fracciones sobre 24 h para alinear trabajo y espera en el mismo eje, con mínimo de ancho para tareas cortas. PlanEnd deriva la posición del baseline desde duración y tipo originales. RowNote identifica Espera.

Migración ActivityDurationTypes agrega dos columnas int default 0 y CHECK de valores 0/1; no cambia fechas ni horas de registros existentes. Migrations usa fecha UTC en el nombre, mientras las notas usan fecha del cliente.

## Proyectos flexibles y edición

WorkCalendar.IsFlexible identifica el calendario Id 3 sin WorkCalendarDays. ProjectActivity.RequestedFinish es nullable para tipos anteriores; obligatorio a nivel de aplicación para ManualDates=2. Duraciones manuales son 0; SQL CK_Activity_Duration permite cero únicamente con tipo 2. No existe esfuerzo opcional separado.

ScheduleCalculator.ManualDateSchedule conserva span = RequestedFinish - RequestedStart, desplaza inicio por predecesoras y termina en Hour=24 del día final. Si predecesora libera Hour=24, comienza el día siguiente. Actividad en curso ancla al inicio real. ActivityEditor valida fin y no requiere horas/calendario para manuales.

Services/ProjectService.cs implementa edición por alcance de iniciativa, conversión pura ProjectRules.ConvertToFlexible y recálculo del grafo/milestones en una sola transacción Serializable. Convierte WorkingTime a ManualDates usando el calendario anterior, sin tocar columnas Planned* ni reales. El calendario receptor se carga con Days. Al pasar de flexible a horario las manuales se conservan.

Components/Shared/ProjectEditModal.razor usa dialog nativo/activity-modal.js y ProjectInput. Administración mantiene IDs, recarga proyectos/listas, y adapta captura nueva sin borrar su nombre si el calendario pasa a flexible. SelectProject/creación eligen ManualDates por defecto en flexible. Edición/captura ofrecen fechas manuales; WorkingTime deshabilitado en flexible y validado en servicios. TV muestra Flexible como nota.

Migración FlexibleProjectCalendars agrega flag, fin solicitado, semilla 3 y CHECKs ampliados. La semilla no asigna horas a flexible.

## Revisión para futura seguridad y trabajo por períodos (sin implementar)

Consultar users-and-work-planning.md. La estructura actual no tiene autenticación/autorización, ni historial/notificaciones. Administration.razor todavía crea entidades directamente; otros cambios usan servicios. Centralizar escrituras y consultas con alcance antes de introducir permisos uniformes. Conservar asignaciones ResponsibleId; no identificar cuentas por texto de nombre. Separar autor, responsable y cambios derivados del motor. La selección de tecnología de login y el modelo de períodos siguen pendientes; no se agregaron paquetes, entidades ni migraciones.

## Ampliación acordada: relaciones opcionales y TV por empleado (pendiente)

Permitir proyecto sin iniciativa y actividad independiente sin proyecto; conservar TV por iniciativa y añadir TV por empleado para Administrador. Modelo actual mantiene Project.InitiativeId y ProjectActivity.ProjectId obligatorios; no hay migraciones nuevas para esta ampliación. Ver users-and-work-planning.md.

Antes de implementar: revisar consultas, selectores, autorización, calendarios y recálculo que asumen InitiativeId/ProjectId obligatorios. Definir alcance explícito para proyectos sin iniciativa; no reunirlos accidentalmente por valor null. Preservar baseline, asignaciones, reglas de horas/espera/manuales, aislamiento por responsable y datos reales. Rutas y esquema nuevos todavía sin definir. TV por empleado no necesita planes por períodos para su primera versión.
