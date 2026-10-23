# Progress

Actualizado: 2026-10-23.

## Icono de calendario en fechas editables (2026-10-23)

Los campos de fecha meta de Iniciativas y Proyectos usan el selector nativo `InputDate` con una clase compartida que hace visible el icono interior en el tema oscuro; conservan edición manual. El formulario de Proyectos se reutiliza en Administración y Mis proyectos. Sin cambios de esquema o datos; revisión visual e interacción en navegador pendientes.

## Jornada diaria editable en Personas (2026-10-23)

Por decisión del usuario, cambiar la bolsa semanal no altera las horas de cada día. `WorkdayEditor` se integra en el formulario compartido de alta y edición de Personas: siete controles, jornada nueva predeterminada de 8/8/8/8/8/5/0 y carga de valores existentes al editar. `PersonService` guarda bolsa y jornada por separado; `PersonInput` exige los siete días únicos y de 0 a 24 h por día con hasta dos decimales. La suma diaria puede diferir de la bolsa. Compilación y 53/53 pruebas xUnit correctas; interacción visual y persistencia SQL pendientes. No se alteraron datos de usuario en SQL durante la implementación.

## Creación de proyectos y actividades por Usuario (2026-10-22)

- `/mis-proyectos` permite al empleado listar proyectos asignados y crear proyectos independientes; iniciativa permanece exclusiva de Administrador.
- `ProjectForm` y `ActivityForm` son componentes reutilizados por Administración. Mi semana abre `ActivityForm` en modal y refresca el pool tras guardar, sin programar la actividad automáticamente.
- Servicios: `ProjectService` impide asociar proyectos del empleado a iniciativas y filtra su listado; `ActivityService` impide asociar actividades a proyectos de otro responsable. Conserva validación y transacción de dependencias para uso futuro.
- Verificado: build 0 errores/advertencias y 33 pruebas xUnit correctas. Pendiente: comprobar los nuevos formularios visualmente y con SQL en uso real.

### Dependencias aplazadas en el formulario (2026-10-22)

Se retiró el selector de predecesoras de `ActivityForm` compartido por Mi semana y Administración. La creación desde la interfaz no envía dependencias. Entidad, datos existentes y métodos del servicio se conservan para después. Compilación y 33/33 pruebas correctas con salida alternativa (`BaseOutputPath=bin/verification/`) por bloqueo de la DLL en `bin/Debug`. Revisión visual pendiente.

### Creación en proyecto propio desde Mi semana (2026-10-22)

Corregido el caso Edgar/Pablo: el administrador veía proyectos de otros responsables en el modal de Mi semana y podía guardar una actividad propia allí. `ActivityForm` usa ahora `ListOwnProjectsAsync` y `CreateOwnAsync` en ese contexto; el servidor comprueba el propietario del proyecto. En Administración el administrador mantiene gestión global. No se cambió la actividad «Nueva» ya capturada. Compilación correcta y 38/38 pruebas xUnit (incluye cinco casos nuevos de política); validación visual y SQL pendiente.

### Edición de proyectos (2026-10-22)

`ProjectForm` sirve para crear y editar. Usuario edita nombre y fecha meta de proyectos propios en `/mis-proyectos`; Administrador puede editar además iniciativa y responsable desde `/administracion`. `ProjectService.UpdateProjectAsync` aplica autorización y validación en servidor. No se modificaron datos existentes durante la implementación. Compilación correcta y 42/42 pruebas xUnit, con cuatro casos nuevos de permiso para editar. Interacción visual y persistencia SQL pendientes.

### Administración por secciones (2026-10-22)

Se añadieron botones para mostrar una sola categoría a la vez: Personas, Iniciativas, Proyectos, Actividades o Plantillas. Proyectos se abre por defecto; cada categoría conserva lista y formulario. No hubo migración ni cambios de datos. Compilación correcta y 42/42 pruebas; revisión visual pendiente.

### Confirmación para eliminar plantillas (2026-10-22)

