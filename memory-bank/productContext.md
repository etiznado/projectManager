# Product context

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

## Acceso provisional disponible

/login permite seleccionar Administrador provisional o un responsable existente como Empleado, sin contraseña. No hay registro. Cambiar usuario / Salir elimina la sesión y regresa a selección. Es un acceso de pruebas locales: cualquier persona que tenga acceso puede seleccionar al administrador. No usar como autenticación real.

Administrador conserva administración y TV; Empleado entra a Mi trabajo, consulta asignaciones y contexto de dependencias directas y edita sus actividades en modal. No puede editar trabajo ajeno, reasignar, modificar visibilidad TV ni cambiar dependencias ajenas. Las demás operaciones de autoservicio siguen pendientes según activeContext.md.


## Uso

La administración permite capturar y mantener proyectos; el dashboard de TV permitirá ver entregas y desvíos sin interacción. El render aprobado es una dirección visual, con fondo azul oscuro, timeline dominante y panel de milestones e impacto. Sus datos eran ilustrativos; no usarlos como datos reales ni como cronograma validado.

## Reglas acordadas

- Separar plan original, pronóstico y fechas reales.
- Duración laboral en horas o espera en días naturales; ninguna equivale al esfuerzo sumado de personas.
- Calendario por proyecto: lunes a viernes 8 h/día; algunos proyectos incluyen sábado 5 h. Domingo 0 h. Sin feriados ni horarios de entrada/salida por ahora.
- Dependencia inicial prevista: fin a inicio. El proyecto receptor respeta su propio calendario.
- Actividad: Pendiente, En curso o Completada. Bloqueo como condición independiente.
- Salud prevista: En tiempo si entrega estimada cumple objetivo; En riesgo si todavía cumple pero existe una amenaza relevante; Atrasado si estimado supera objetivo. El motor de evaluación aún no existe.
- Un atraso en una actividad no implica automáticamente atraso de toda la iniciativa; deben considerarse dependencias y margen.
- Milestone: resultado o entrega de duración cero, con objetivo, estimado y fecha real. Puede requerir varias actividades y aprobación.
- Mostrar en el dashboard de TV solo controla visibilidad. Las actividades ocultas siguen formando parte del proyecto y deberán contar en sus cálculos.
- Responsables opcionales durante captura.

## Edición implementada

- Plan original protegido. Duración estimada es duración total prevista, no horas restantes.
- Pendiente: sin fechas reales. En curso: inicio real requerido y fin vacío. Completada: ambas fechas reales requeridas.
- Fechas reales no futuras; fin real no anterior al inicio. Se permiten fechas reales en días no laborables.
- Inicio laboral debe ser laborable y usa calendario del proyecto. La espera en días naturales puede comenzar cualquier día.
- Modal de actividad: Guardar cierra y refresca; Cancelar/X/Escape cierran sin guardar; conservar iniciativa y proyecto seleccionados.

## Dependencias implementadas

El campo Inicio solicitado (no antes de) fija un límite manual; el inicio y fin estimados se calculan con todas las predecesoras. Retirar vínculos o acortar duraciones permite adelantar, sin retroceder más allá del inicio solicitado. Sin dependencias las actividades son paralelas; no se considera disponibilidad de personas todavía.

Actividades ya iniciadas no se desplazan por nuevas dependencias. Una completada libera sucesoras desde el día posterior a su fin real porque no existe hora de término real. El resumen de iniciativa compara la última fecha (real si completada, estimada en otros casos) con el objetivo en días naturales; no calcula holguras ni estado En riesgo.

## Milestones implementados

Un milestone pertenece a un proyecto y requiere una o varias actividades, incluso de otros proyectos de la misma iniciativa. No tiene duración. La fecha estimada es el máximo de fines requeridos; se usa fin real de completadas. El objetivo se captura y puede cambiar mientras no esté confirmado.

