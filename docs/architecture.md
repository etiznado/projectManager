# Arquitectura

Mapa del código y de las decisiones transversales. El estado de cada feature, sus aprobaciones y sus resultados de QA se consultan en el issue y los artefactos de Codex según el [índice](README.md).

## Aplicación y persistencia

Una Blazor Web App en `src/RaceIT.ProjectManager`, namespace `RaceIT.ProjectManager`, .NET 10 e Interactive Server. La UI y la API privada viven en la misma aplicación. EF Core usa SQL Server; los servicios reciben `IDbContextFactory<ProjectManagerDbContext>` y crean contextos cortos por operación.

`Domain/ProjectEntities.cs` contiene personas y jornadas, iniciativas, proyectos, actividades, dependencias, planes semanales, bloques, registros de trabajo, plantillas y continuaciones. `Data/ProjectManagerDbContext.cs` define relaciones, precisión decimal, restricciones y datos iniciales del modelo. Los cambios de esquema viven en `Migrations/`; su existencia no demuestra que estén aplicados en una base concreta.

Claves e invariantes de persistencia:

- `PersonWorkday`: `(PersonId, DayOfWeek)`; horas entre 0 y 24.
- `WeeklyPlan`: único por persona y comienzo de semana. `PlanBlock` referencia una actividad y conserva posición y horas planeadas.
- `ActivityDependency`: `(PredecessorId, SuccessorId)`, sin autorreferencia; el servicio comprueba ciclos del grafo.
- `Continuation`: vínculo manual entre actividades; la contabilidad definitiva de continuaciones sigue siendo una limitación descrita en el diseño funcional.
- Proyectos y actividades tienen responsables independientes. Reasignar un proyecto no reasigna automáticamente sus actividades.

## Servicios

| Área | Archivos en `Services/` |
| --- | --- |
| Permisos y personas | `WorkAccess.cs`, `PersonService.cs` |
| Iniciativas y proyectos | `ProjectService.cs` |
| Actividades, dependencias y continuaciones | `ActivityService.cs` |
| Sesiones, captura manual y terminación | `WorkRecordService.cs` |
| Semana y bloques | `WeeklyPlanService.cs` |
| Panorama y riesgo | `PanoramaService.cs`, `RiskCalculator.cs` |
| Entregas de iniciativas | `InitiativeDeliveryService.cs`, `InitiativeDeliveryRules.cs` |
| Plantillas y generación | `TemplateService.cs`, `TemplateRules.cs` |
| Reglas de esfuerzo, distribución y formularios | `PlanningRules.cs`, `WeekLayoutCalculator.cs`, `PoolGrouping.cs`, `PlanningInputs.cs` |

Las reglas puras se prueban sin SQL. `PlanningRules.RemainingHours` resta tiempo registrado de estimación y `NeedsReestimate` identifica una actividad abierta sin esfuerzo pendiente. La terminación es explícita. El cálculo de riesgo usa una bolsa compartida de capacidad; consultar el código y las pruebas para su fórmula efectiva, sin asumir un motor completo de reprogramación por todas las fechas.

`InitiativeDeliveryService` alimenta Initiatives con proyectos y actividades de la persona autenticada, incluso para administradores. Agrupa por ID de iniciativa y respeta los cierres explícitos de `JarvisPlanningState`. `InitiativeDeliveryRules` determina trabajo iniciado abierto, desvíos calendario y estado de entrega; no recalcula fechas desde esfuerzo. `Project.TargetDate` conserva el compromiso y `Project.EstimatedDate` es una fecha manual opcional independiente. `ProjectService.UpdateEstimatedDateAsync` y `UpdateAgreedDateAsync` validan sesión activa y propiedad o administración y escriben exclusivamente la fecha seleccionada. La edición general de proyecto conserva la estimación; los contratos de la API privada no se amplían. La columna estimada requiere su migración específica antes de ejecutar la nueva revisión contra una base; la existencia de sus archivos no implica aplicación autorizada. Los valores de ambas fechas abren un modal común que conserva contexto y devuelve el foco al control exacto de apertura. El módulo JS de Initiatives mide una referencia oculta e inerte de dos proyectos para el detalle individual, y gestiona el clic nativo y el evento `fullscreenchange` sin modificar el módulo de pantalla completa de otras vistas.

## Identidad y permisos

`Program.cs` configura Google OAuth, una cookie externa temporal y la cookie de sesión de la aplicación. `GoogleAccessService` vincula una persona activa a `issuer`/`subject` verificados o consume una invitación privada válida. Las invitaciones almacenan un hash del token, vencimiento y consumo; los vínculos y tokens tienen índices únicos. El consumo y el vínculo se confirman en transacción serializable.

