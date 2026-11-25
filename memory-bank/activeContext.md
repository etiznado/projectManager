# Active context

La cabecera actual de Frentes abiertos muestra el título a la izquierda y el enlace de regreso a la derecha en una sola fila. Las tarjetas siguen inmediatamente debajo, sin contador.

En Frentes abiertos se eliminó toda la cabecera, incluido el título. La región principal conserva su nombre accesible. Las tarjetas aparecen primero, con filas de altura natural y menos espacio interior en escritorio. El enlace de regreso está al pie.

## Depuración local Private QA en VS Code — issue #17 (2026-11-25)

Se agrega el perfil separado `RaceIT.ProjectManager (Private QA)` y una tarea de build serial para ese perfil. Seleccionarlo en Ejecutar y depurar antes de F5; usa únicamente loopback `127.0.0.1:5244`, entorno `PrivateQa`, selector sintético explícito y la instancia/base desechable `RaceIT_PM_SyntheticQA`, sin heredar `launchSettings.json`. El perfil Development original se conserva; sin Google configurado y fuera de `PrivateQa`, el acceso permanece cerrado por diseño. No se alteran reglas de autenticación, datos ni las ediciones manuales de `OpenFronts`. La configuración JSON se valida por separado; resultados de arranque/login y revisión de la UI exacta se registran en [#17](https://github.com/etiznado/projectManager/issues/17#issuecomment-7177019509).

El arranque no publicado habilita `UseStaticWebAssets()` únicamente en `PrivateQa` para resolver los estilos aislados generados durante build; no se activa ese cargador adicional en otros entornos. Corrección y evidencia de runtime en [#17](https://github.com/etiznado/projectManager/issues/17#issuecomment-7177141561).

## Simplificación visual de Frentes abiertos (2026-11-25)

Acceso: añadido «Frentes abiertos» al menú principal, junto a «Mi semana», para las sesiones autenticadas. Apunta a `/frentes-abiertos`. Build con salida separada sin errores/advertencias y 97/97 pruebas; navegación visual pendiente.

Seguimiento: por solicitud de Edgar se elimina el mensaje «Estado de … guardado: …» después de cambiar un estado. Se conservan la actualización de la ficha y los avisos de error. Build con salida separada sin errores/advertencias y 97/97 pruebas; sin nueva verificación visual.

Por solicitud directa de Edgar se retiran la marca «RaceIT · Vista personal» y el rombo, el subtítulo y los filtros de `OpenFronts`. La lista y su contador usan siempre todos los frentes personales cargados. Se oculta el contorno del foco de navegación únicamente en el título; los controles conservan su foco visible. Los botones de cambio de estado son más pequeños (12 px, altura mínima 28 px), con opciones no seleccionadas en gris y la activa en su color; siguen siendo pulsables y guardan mediante el servicio existente. Build con `-m:1` sin errores/advertencias y 97/97 pruebas fuera del sandbox; revisión visual de esta versión pendiente.

## Ajuste de controles de estado — issue #17 (2026-11-25)

Por solicitud directa de Edgar, el selector y Guardar se sustituyen por tres etiquetas de color: pulsar una guarda inmediatamente ese estado mediante el mismo servicio SQL. La etiqueta activa refleja solo el estado confirmado, las operaciones en curso bloquean pulsaciones repetidas y se conserva el reintento/recarga ante errores o concurrencia. Sin cambios de modelo, autenticación, API ni Mi semana. Build sin errores/advertencias. La autorización, el handoff y los resultados de revisión/navegador de esta nueva versión se registran en [#17](https://github.com/etiznado/projectManager/issues/17#issuecomment-7176416403); Edgar conserva la aceptación de la UI exacta.

## Frentes abiertos persistidos — issue #17 (2026-11-25)

Implementación local en `codex/feat-005-frentes-abiertos`, con FEAT-005 conservado como antecedente de evaluación. Se recuperaron composición, layout y CSS de la demo Blazor de cuatro columnas; `/frentes-abiertos` ahora lee frentes personales de SQL mediante `OpenFrontService` y permite guardar el estado desde cada ficha. Solo se muestran/actualizan frentes del propietario activo, incluso para Administrador. Estados: En curso, Necesita decisión y En espera. Último avance y siguiente paso se conservan al cambiar estado; la revisión de concurrencia rechaza ediciones obsoletas. Tabla `OpenFronts` independiente de actividades/planificación, migración aditiva y sin semilla de frentes reales o ficticios en la base habitual. Sin cambios en Mi semana, autenticación ni API de Jarvis; FEAT-016 podrá reutilizar el servicio. Requisitos y diseño autorizados en comentarios de [#17](https://github.com/etiznado/projectManager/issues/17); la carga inicial real/CRUD sigue fuera del alcance. Verificación SQL desechable y visual a cargo del coordinador/QA; aceptación de UI de Edgar pendiente. No se ha aplicado esta migración a la base del dueño ni hay push/PR/despliegue.

Verificación posterior del coordinador (2026-11-25): migraciones aplicadas solo a una instancia/base LocalDB desechable nueva `RaceIT_PM_SyntheticQA`, con archivos nuevos y datos sintéticos. Persistencia comprobada con el servicio y un contexto nuevo, y desde navegador al salir/reabrir la vista. Comprobados aislamiento entre dos administradores, filtros/vacío, rechazo real de SQL con reintento y conflicto entre dos pestañas con recarga. La planificación mantuvo su huella antes/después. Cuatro columnas verificadas a 1920×1080 y una columna sin desbordamiento horizontal a 390×844; cinco fichas con los controles de edición requieren desplazamiento vertical. Preview exclusivamente loopback en `http://127.0.0.1:5243/frentes-abiertos`, sin autorización de publicación. La revisión de UI de Edgar sigue pendiente; no se tocó la base habitual. Evidencia final de QA y decisiones en #17.

Actualizado: 2026-11-23 (America/Denver).

## Approved scope launches the complete issue flow (2026-11-23)

Edgar corrected the workflow instructions: Jarvis creates a Draft issue with only requested scope, obtains Edgar's approval of that exact scope, and marks ready. Codex then runs Analyst (tagged Gherkin) → Architect → Developer → Reviewer → QA without a second approval of in-scope Gherkin or new per-phase authorizations. After verified Gherkin, Architect verifies approved scope and requirements, then verifies ready → inprogress before design. Material scope changes require Edgar's approval unless expressly delegated for that feature. Codex authors, posts, and reads back its own artifacts; posting or verification failure stops the phase and is reported BLOCKED to Jarvis, which must not draft or publish those artifacts. Jarvis marks completed only after Edgar confirms completion. Required UI, merge, and deployment approvals remain. Historical records, including FEAT-007, remain prior evidence; earlier workflow notes below are history and are superseded where they conflict with this entry and [the current workflow](../features/README.md).

This task changes instructions only on `feature/AgentGithubIssueIntegration`; no app code, monitors, actual issues, labels, or historical feature files were changed. Checkout and clean Git state were verified before editing. Verification results are recorded in progress.md; no end-to-end GitHub flow was exercised.

## First agent claim changes issue label (2026-11-22)

Owner clarified the issue-backed feature flow: after requirements approval and execution authorization, the Architect is the first agent to work from `Ready`. It changes the supplied issue label from `ready` to `inprogress`, preserving unrelated labels, and verifies the issue before design work. Unexpected or partial label states block the phase until reconciled. The coordinator records the verified claim in the issue's feature record comment. This updates instructions only; no issue was supplied and no label was changed here.

## GitHub issue as new feature record (2026-11-22)

Owner changed the feature workflow: direct Codex requests must supply a GitHub issue number/URL; Jarvis supplies it in its launch. For new feature work, the issue is the canonical record. The coordinator posts state/decision/handoff references, while Analyst, Architect, Developer, Reviewer, and QA post their complete outputs as distinct issue comments. Comment permalinks and exact code revisions drive gates; approved requirements are versioned with a new comment and reapproved when changed. New `features/FEAT-NNN/` directories and implementation logs are no longer created. Existing FEAT files and the `_template` files remain historical evidence and content guides. Repository instructions and the five role definitions were updated; no issue was supplied in this task, so no comment was posted and no end-to-end comment handoff was exercised. See [feature workflow](../features/README.md).

## Jarvis-to-Codex feature handoff clarification (2026-11-22)

The failed Jarvis launch ran in a surface without the configured Codex role types or a Git work tree; its report correctly stopped before implementation. The repository role files are tracked, and this local Codex runtime exposes all five `pm_*` agent types. `git rev-parse --show-toplevel` resolves to this repository; `git status` works. The workflow now directs Jarvis to start a Codex task in this repository and lets that task invoke each real `agent_type` after a work-tree and role preflight. This changes coordination instructions only; it does not configure Jarvis itself or start a feature. Local build passed with 0 warnings/errors and 69/69 tests passed. See [feature workflow](../features/README.md#jarvis-activation).

## FEAT-006: selector Light/Dark (2026-11-22)

Edgar autorizó el arranque manual en el checkout aislado `C:\Users\etizn\source\repos\projectManager-FEAT-006`, rama `feature/feat-006-theme-toggle`. El candidato local sin commit agrega un estado de tema scoped por circuito, un botón accesible en el encabezado autenticado y paletas CSS Light/Dark. `pm_architect`, `pm_developer`, `pm_reviewer` y `pm_qa` actuaron en ese orden; Reviewer aprobó sin correcciones. Build 0 errores/0 advertencias y 70/70 pruebas. QA: un caso unitario PASS y cinco casos de navegador BLOCKED por falta de loopback exclusivo con fixture SQL ficticio verificado; no se inició app ni SQL. Estado `In Progress`; revisión visual y aprobación final de Edgar pendientes. Expediente y huellas en [FEAT-006](../features/FEAT-006/README.md).

## Feature workflow for future requests (2026-11-22)

The owner defined a new Jarvis-coordinated order for future features: Requirements Analyst → Architect → Developer → Reviewer → QA. New features use `features/FEAT-NNN/` with `README.md`, `requirements.md`, `design.md`, and `test-plan.md`. The analyst drafts concise requirements from the owner's informal request; the architect writes the design against approved requirements; the reviewer may correct implementation code before QA; QA writes a bounded positive/negative plan and validates the final revision. Existing flat FEAT records and their decisions remain unchanged. Repository instructions and agent definitions are updated; no Dot trigger was installed or activated here. See [the workflow](../features/README.md).

Clarification from the owner: Gherkin is required for new `requirements.md` files. Each acceptance scenario has a unique `@AC-NN` tag and concrete Given/When/Then steps; the set covers normal and relevant rejection or exception behavior. The owner approves that exact scenario revision before `Ready`. QA maps its test cases to the scenario tags. The template, workflow, and Analyst/QA instructions now require this; no existing feature record was rewritten.

## FEAT-002: zonas de arrastre en Mi semana (2026-11-20)

En la copia local de Windows, FEAT-002 está `In Progress`. Se añadieron indicadores «Insertar antes» sobre la fila poblada completa y «Agregar al final» en el encabezado y la zona inferior izquierda. La zona inferior derecha permanece sin destino de drop. La limpieza al salir de una fila usa una comprobación geométrica para no ocultar el indicador al cruzar elementos internos. No se cambió `WeeklyPlanService`, la semántica del plan ni la corrección de FEAT-001. Build con salida separada: 0 errores/0 advertencias; la suite no produjo resultado por fallo de conexión `vstest`/`testhost`. QA quedó BLOCKED para aceptación completa; Reviewer aprobó técnicamente el diff corregido con esos límites. Jarvis debe verificar UI y persistencia en Chrome con base SQL aislada autorizada y datos sintéticos antes de avanzar de estado. Revisión exacta y evidencia por rol en [FEAT-002](../features/FEAT-002.md).

## Pool agrupado por proyecto (2026-10-23)

El pool de Mi semana agrupa las actividades por proyecto con encabezado colapsable (expandido por defecto; el colapso vive en memoria del componente). Regla pura `PoolGrouping.Group`: «Sin proyecto» al inicio y el resto alfabético, conservando el orden de creación dentro de cada grupo. Por decisión del usuario el encabezado muestra solo el nombre, sin iniciativa y sin encabezados fijos. Verificado: build 0 errores/0 advertencias y 69/69 pruebas xUnit; revisión visual pendiente. Detalle en `progress.md`.


## Carriles uniformes en Mi semana (2026-10-23)

Decisión del usuario: en `/mi-semana` cada día con jornada ocupa el **mismo ancho** y los días de 0 h no ocupan espacio; el trabajo que excede la jornada sumada se recorta en el borde con la marca «fuera de jornada». Implementado en `WeekLayoutCalculator` (`WorkingDays`, `TrackPercent`, `SplitIntoDays`, `SegmentFillFraction`), en `MyWeek.razor` (encabezado, cuadrículas y una barra por tramo) y en `app.css` (tramos continuos, chip de desborde). Se retiró `AxisTotal`. Efecto asumido: la escala de horas deja de ser lineal (una hora en un día corto ocupa más ancho que en un día largo). Tras la primera revisión visual del usuario se fijó la altura de la barra (`height: calc(1.2em + .6rem)`), porque los tramos sin etiqueta quedaban más delgados que el primero; tras la segunda, la etiqueta de horas pasó a ser una sola capa centrada sobre el bloque completo (`BlockVm.LeftPercent`/`SpanPercent`), porque dibujarla en el primer tramo la recortaba cuando ese tramo era una astilla. Verificado: build 0 errores/0 advertencias y 63/63 pruebas xUnit; revisión visual pendiente. Detalle en `progress.md`.


## Redefinición del producto (vigente)

Se descartó por completo el enfoque anterior (cronograma por fechas/Gantt, milestones, bloqueos, calendarios por proyecto y TV por iniciativa), junto con su código y base de datos. La fuente de verdad del nuevo diseño es `Project_Manager_Notas_Diseno.md`: modelo de **esfuerzo y plan semanal** con registro de tiempo y alertas de riesgo. Los renders `01/02/03` de memory-bank son la referencia visual.

## Estado implementado (Etapas 1 a 7)

- Stack sin cambio: .NET 10, Blazor Interactive Server, EF Core + SQL Server (base `projectManager`, recreada vacía).
- Modelo de dominio nuevo: `Person` (+`PersonWorkday`), `Initiative`, `Project`, `Activity`, `ActivityDependency`, `WeeklyPlan`, `PlanBlock`, `WorkRecord`, `Template`, `TemplateItem`, `Continuation`. Las actividades **no** requieren fechas.
- Login provisional por selección de persona (sin contraseña), cookie de sesión, roles Administrador/Usuario.
- Servicios: `WorkAccess` (permisos), `PersonService`, `ProjectService` (iniciativas y proyectos), `ActivityService` (actividades sin fechas, dependencias con validación de ciclos, horas pendientes y continuaciones), `WorkRecordService` (sesiones, registro manual, contador único, completar y `ListOpenSessionActivityIdsAsync`), `WeeklyPlanService` (plan semanal y bloques), `WeekLayoutCalculator` (regla pura de colocación), `PanoramaService` (panorama y riesgo), `RiskCalculator` (regla pura de capacidad compartida), `TemplateService` (plantillas de carreras y generación), `TemplateRules` y `PlanningRules` (reglas puras). `BlockHoursInput` valida las horas planeadas del bloque (máximo = pendientes).
- Pantallas base: `/login`, `/mi-semana` (navegación semanal, métricas, pool de actividades colapsable con arrastre al timeline, timeline con barras proporcionales y estados de trabajo, reordenamiento vertical por arrastre y detalle de la actividad en modal con registro de tiempo y edición de horas del bloque), `/initiatives` (vista de solo lectura de iniciativas y proyectos con métricas, banner de riesgo, auto-refresco y pantalla completa), `/administracion` (personas, iniciativas, proyectos, actividades y plantillas).
- Verificado: build sin errores/advertencias, 33 pruebas xUnit (PlanningRules, WorkAccess, WeekLayoutCalculator, RiskCalculator, TemplateRules y BlockHoursInput), migraciones `InitialModel`, `Continuations` y `WorkweekEightFive` aplicadas sobre base vacía con semilla del Administrador (Persona Id=1).

## Siguiente

MVP completo (Etapas 1–7). Pendientes diferidos (no bloquean): login real, contabilidad de continuaciones, timeline visual con líneas punteadas y riesgo semanal.

## Creación para empleados (2026-10-22)

- El usuario decidió conservar la creación de iniciativas exclusivamente para Administrador. Usuario puede crear proyectos independientes (sin iniciativa) y actividades propias.
- Nueva página `/mis-proyectos` para Usuario, enlazada desde la navegación; lista sus proyectos y permite crear uno sin iniciativa. `ProjectService` filtra su listado por responsable y rechaza en servidor un `InitiativeId` enviado por Usuario.
- Mi semana incorpora «Nueva actividad» en el pool: abre un modal con el formulario de actividad. Al crear, refresca el pool de la semana visible sin agregar automáticamente un bloque. El formulario ofrece proyectos propios al usuario; `ActivityService` valida en servidor que el proyecto sea propio.
- Administración reutiliza los componentes `ProjectForm` y `ActivityForm`; iniciativas, personas y plantillas siguen en Administración.
- Verificado: `dotnet build RaceIT.ProjectManager.sln` 0 errores/advertencias y `dotnet test RaceIT.ProjectManager.sln --no-build` 33/33. Pendiente: interacción visual y persistencia SQL de estos flujos en uso real.

## Dependencias aplazadas en la creación (2026-10-22)

Por decisión del usuario, el formulario compartido `ActivityForm` ya no muestra ni envía predecesoras, tanto en Mi semana como en Administración. El modelo y la lógica del servicio permanecen para una posible recuperación posterior. No se modificaron dependencias ya guardadas ni se cambió el esquema. Verificado: compilación con salida alternativa (`BaseOutputPath=bin/verification/`) y 33/33 pruebas. La salida normal estaba bloqueada por otro proceso; revisión visual pendiente.

## Proyecto propio al crear desde Mi semana (2026-10-22)

El usuario mostró que Edgar (Administrador, PersonId=1) creó la actividad «Nueva» en «Cajones de herramientas» de Pablo (PersonId=2). La opción anterior de Mi semana cargaba todos los proyectos para el administrador y `CreateAsync` permitía cualquier proyecto. Se corrigió: `ActivityForm` en modo `AssignToCurrentPerson` usa `ListOwnProjectsAsync` y `CreateOwnAsync`, que fija al responsable actual y valida la propiedad del proyecto también en servidor. Administración conserva la creación global del administrador. La fila «Nueva» ya guardada no se modificó. Verificado: compilación y 38/38 pruebas, incluidas 5 variantes de la regla de propiedad. Prueba visual y de SQL pendiente.

## Edición de proyectos (2026-10-22)

- El formulario compartido `ProjectForm` permite crear o editar. `/mis-proyectos` ofrece Editar para proyectos asignados al Usuario: nombre y fecha meta. En Administración, Editar permite además cambiar iniciativa y responsable.
- `ProjectService.UpdateProjectAsync` valida nombre, existencia y propiedad del proyecto. Usuario no puede alterar iniciativa ni responsable, incluso mediante datos enviados manualmente; Administrador puede hacerlo. Se conservan las actividades vinculadas al proyecto.
- Verificado: compilación en `bin/verification` y 42/42 pruebas xUnit, incluidas 4 variantes nuevas de autorización. Pendiente: interacción visual y persistencia SQL en uso real.

## Administración por secciones (2026-10-22)

La página `/administracion` organiza Personas, Iniciativas, Proyectos, Actividades y Plantillas en botones de sección; solo se ve una categoría a la vez. Proyectos es la sección inicial y conserva su lista, edición y creación. Cambiar de sección limpia errores globales y cierra la edición de proyecto abierta. No se modificaron servicios ni datos. Verificado: compilación y 42/42 pruebas; revisión visual pendiente.

## Confirmación al eliminar plantilla (2026-10-22)

En Administración, Eliminar plantilla abre un modal que muestra su nombre y explica que se quitarán sus elementos mientras las actividades ya generadas permanecen. Solo «Sí, eliminar» llama a `TemplateService.DeleteAsync`; Cancelar, Escape o clic en el fondo cierran el modal. Se impide confirmar dos veces durante el guardado y se muestra el error en el diálogo si falla. Verificado: compilación y 42/42 pruebas existentes; interacción visual pendiente.

## Histórico: editor único de elementos de plantilla (2026-10-22; sustituido por `TemplateForm`)

Esta etapa intermedia usó un selector, `TemplateItemsEditor` y `TemplateService.UpdateItemsAsync` para editar los elementos. Fue sustituida el mismo día por el formulario único de creación/edición descrito abajo. Las actividades ya generadas son copias y no cambian. En esa etapa se verificaron compilación y 46/46 pruebas xUnit; la interacción visual y persistencia SQL quedaron pendientes.

## Administración: formulario único por módulo (2026-10-22; sustituye la disposición anterior)

- En Personas, Iniciativas, Proyectos, Actividades y Plantillas, el mismo formulario sirve para crear o editar: aparece arriba, y la lista de entidades con botón Editar queda abajo. Seleccionar una fila carga sus valores y lleva el enfoque al encabezado del formulario; Guardar o Cancelar edición vuelve al modo de creación.
- Se añadieron `PersonForm` e `InitiativeForm`, se adaptaron `ProjectForm` y `ActivityForm`, y `TemplateForm` sustituye el editor de solo elementos y el formulario separado de creación. Plantillas permite editar nombre, categoría y elementos en una sola operación. El modal de confirmación para eliminar plantillas sigue vigente.
- Servicios nuevos: `PersonService.UpdateAsync`, `ProjectService.UpdateInitiativeAsync`, `ActivityService.UpdateAsync` y `TemplateService.UpdateAsync`. Editar actividades no cambia estado, registros, bloques ni dependencias; editar iniciativas/proyectos/personas no modifica entidades hijas. Cambiar el rol propio durante la sesión se rechaza. La regla anterior de escalar la jornada al cambiar la bolsa semanal fue sustituida el 2026-10-23.
- Verificado: compilación y 49/49 pruebas xUnit con salida `bin/verification`; no se modificaron datos de usuario en SQL. Pendiente: comprobación visual y de persistencia SQL.

## Jornada diaria editable en Personas (2026-10-23)

- Decisión del usuario: cambiar la bolsa semanal deja las horas diarias como están. La bolsa y la suma de la jornada pueden diferir; se muestran ambas, sin forzar igualdad.
- `PersonForm` incluye `WorkdayEditor` al crear y editar. Muestra lunes a domingo, inicializa una persona nueva con 8/8/8/8/8/5/0 y carga la jornada persistida para edición. El servidor valida siete días únicos y horas de 0 a 24 con máximo dos decimales; guarda bolsa y jornada explícitamente, sin escalado automático.
- Verificado: compilación y 53/53 pruebas xUnit con salida `bin/verification`. Sin modificación directa de datos de usuario en SQL; pendientes la interacción visual y una prueba de persistencia con SQL Server.

## Selector de calendario en fechas meta (2026-10-23)

- Los dos campos editables de fecha meta (`InitiativeForm` y `ProjectForm`) siguen usando `InputDate`, que permite captura manual y abre el calendario nativo desde el icono dentro del campo. La clase compartida `calendar-date` aplica el tema oscuro al control y deja visible su indicador; `ProjectForm` se usa en Administración y Mis proyectos.
- Cambio de interfaz sin migración ni modificación de datos. Pendiente: comprobación visual e interacción en navegador.

## Puntos abiertos (del diseño; no bloquean)

- Jornada confirmada: 8 h de lunes a viernes, 5 h el sábado y 0 el domingo (45 h/sem). Aplicada a Edgar y Pablo; configurable por persona.
- Tolerancias de desfase de riesgo y contabilidad de continuaciones por cerrar.
- Revisar posibles sesiones colgadas: "Instalar cierre" acumuló 35.16 h registradas con estimación de 3 h ("Revisar estimación"); verificar si quedó una sesión abierta demasiado tiempo.
- Login real diferido (proveedor de identidad no elegido).

## Límites

El plan original protegido del modelo anterior ya no aplica (la base se vació por decisión explícita del usuario). En adelante, conservar los datos reales que capture el usuario. Milestones y bloqueos quedan fuera de este MVP (no figuran en el nuevo diseño).

## Arrastrar y soltar en Mi semana (2026-10-20)

- Implementado: arrastre nativo de tarjetas del pool a una fila del timeline/tabla (inserta antes) o a la zona final, incluso en un plan vacío. Usa las horas pendientes, o 1 h si se requiere reestimar; las horas siguen siendo editables.
- Implementado: reordenamiento vertical de bloques arrastrando la barra o el indicador ⠿ de la tabla. Conserva botones Agregar y ↑/↓ como alternativa sin arrastre. El eje horizontal se recalcula por orden y horas; no cambia sesiones ni registros.
- WeeklyPlanService inserta el nuevo bloque y ajusta posiciones en un solo SaveChanges; rechaza destinos inexistentes y órdenes con IDs duplicados. Sin migraciones ni datos de ejemplo nuevos.
- Verificado: build sin errores/advertencias y 29 pruebas existentes correctas. Pendiente: probar interacción de arrastre en navegador y persistencia SQL en uso real; no se afirma validación visual.
- Pendientes restantes: login real, contabilidad de continuaciones, timeline de Initiatives con líneas punteadas y riesgo semanal.

### Pool de la semana seleccionada (2026-10-20)

El pool excluye actividades que ya tienen al menos un bloque en el plan de la semana seleccionada. Al quitar el último bloque, la actividad abierta vuelve al pool; bloques en otras semanas no la ocultan. Se aplica al refrescar después de agregar, quitar o cambiar de semana.

### Filas integradas del timeline (2026-10-20)

Por solicitud del usuario se retiró la tabla inferior, la columna editable Horas y los controles ↑/↓. Cada actividad y su proyecto se muestran a la izquierda de su barra, alineados en una misma fila como el render expandido. La barra muestra solo horas; selección desde título/barra abre el mismo detalle. Se conserva quitar del plan y reordenamiento por arrastre. Las horas planeadas se conservan en datos, pero ya no tienen editor en esta vista. Cuadrícula diaria y encabezado comparten el eje proporcional. Build verificado sin errores/advertencias; revisión visual en navegador pendiente.

### Ancho completo (2026-10-20)

Se eliminó el máximo de 1180 px del contenedor principal compartido (.app-body). Las páginas aprovechan el 100% del ancho disponible conservando el padding lateral de 1.25rem. Cambio CSS comprobado en fuente; revisión visual en navegador pendiente.

### Pool sin botón Agregar al plan (2026-10-20)

Por solicitud del usuario se eliminó el botón Agregar al plan de las tarjetas y su manejador sin uso. La incorporación al plan se realiza mediante arrastre; pulsar el título sigue abriendo el detalle. Cambio revisado en fuente; sin verificación visual nueva.

### Columna Actividad más compacta (2026-10-20)

El encabezado del timeline ahora es Actividad. Su columna se redujo del 32% al 25%, con mínimo de 160 px, dejando más espacio al eje semanal. Cambio revisado en fuente; revisión visual pendiente.

### Filas más compactas (2026-10-20)

Se redujo la altura mínima de las filas del timeline de Mi semana de 88 a 72 px por solicitud del usuario. Cambio CSS revisado en fuente; revisión visual pendiente.

### Soltar dentro de Actividad (2026-10-20)

Se retiró el recuadro externo para agregar al final. El encabezado Actividad y el espacio vacío bajo sus filas aceptan actividades/bloques para agregarlos al final; las filas existentes conservan insertar antes. El espacio final forma parte de la cuadrícula, incluso en semana vacía. Revisión visual pendiente.

### Cabecera compacta de Mi semana (2026-10-20)

Se retiró el título visible Mi semana (se conserva PageTitle). Navegación semanal y métricas comparten una sola fila sobre pool/timeline; la barra permite desplazamiento horizontal si el ancho no alcanza. Compilación verificada sin errores ni advertencias; revisión visual pendiente.

### Timeline sin título ni instrucción (2026-10-20)

Se retiraron el título Plan de la semana y el texto de ayuda de arrastre del panel del timeline. La cuadrícula comienza directamente con Actividad y los días. Cambio de marcado revisado en fuente; sin nueva verificación visual.

### Altura disponible y scroll de paneles (2026-10-20)

Mi semana usa el alto restante del viewport después de la cabecera, navegación y métricas. Pool y timeline se estiran a la misma altura, con scroll vertical independiente al desbordarse. La cuadrícula llena el panel aun sin bloques: el espacio final de Actividad crece para aceptar arrastres. Encabezados del timeline fijos durante scroll. Plantillas y detalle siguen debajo del área principal, accesibles desplazando la página. Build sin errores/advertencias; verificación visual e interacción en navegador pendientes.

### Encabezados compactos del timeline (2026-10-20)

Se redujo el padding vertical de Actividad y los días, el interlineado de los días y la separación de sus horas. La fila conserva altura automática según el contenido. Cambio CSS revisado en fuente; sin verificación visual nueva.

### Actividades con filas compactas (2026-10-20)

Se redujo el mínimo de las filas de actividades a 44 px y el padding/interlineado de su contenido. Crecen cuando el título requiere más líneas; barras más compactas centradas. El espacio final sigue llenando el alto restante. Cambio revisado en fuente; revisión visual pendiente.

### Timeline sin scroll interno (2026-10-20)

Por solicitud del usuario, el panel del timeline deja de tener scroll interno: conserva el alto mínimo disponible y se extiende hacia abajo cuando sus actividades lo requieren. La página permite recorrer ese contenido. El pool conserva su alto disponible y scroll independiente. Cambio CSS revisado en fuente; comprobación visual pendiente.

### Compactación completa del timeline (2026-10-20)

Encabezado y actividades usan tracks max-content para evitar que las filas automáticas absorban altura sobrante. Actividades con mínimo de 40 px y menor padding, interlineado y tipografía; encabezado también compacto. Solo la fila vacía final absorbe el alto libre del panel. Sin scroll interno del timeline. Verificación visual pendiente.

### Corrección de compactación (2026-10-20)

Se restauraron el tamaño anterior de barras y la tipografía de encabezados/actividades. Se conserva únicamente la compactación del espacio interior y el ajuste de filas al contenido, sin estirarlas. Cambio CSS revisado en fuente; sin verificación visual nueva.

### Padding de 2 px en timeline (2026-10-20)

Encabezados y celdas de Actividad usan 2 px de padding. Se eliminaron el mínimo de altura de actividad, padding anidado del título y separación adicional del proyecto. Barras conservan su tamaño; su pista reserva su altura más 4 px. Tracks explícitos se generan como una única cadena de estilo. Revisión visual pendiente.

### Scroll del pool con tema oscuro (2026-10-20)

Barra del pool delgada, con pista del color del panel y control del color de los bordes; contraste mayor al pasar el mouse donde el navegador admite ese estilo. Conserva desplazamiento con rueda y touchpad. CSS scrollbar-width/color y fallback WebKit. Cambio revisado en fuente; revisión visual pendiente.

### Pool siempre igual al timeline (2026-10-20)

Se eliminó el máximo de altura del pool basado en viewport. El pool usa contención de tamaño y stretch: su contenido no determina la altura compartida, que sigue al timeline, incluso cuando este crece hacia abajo. Conserva scroll interno y estilo oscuro. Cambio CSS revisado en fuente; verificación visual pendiente.

### Icono del pool junto al título (2026-10-20)

El indicador de arrastre de cada tarjeta del pool comparte línea con el nombre de actividad mediante una cabecera flex. El título puede envolver si no cabe. Cambio de marcado/CSS revisado en fuente; revisión visual pendiente.

### Pool colapsable (2026-10-20)

Mi semana incorpora enlace visual << en el encabezado del pool y pestaña >> para expandirlo cuando está cerrado, con nombres accesibles. El timeline ocupa el espacio liberado; el estado local se mantiene al refrescar datos/cambiar semana durante la misma instancia de página. No se persiste entre recargas. Revisión visual pendiente.

### Padding del timeline a 5 px (2026-10-20)

Se aumentó el padding compartido de encabezados/celdas del timeline de 2 a 5 px; la pista reserva 10 px adicionales alrededor de la barra sin cambiar su tamaño. Cambio CSS revisado en fuente; revisión visual pendiente.

### Iconos para colapsar el pool (2026-10-20)

Los controles del pool usan SVG de doble chevrón en lugar del texto >>/<<, conservando etiquetas accesibles y comportamiento. Cambio de marcado/CSS revisado en fuente; revisión visual pendiente.

### Jornada confirmada de 45 h (2026-10-20)

El usuario confirmó 8 h de lunes a viernes, 5 h el sábado y 0 el domingo. Sustituye la jornada ilustrativa de 7.5 h/día. Esta fue la regla inicial de creación para capacidad 45 h; desde el 2026-10-23, la jornada se captura explícitamente en Personas y no cambia automáticamente con la bolsa. DataSeeder reutiliza la regla inicial; semilla del modelo actualizada. Migración WorkweekEightFive cambia jornadas anteriores de 7.5 h en personas de capacidad 45 h sin modificar bloques ni registros. Aplicada en SQL local mediante transacción y registrada en __EFMigrationsHistory; lectura posterior confirmó Edgar/Pablo con 8/8/8/8/8/5/0. Build 0 errores/advertencias y 29 pruebas existentes correctas; revisión visual pendiente.

### Etiqueta Completadas (2026-10-20)

Por solicitud del usuario, la métrica Registradas (total) ahora se titula Completadas. Solo cambió la etiqueta: sigue sumando las horas registradas de todas las actividades/semanas, no únicamente actividades terminadas ni la semana seleccionada. Cambio de texto revisado en fuente.

### Orden de métricas (2026-10-20)

Las métricas de Mi semana se muestran en el orden solicitado: Disponibles, Planeadas, Sin asignar, Completadas. Solo cambia el orden visual; cálculos sin cambios. Revisión en fuente, sin verificación visual nueva.

### Colores de métricas (2026-10-20)

Valores de Planeadas en azul, Sin asignar en amarillo y Completadas en verde, usando los colores existentes del tema. Disponibles conserva su color. Cambio de marcado/CSS revisado en fuente; revisión visual pendiente.

### Relleno verde en barras (2026-10-20)

Barras conservan longitud/etiqueta de horas planeadas y añaden relleno verde de tiempo registrado / estimación total actual de la actividad (limitado a 0–100%); actividad completada explícitamente llena la barra. Múltiples bloques de la misma actividad usan la misma proporción. Es indicador de consumo de esfuerzo, no medición independiente de avance físico ni finalización automática; se conserva la regla de revisar estimación al agotarse. Los datos actuales no incluyen porcentaje de avance físico. Verificación visual pendiente.

### Editar horas planeadas del bloque (2026-10-22)

Aclarado: la barra del timeline ya muestra las horas planeadas del bloque (`PlanBlock.PlannedHours`); coincidían con las pendientes porque al arrastrar del pool se inicializan con `RemainingHours`. Se conserva esa lógica de arrastre (el reordenamiento vertical solo cambia la posición). Se añadió en el detalle de la actividad un editor de las horas planeadas del bloque de la semana visible, con máximo = horas pendientes de la actividad (estimación − registradas). Validación en `BlockHoursInput` (máximo dinámico) y en `WeeklyPlanService.SetBlockHoursAsync` (servidor). Nuevo `BlockHoursInputTests` (4 pruebas). Build 0 errores/0 advertencias y 33 pruebas xUnit correctas; revisión visual en navegador pendiente.

### Detalle de la actividad como modal (2026-10-22)

El detalle de la actividad (antes al fondo de la página) ahora se abre como modal sobre un fondo oscurecido, con encabezado, botón ✕ y botón Cerrar. Cierra con clic en el fondo, con ✕, con Cerrar o con Escape; el enfoque al modal se hace con `ElementReference.FocusAsync` (sin JS propio). El contenido no cambió: estado, métricas, Iniciar/Pausar/Reanudar, Registrar tiempo, Completar, Continuar, edición de horas del bloque, registros y continuaciones; el error de las acciones del modal se muestra dentro del modal. Estilos nuevos en `app.css` (`.modal-backdrop`, `.modal`, `.modal-head`, `.modal-close`). Build 0 errores/0 advertencias y 33 pruebas xUnit correctas; revisión visual en navegador pendiente.

### Ajustes del modal y barras en curso (2026-10-22)

Se quitó el botón Cerrar del modal (quedan la ✕, el clic en el fondo y Escape). El nombre del proyecto en el modal usa la clase `.project-name` (color acento). Las barras se distinguen por estado de trabajo: amarillo brillante (`schedule-bar.running`, variable `--running`, con brillo) si la actividad tiene una sesión abierta (contador activo) y amarillo pálido (`schedule-bar.in-progress`, variable `--paused`) si está iniciada pero pausada. Se añadieron `Status` y `HasOpenSession` a `BlockVm` y `WorkRecordService.ListOpenSessionActivityIdsAsync` para conocer las sesiones abiertas. Build 0 errores/0 advertencias y 33 pruebas xUnit correctas; revisión visual en navegador pendiente.


## FEAT-001: unidad de horas en Mi semana (2026-11-20)

Autorizada por Edgar para la copia local. El resumen del modal usa `Format` sin añadir una segunda unidad literal; no cambian números, límites ni guardado. Build correcto (0 errores/advertencias), 69/69 pruebas. Revisión visual de Jarvis en Windows con SQL aislado y datos sintéticos pendiente; FEAT-001 sigue In Progress. Ver [expediente FEAT-001](../features/FEAT-001.md).

## FEAT-003 Google (trabajo aislado 2026-11-21)

En el worktree de FEAT-003 se implementó inicio con Google para Personas activas previamente vinculadas, invitaciones privadas de Administrador confiable y bootstrap por comando de operador. El selector solo se habilita con configuración explícita en `PrivateQa`, datos sintéticos y loopback; el POST aplica la misma guardia. Se retiró la ejecución automática de `DataSeeder` para evitar sobrescribir datos capturados. La migración `GoogleAccess` se aplicó solo a una LocalDB sintética nueva; no a la base del dueño. Sobre la revisión `03716799dbdefd66ca46c9657784393e557c0ed713b00b66b2817139fb112bab`, QA independiente pasó 87/87 xUnit y Jarvis comprobó la UI privada; Edgar aprobó solo la vista visual. Reviewer pidió corregir persistencia de invitación tras cancelación y resultado concurrente perdedor; `pm_developer` entregó la corrección y `pm_qa` independiente dio PASS offline a la revisión `368681aed2b53159fb2f913470acc65c3bd9c0ea069b460013f8744a1d96347f` (build limpio, 90/90 xUnit). La nueva revisión no tiene todavía prueba Google/HTTP, carrera SQL Server ni aprobación visual propia. El alcance excluye registro abierto, revocación/reemisión, identidades reales, secretos, despliegue y prueba manual con Google. Ver `features/FEAT-003.md`.

Incidente: `dotnet ef migrations remove --force --no-build` leyó la instancia SQL local configurada para consultar el historial de migraciones, fuera del alcance autorizado. No hubo escritura ni aplicación de migración allí. El snapshot se corrigió y QA lo verificó offline antes de aplicar la migración únicamente a la LocalDB sintética.