El botón Eliminar de Administración abre un modal con el nombre de la plantilla, Cancelar y «Sí, eliminar». Solo la confirmación llama al servicio; Escape y clic en el fondo cancelan. Se muestran errores dentro del modal. Compilación correcta y 42/42 pruebas existentes; prueba visual pendiente.

### Histórico: editor único de plantilla seleccionada (2026-10-22; sustituido por `TemplateForm`)

Esta etapa intermedia usó un selector y un formulario solo para editar elementos; `TemplateService.UpdateItemsAsync` aplicaba las modificaciones. El formulario único de creación/edición descrito abajo sustituyó ambos. Las actividades generadas siguen independientes. En esa etapa hubo compilación correcta y 46/46 pruebas xUnit; revisión visual y SQL quedaron pendientes.

### Un formulario de creación/edición por sección (2026-10-22; reemplaza el diseño anterior)

Administración muestra el formulario arriba y la lista abajo en los cinco módulos. Editar una fila carga el mismo formulario; guardar o cancelar vuelve al modo de creación. Personas, iniciativas y actividades tienen nuevos métodos de actualización; plantillas ahora comparten `TemplateForm` para crear y editar nombre, categoría y elementos, y se eliminó el formulario antiguo de texto y el selector separado. Proyectos reutiliza `ProjectForm`. Se preservan registros, planes, dependencias y actividades ya generadas. Compilación correcta y 49/49 pruebas xUnit; interacción visual y SQL pendientes.

## Redefinición completa (Etapas 1 y 2)

El usuario decidió descartar el modelo anterior y reconstruir según `Project_Manager_Notas_Diseno.md`. Se eliminaron dominio, servicios, páginas, migraciones y pruebas anteriores; se borró la base `projectManager` y se recreó vacía.

- Modelo de dominio nuevo (10 entidades): `Person`/`PersonWorkday`, `Initiative`, `Project`, `Activity`, `ActivityDependency`, `WeeklyPlan`, `PlanBlock`, `WorkRecord`, `Template`, `TemplateItem`. Sin fechas obligatorias en actividades.
- `ProjectManagerDbContext` con restricciones CHECK, índice único `WeeklyPlan(PersonId, WeekStart)` y semilla del Administrador (Persona Id=1; jornada inicial ilustrativa Lun–Sáb 7.5 h, corregida después a 8/8/8/8/8/5/0 con la migración `WorkweekEightFive`).
- Migración `InitialModel` (20261021010653) en `src/RaceIT.ProjectManager/Migrations/`, aplicada sobre base vacía.
- Login provisional con cookie; `WorkAccess` con roles Administrador/Usuario y métodos estáticos puros para pruebas.
- Servicios: `PersonService`, `ProjectService`, `ActivityService` (crear actividad sin fechas + dependencias con validación de ciclos + horas pendientes) y `PlanningRules`.
- Pantallas base: `/login`, `/mi-semana`, `/panorama`, `/administracion`.

Verificación: build 0 errores/0 advertencias; 19 pruebas xUnit correctas (`PlanningRulesTests`, `WorkAccessTests`). Sin verificación visual automatizada.

## Etapa 3: registro de tiempo (2026-10-21)

- `WorkRecordService`: iniciar/pausar/reanudar sesiones, registro manual, un solo contador activo por persona (iniciar otra actividad pausa la anterior) y completar explícito que cierra sesiones abiertas.
- `PlanningRules.SessionHours`: duración de sesión en horas decimales.
- `ManualTimeInput`: validación de registro manual.
- `/mi-semana`: filas seleccionables + detalle inferior con Iniciar/Reanudar/Pausar/Registrar tiempo/Completar, horas estimadas/registradas/pendientes y registros recientes (más adelante el detalle pasó a mostrarse en modal).
- Verificación: build 0/0 y 21 pruebas xUnit. Sin migración nueva (WorkRecords ya existía en InitialModel). Sin prueba visual ni interacción SQL automatizada.

