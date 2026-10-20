# Planificación: usuarios y trabajo por períodos

## Decisiones de proyectos y TV (2026-10-19; sin implementar)

El usuario pidió conservar estas decisiones en el memory bank:

- Iniciativa opcional para un proyecto; no crear agrupaciones artificiales. Cajones de herramienta es un proyecto sin iniciativa, con actividades de diseñar, comprar material, fabricar e instalar.
- Actividad de proyecto y actividad independiente son casos distintos. La primera usa calendario del proyecto y participa en su entrega; la segunda no requiere proyecto ni iniciativa, tiene fechas/calendario propios por detallar y no afecta entregas de proyectos.
- Proyectos sin iniciativa conservan responsable, calendario, actividades, seguimiento y entrega estimada. Empleado puede crearlos para sí mismo; administrador puede reasignarlos o vincularlos después a una iniciativa. No cambia el permiso sobre actividades ajenas.
- Mi trabajo reunirá proyectos asignados con/sin iniciativa y actividades independientes. Actualmente solo permite consulta y edición de actividades existentes; falta Nueva actividad y demás altas de autoservicio.
- Conservar TV por iniciativa y añadir TV por empleado, accesible al administrador, con todo el trabajo activo asignado: proyectos con/sin iniciativa y actividades independientes. Agrupar por proyecto y mostrar fechas, avance, bloqueos y entrega estimada/estado de cada proyecto.
- Iniciar TV por empleado sin depender de planes semanales/quincenales. Se contempla filtro de un proyecto para pantalla dedicada; ruta y diseño aún pendientes.
- Próximo desarrollo sugerido: completar Gestionar mi trabajo (actividades propias, bloqueos, proyectos propios e independientes), contemplando estas relaciones opcionales, y TV por empleado. Esta actualización autoriza documentación, no implementación automática.

Estado comprobado en código: Project.InitiativeId y ProjectActivity.ProjectId siguen siendo int obligatorios; MyWork no tiene altas y TV solo ofrece /tv/{Id} por iniciativa. Última migración existente FlexibleProjectCalendars. No se cambió código, SQL ni migraciones ni se ejecutaron pruebas nuevas para documentar estos acuerdos.

Pendientes de diseño: alcance de dependencias/recálculo sin iniciativa, calendario de independientes, vinculación posterior de proyecto a iniciativa y alcance exacto al mezclar responsables en TV. No asumir que todos los proyectos sin iniciativa pertenecen a un mismo grafo.

## Acuerdos posteriores y alcance actual

El usuario autorizó implementar progresivamente con menores permisos y aprobó cinco grupos: Consultar mi trabajo, Gestionar mi trabajo, Administrar proyectos, Consultar todo y Administrar usuarios. Empleado recibe los dos primeros; Administrador, los cinco. La asignación actual, no la autoría, determina edición. Responsable de proyecto no edita actividades ajenas. Empleado cambia dependencias solo entre dos actividades propias; enlaces con otra persona, solo Administrador. Bloqueos propios se propusieron pero no recibieron aprobación individual antes de adoptar la agrupación mínima.

Solicitud actual: login dummy por selección sin contraseña; registro y autenticación real después. Implementación parcial de permisos como base de acceso: ver activeContext.md y progress.md. No confundir los cinco grupos definidos con todas sus pantallas terminadas. Los apartados históricos de abajo conservan el estado previo a esta autorización.


Actualizado: 2026-10-19 (America/Denver).

## Estado y límite de autorización

Documento de planificación. Nada de esta fase está implementado. El usuario pidió explícitamente: «todavia no implementes los cambios, vamos a planear bien». La autorización posterior para actualizar el memory bank solo autoriza documentación; no código, paquetes, migraciones ni cambios en SQL.

## Necesidad

Gestionar el trabajo de empleados combinando proyectos que duran varias semanas con actividades independientes: cambio de aceite al Tundra, instalar base de impresora o fabricar base de TV. No crear un proyecto artificial por cada semana. Mantener la vista TV de una iniciativa existente; se propuso una vista operativa separada por empleado/período. Se mostró un render conceptual, sin implementación.

## Acuerdos funcionales

- Sustituir captura libre del responsable por selección de personas del sistema.
- Un empleado puede crear proyectos y actividades asignados solamente a él mismo, incluidas actividades independientes.
- Puede actualizar su trabajo sin aprobación previa del administrador. Los límites precisos de cada acción se deben terminar de definir.
- El administrador puede consultar y administrar todo y asignar/reasignar trabajo a cualquier persona.
- El empleado ve sus proyectos y actividades asignadas, incluso si abarcan varias semanas.
- También consulta actividades ajenas con dependencia directa respecto de las suyas, predecesoras y sucesoras. Ve responsable, estado, fechas y bloqueos relevantes para la relación.
- Esa consulta no da permiso de editar trabajo ajeno, ver todo el proyecto ni recorrer automáticamente toda la cadena de dependencias.
- Roles extensibles mediante permisos por acción y alcance; comenzar con Administrador y Empleado, dejando posible Supervisor posteriormente. No codificar todas las reglas como comparaciones fijas de nombres de rol.
- El administrador recibe notificaciones solo de cambios relevantes; no se requiere aprobación para que el cambio surta efecto.