Emitir una invitación requiere un administrador activo con sesión Google. El bootstrap inicial es un comando de operador, sin endpoint público de emisión. La configuración real y su activación requieren autorización operativa; que el código exista no demuestra que Google esté configurado en una instancia.

El selector de personas es exclusivo de `PrivateQa`, con habilitación explícita, marca de datos sintéticos, cliente loopback y destino aceptado por `AuthenticationGate` o por el perfil privado validado. No es un login general sin contraseña. POST de sesión usa antiforgery; las cookies incluyen `auth_source` y se revalidan contra la persona activa y la fuente permitida. `DataSeeder` no se invoca automáticamente en el arranque actual.

`WorkAccess` centraliza identidad, rol y propiedad. Administrador dispone de gestión global; Usuario gestiona trabajo propio/asignado según las comprobaciones concretas del servicio. Los permisos interactivos no sustituyen los del conector:

- `ProjectService.ListProjectsAsync` filtra por responsable para Usuario; sus proyectos nuevos son independientes. Solo Administrador asocia iniciativas o cambia responsable.
- La edición propia de proyecto permite nombre y fecha meta; el administrador puede cambiar además iniciativa y responsable.
- Mi semana usa `ListOwnProjectsAsync` y `ActivityService.CreateOwnAsync`: incluso un administrador crea allí actividades para sí y en proyectos propios. La gestión global está en Administración.
- `ListInitiativesAsync` interactivo carga iniciativas con sus proyectos; no debe reutilizarse suponiendo que proporciona aislamiento por propietario a una API privada.

## Planificación e interacción

`MyWeek.razor` representa una semana seleccionada y usa eventos HTML de arrastre. `WeeklyPlanService` valida permisos y destino al insertar o reordenar. Arrastrar cambia el plan, no inicia sesiones ni modifica el tiempo registrado. Incorporar una actividad desde el pool propone sus horas pendientes; las horas de un bloque se editan desde el modal, con validación del límite en el formulario y en servidor.

`WeekLayoutCalculator` distribuye bloques consecutivos en carriles de igual ancho para días con jornada positiva. Los días de cero horas no ocupan espacio; un bloque puede atravesar días sin sumar la noche. El exceso se representa como «fuera de jornada». `PoolGrouping` coloca «Sin proyecto» al principio y luego proyectos alfabéticos, con grupos colapsables.

El detalle de actividad abre en modal con foco inicial, cierre mediante ✕/fondo/Escape y errores dentro del modal. Conservar semana y contexto al editar y refrescar la lista al guardar. Las barras distinguen contador activo de actividad iniciada y pausada; el relleno representa consumo de esfuerzo, no un porcentaje garantizado de avance.

Administración usa formularios compartidos de creación/edición y una sección a la vez: Personas, Iniciativas, Proyectos, Actividades y Plantillas. Los formularios están arriba y las listas abajo. Las fechas meta usan calendario nativo. La bolsa semanal y las siete jornadas diarias se editan de forma independiente. Editar una plantilla afecta generaciones futuras; eliminarla pide confirmación y no elimina actividades ya generadas.

## Frentes abiertos y API privada

`OpenFrontService` conserva propietario, revisión y cambios de estado/avance/próximo paso con historial atómico. `OpenFronts.razor` consulta cada 30 segundos mediante `OpenFrontRefreshController`, evita solaparse con operaciones locales y conserva fichas válidas si una lectura falla. Los IDs mostrados son los persistidos.

`JarvisConnectorAuthentication` configura Bearer HTTPS independiente de las cookies, con digest de credencial y propietario activo del servidor. Los permisos de Frentes abiertos proceden de grants, procedencia de creación o modo de todos los frentes del propietario. La planificación añade configuración privada de planes y aprobaciones específicas; no puede ampliar permisos mediante flags enviados por el cliente.

`JarvisPlanningState` y `JarvisPlanningChange` son tablas auxiliares para propiedad atribuida, revisión, cierre, avance e historial. Las entidades originales siguen siendo fuente de nombre, fecha, relaciones, estimación y estado de actividad. Las escrituras usan transacción serializable y fingerprint canónico para detectar cambios realizados por otras vías. Las relaciones y el historial evitan exponer datos ajenos.

Los detalles de rutas, errores, consumo de aprobaciones y límites de escritura se mantienen únicamente en los contratos de [Frentes abiertos](jarvis-open-fronts-api.md) y [planificación](jarvis-planning-api.md). La API no permite borrar entidades ni cambiar horas existentes. Configurar, arrancar, migrar o activar un entorno son acciones distintas de implementar el código.