## Etapa 4: Mi semana (2026-10-21)

- `WeekLayoutCalculator`: regla pura que coloca bloques consecutivos sobre el eje de horas laborables y calcula días, offsets y horas libres.
- `WeeklyPlanService`: obtener/crear plan semanal, agregar/quitar bloques, editar horas y reordenar.
- `/mi-semana`: navegación de semana, métricas (disponibles/planeadas/registradas/sin asignar), pool de actividades, timeline con barras proporcionales y línea "ahora", reordenamiento vertical y detalle inferior con registro de tiempo (más adelante el detalle pasó a mostrarse en modal).
- Reordenamiento vertical por arrastre implementado; la tabla separada y las flechas ↑/↓ se retiraron en favor del drag & drop nativo.
- Verificación: build 0/0 y 29 pruebas xUnit. Sin migración nueva (WeeklyPlans y PlanBlocks ya existían). Sin prueba visual ni interacción SQL automatizada.

## Etapa 5: panorama y riesgo (2026-10-21)

- `RiskCalculator`: regla pura de capacidad compartida — compara el trabajo pendiente de los proyectos con fecha meta contra la capacidad de una sola bolsa semanal; marca conflicto compartido e indicadores individuales.
- `PanoramaService`: carga la bolsa, proyectos e iniciativas de la persona, horas pendientes por proyecto y ejecuta el cálculo de riesgo.
- `/panorama`: métricas (bolsa/pendientes/proyectos/en conflicto), banner de "En riesgo por carga compartida" con exceso de compromiso y grupos iniciativa→proyecto con badges.
- Verificación: build 0/0 y 27 pruebas xUnit. Sin migración nueva. Sin prueba visual automatizada.

## Etapa 6: plantillas y continuaciones (2026-10-21)

- `TemplateService`: plantillas de carreras con semilla por defecto (Antes/Durante/Después), creación/eliminación y generación de actividades al pool con título "Título — Carrera Año".
- `TemplateRules`: regla pura de títulos generados.
- Continuaciones: entidad `Continuation` (migración `Continuations` aplicada), `ActivityService.ContinueActivityAsync` y `ListContinuationsAsync`; botón "Continuar" en el detalle de la actividad.
- Historial de semanas: disponible navegando con ← / → (los planes y registros persisten por semana); no hay movimiento automático de pendientes.
- Limitación documentada: la contabilidad de horas entre continuaciones y su efecto en el riesgo queda pendiente (aplazada por el usuario).
- Verificación: build 0/0 y 29 pruebas xUnit. Migración `Continuations` aplicada. Sin prueba visual automatizada.

## Etapa 7: Initiatives (antes "TV") (2026-10-21)

- La vista de solo lectura de iniciativas y proyectos se llama **Initiatives** (en inglés, por decisión del usuario), ruta `/initiatives`.
- Reutiliza `PanoramaService`; auto-refresco cada 30 s, botón de pantalla completa (`wwwroot/display.js`) y sello de última actualización.
- Se eliminó `/panorama` y el enlace del menú pasó a "Initiatives".
- Cuentas/roles: cubiertos por el login provisional con roles Administrador/Usuario y el alta de personas por el administrador; el login real sigue diferido.
- Verificación: build 0/0 y 29 pruebas xUnit. Sin prueba visual automatizada.

Pendientes diferidos (no bloquean): login real, drag & drop del pool, contabilidad de continuaciones, timeline visual con líneas punteadas y riesgo semanal.

## Datos de prueba (2026-10-21)

`DataSeeder` corre al arrancar en Development (idempotente): renombra el admin a **Edgar**, crea al empleado **Pablo** y, si no hay iniciativas, siembra Data Logger (PCB, Case interno, Selección de batería), el proyecto independiente Cajones de herramientas, actividades con dependencias, plan semanal, registros de tiempo y una continuación. Verificado con arranque local (login 200 y log "Datos de prueba sembrados").

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