## Propuesta de organización de trabajo, por detallar

- Una actividad independiente no requiere proyecto.
- Un plan de trabajo pertenece a una persona y un período: semana, catorcena de 14 días con fecha de referencia o quincena del 1 al 15 / 16 al último día.
- Puede reunir actividades independientes y referencias a actividades de proyectos. No duplicar actividades al incluirlas en un período.
- Un proyecto puede continuar durante varios períodos. Distinguir el proyecto asignado de los compromisos seleccionados para el período.
- Al trasladar trabajo pendiente, conservar la misma actividad, el compromiso original y su historial.
- Quedan por precisar calendario de períodos, compromisos, arrastre y presentación exacta de la vista del empleado.

## Personas, cuentas y permisos: propuesta técnica

Separar persona asignable, cuenta de acceso y roles/permisos. Una persona puede existir sin acceso; una cuenta autenticada identifica quién realiza una operación. Distinguir creador de responsable asignado. El autoservicio del empleado requiere autenticación; ocultar botones no sustituye la autorización en el servidor.

Conservar responsables y asignaciones actuales al migrar. No fusionar personas automáticamente solo porque coincida el nombre. La tecnología exacta de autenticación, el alta inicial del administrador y la administración de cuentas no están decididas.

Los permisos deben expresar acción y alcance (propio/asignado/todo según corresponda). Falta resolver cómo se relacionan propiedad, creación y asignación si el administrador reasigna un proyecto o una actividad.

## Historial y notificaciones

Registrar actor, fecha/hora, objeto y cambios anteriores/nuevos. Mantener historial incluso para cambios menores. No confundir responsable del trabajo con usuario que hizo la modificación.

Cambios relevantes acordados para notificar al administrador:
- Creación de proyecto.
- Cambios de fechas o duración estimadas.
- Cambios de dependencias.
- Nuevo bloqueo o reapertura.
- Confirmación o reapertura de milestone.

Correcciones menores de nombre/descripción y visibilidad en TV: historial sin notificación. Un recálculo que afecta varias actividades produce una notificación agrupada con el impacto, no una por actividad. Se propuso bandeja dentro de la aplicación con no leídas; canal y detalles todavía por confirmar. Falta decidir si cambios realizados por el propio administrador y cambios de estado/finalización también generan avisos.

Los cambios derivados del motor de cronograma sobre trabajo ajeno se registran como impacto de la operación autorizada; no otorgan al empleado permiso general para editarlo. Propuesta: persistir historial y evento de notificación de forma consistente con la operación (misma transacción o patrón equivalente), con identificador común para agrupar impactos. Aún no hay esquema aprobado.

## Revisión de estructura existente (solo lectura)

- Una aplicación Blazor Interactive Server; no hace falta dividirla en microservicios para esta fase.
- Domain/ProjectEntities.cs contiene Responsible (Id, Name, Email), sin vínculo a login, roles ni auditoría. Las asignaciones existentes ya usan ResponsibleId.
- Los formularios/servicios resuelven responsables por nombre o los crean; el confirmador del milestone también se captura por nombre.
- Program.cs y Routes.razor no tienen autenticación/autorización; Routes usa RouteView.
- ProjectActivity.ProjectId es obligatorio; no existe modelo de actividad independiente ni de plan de trabajo por período.
- Administration.razor crea iniciativas/proyectos/actividades directamente con DbContext. Otras mutaciones pasan por ProjectService, ActivityPlanningService, MilestoneService y BlockerService. Centralizar escrituras antes de aplicar permisos/auditoría de manera uniforme.
- También existen lecturas directas desde componentes; limitar consultas en el servidor, incluidos acceso por ID, selectores, dependencias y dashboards.
- ProjectService y ActivityPlanningService pueden recalcular actividades de toda la iniciativa; considerar ese impacto al autorizar y notificar.
- TvDashboardService lee la iniciativa completa sin aislamiento de usuario. Definir acceso de pantalla TV antes de introducir privacidad; no conceder acceso global implícito a empleados.
- Conservar plan original protegido, datos reales y reglas del motor existentes. La suite existente tiene 66 pruebas; no se volvió a ejecutar durante esta revisión documental.

## Decisiones pendientes para el siguiente chat

1. Matriz detallada de permisos: crear iniciativas/proyectos, asignaciones, cambios de calendario, actividades, dependencias, bloqueos, confirmación de milestones y administración de personas/cuentas.
2. En qué iniciativas puede crear proyectos el empleado; si puede crear iniciativas propias.
3. Qué permite ser responsable de un proyecto respecto de actividades asignadas a otros; efecto de reasignar sobre permisos del creador.
4. Quién puede crear/modificar una dependencia con actividad ajena y qué contexto de consulta se muestra sin exponer el resto del proyecto.
5. Mecanismo de login, personas sin acceso, desactivación y conservación del historial.
6. Acceso de TV y de la futura vista de trabajo por empleado.
7. Detalles de períodos/compromisos/arrastre y calendario de actividades independientes.
8. Cerrar reglas/canal de notificaciones y esquema de historial.

## Próximo paso

Continuar planificación y cerrar la matriz de permisos con ejemplos antes de diseñar migraciones o implementar. El usuario no ha autorizado implementar esta fase.