Completadas todas las actividades, queda Listo para confirmar. La aceptación es manual, capturando fecha real y nombre. No se permite confirmar antes del último fin real ni con fecha futura. Un confirmado debe reabrirse para cambiar definición; si se reabre una actividad requerida, se retira automáticamente la confirmación. Reabrir limpia las aprobaciones actuales; aún no existe historial de estas acciones.

El resumen de entrega de iniciativa también considera fechas estimadas/reales de milestones, para reflejar aceptación posterior a la terminación de actividades.

## Bloqueos implementados

Un problema abierto pertenece a una actividad y tiene descripción y responsable opcional para resolver. Se registran creación y resolución. Resolver no completa actividad ni cambia fechas. Reabrir conserva creación y limpia resolución actual; no hay historial de reaperturas todavía.

No se crean/reabren bloqueos en completadas. Para completar una actividad hay que resolver primero sus bloqueos; los requisitos de milestones también deben estar libres de bloqueos para confirmar. Cada actividad muestra salud separada de estado de ejecución: atrasada si estimado supera plan o estimado ya pasó sin terminar, en riesgo si tiene bloqueo abierto sin estar atrasada. La alerta de iniciativa muestra cantidad abierta sin afirmar atraso global. El efecto de bloquear una predecesora sobre riesgo de sucesoras/ruta crítica sigue pendiente.

## TV implementada

Abrir Ver en TV desde la iniciativa activa. Una pantalla con objetivo, pronóstico/real, desviación y estado, timeline de principales y paneles de próximos milestones y bloqueos abiertos. Los datos ocultos en el timeline siguen contando para el resultado.

Refresco 30 s y rotación 15 s; se usan páginas para preservar legibilidad. Milestones/bloqueos se muestran de dos en dos, actividades ocho por página. Error de lectura muestra última actualización y conserva datos anteriores. Estado global En riesgo es una alerta por bloqueos abiertos o estimados vencidos sin terminar, no una confirmación de atraso; el atraso se compara contra objetivo. Iniciativa completada requiere todas las actividades completas y todos sus milestones confirmados.

## Trabajo y espera del proveedor

Para PCB separar Enviar orden (1 h laborable), Fabricación y entrega (por ejemplo 10 días naturales) y Recibir e inspeccionar (horas laborables), enlazadas por dependencias. Poner una espera en curso significa seguimiento del pedido, no trabajo continuo del responsable.

Un día natural son 24 h continuas, incluidos sábados y domingos. El inicio del plazo puede conservar una hora parcial de su predecesora. Si el pedido llega fuera de la jornada receptora, la siguiente tarea laboral espera al próximo intervalo de su calendario. Se mantiene la regla conservadora de liberar al día siguiente cuando solo se conoce una fecha de fin real.

Plan original y pronóstico conservan tipos de duración separados. Los registros anteriores siguen como laborales; se puede cambiar el tipo estimado desde Editar sin cambiar el plan original.

## Trabajo personal sin horario

El usuario trabaja en algunos proyectos sin horarios ni horas semanales. Estos usan Flexible / sin horario y actividades Fechas manuales: inicio/fin directos, cualquier día, sin calcular fechas desde horas. No confundir con disponibilidad de 24 horas laborales ni con espera continua de proveedor.

Las dependencias pueden desplazar el intervalo manual conservando su cantidad de días entre inicio/fin. Con fechas sin hora se libera al siguiente día al terminar. Horas de esfuerzo opcionales no tienen campo propio todavía.

Se pueden editar nombre, responsable y calendario del proyecto seleccionado mediante botón Editar proyecto y modal. Pasar a flexible convierte pronósticos laborales en intervalos manuales, conserva plan original/reales y deja esperas continuas intactas. Volver a horario no convierte manuales a horas automáticamente.

## Ampliación acordada, todavía sin implementar

Usuarios, permisos, consulta de dependencias directas y avisos relevantes al administrador se detallan en users-and-work-planning.md. También se describe la propuesta de actividades independientes y planes por empleado/período, diferenciada de las reglas aprobadas. La vista TV de iniciativa permanece como alcance existente. No tratar la planificación como funcionalidades disponibles ni como autorización para implementar.

