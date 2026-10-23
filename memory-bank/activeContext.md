# Active context

Actualizado: 2026-10-23 (America/Denver).

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

