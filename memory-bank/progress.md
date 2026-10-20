# Progress

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

## Primera etapa de acceso dummy

Implementado login por selección sin contraseña, cookies de sesión, roles iniciales y cinco permisos agrupados. Administrador provisional sin insertar datos; empleados desde Responsible existente. Se restringieron rutas administrativas y TV, servicios de escritura y editor por asignación vigente. Nueva vista /mi-trabajo con consulta asignada y contexto directo ajeno sin enlaces de edición. No cambia el plan original.

Pruebas: 71 xUnit correctas y build sin errores/advertencias. HTTP con SQL local verificó entrada admin y empleado, rechazo de Administración/TV para empleado y salida. Sin prueba visual automatizada. No se aplicaron migraciones ni se cambiaron registros de negocio.

Pendiente de la matriz: altas propias de proyectos/actividades, edición de nombre propio de proyecto, interfaz de bloqueos del empleado, habilitación de iniciativas y gestión de personas/cuentas. Historial/notificaciones, actividades independientes y períodos tampoco implementados. Registro/autenticación real expresamente diferidos por el usuario.


Actualizado: 2026-10-19.

## Implementado

- Solución RaceIT.ProjectManager con Blazor Interactive Server .NET 10.
- F5/Run and Debug en VS Code, compilación previa y apertura del navegador.
- EF Core y conexión SQL Server con autenticación Windows, botón de comprobación.
- Modelo inicial, migración aplicada y diez tablas de dominio.
- Calendarios base de 40 y 45 horas.
- Captura de iniciativas, proyectos, responsables por nombre y actividades.
- Listado por proyecto; inicio/fin planeados y estimados, horas planeadas, estado y visibilidad.
- Edición de actividad, fin estimado calculado y protección del plan original.
- Modal nativo con cierre al guardar, Cancelar/X/Escape y refresco sin reiniciar selección.
- Memory bank e instrucciones de continuidad.

## Verificación realizada

- Compilaciones sucesivas: 0 errores y 0 advertencias, incluida la versión del modal.
- Conexión a projectManager confirmada por herramienta y usuario.
- Tablas, historial de migración, semillas de 40/45 horas y restricciones de calendario comprobados en SQL Server.
- EF reportó modelo sin cambios pendientes tras migración inicial.
- Pruebas temporales: 13 h desde viernes termina sábado con calendario 45 h y lunes con calendario 40 h; fracciones, jornada exacta, rechazo de domingo y duración cero.
- Escritura/lectura de iniciativa, proyecto y actividad en transacción revertida.
- Pruebas del editor: plan intacto, pronóstico recalculado, estados/fechas inválidos rechazados, actualización SQL y retiro de responsable; transacción revertida.
- /administracion y app.css respondieron HTTP 200 con formularios/estilos esperados antes del cambio a modal.
- Usuario confirmó captura y edición funcionando.
- Suite persistente: tests/RaceIT.ProjectManager.Tests, 66 pruebas xUnit (cronograma, milestones, bloqueos, TV, tiempos continuos y proyectos flexibles). Las pruebas anteriores del editor/calendario y las de integración SQL son programas temporales. Sin verificación visual automatizada del modal.

## Pendiente

- Dependencias y propagación implementadas; queda ruta crítica, holguras y diagnóstico de conflictos con ejecución real.
- Milestones y bloqueos implementados; pendiente propagación de riesgo hacia sucesoras y cálculo de holguras.
- Dashboard TV implementado; revisión visual y validación del refresco en navegador del usuario pendientes.
- Editar iniciativas y calendarios detallados; feriados y excepciones. Proyectos ya tienen edición.
- Autenticación y concurrencia.
- Duraciones planeada y estimada ya se muestran con su unidad (horas laborables o días naturales).
- Publicación/hosting y aprobación del plan, fuera del alcance actual.

No se han creado commits ni publicado cambios a GitHub durante este trabajo. Comprobar git status antes de asumir el estado actual del repositorio.

## Fase de dependencias completada

- Modal permite seleccionar actividades predecesoras de cualquier proyecto de la misma iniciativa; vista previa del inicio y fin calculados.
- Grafo validado por orden topológico: rechaza ciclos, autorreferencias y referencias fuera de la iniciativa.
- Servicio ActivityPlanningService guarda actividad, vínculos y pronóstico de todas las actividades de la iniciativa en una transacción Serializable.
- Motor conserva horas parciales por jornada y calendario receptor; sin asignación de recursos ni horarios de reloj.
- RequestedStart separa restricción manual de EstimatedStart calculado para poder adelantar al retirar dependencias.
- EstimatedStartHour y EstimatedFinishHour conservan desplazamientos internos dentro de la jornada.
- Migración DependencyScheduling inicializa RequestedStart desde EstimatedStart existente. No modifica plan original.
- Lista muestra predecesoras; resumen compara máximo fin real/estimado contra objetivo en días naturales. Todas las actividades participan, aunque estén ocultas en TV.
- 14 pruebas xUnit pasaron; prueba temporal SQL verificó cambios de predecesora, propagación, eliminación de vínculo, rechazo de ciclo/otra iniciativa y rollback de errores. Datos temporales limpiados por GUID.
- HTTP 200 de administración y editor con actividad existente; modelo sin cambios pendientes. No se verificó interacción visual automatizada.

## Fase de milestones completada

- MilestonePanel integrado por proyecto; creación/edición en modal y selección de actividades de toda la iniciativa.
- Requiere nombre, objetivo y al menos una actividad válida. Estimado calculado desde último fin requerido.
- Listado muestra objetivo, estimado, cumplimiento real, requisitos completados, estado y confirmador.
- Confirmación manual con fecha y nombre de quien acepta; exige todas las actividades completadas, fin real no posterior al cumplimiento y fecha no futura.
- Definición confirmada protegida hasta reabrir. Reapertura manual y automática si un requisito deja de estar completado o se modifica a un fin real posterior a la confirmación.
- ActivityPlanningService actualiza milestones en la misma transacción que el cronograma. Resumen de iniciativa incluye fines estimados/reales de milestones además de actividades.
- Sin cambio de esquema: reutiliza Milestones, MilestoneRequirements, ApprovedDate y ApprovedById.
- 25 pruebas persistentes pasaron. Integración temporal SQL y renderizado estático del panel pasaron; datos temporales limpiados por GUID. Sin verificación visual automatizada de interacción.

## Fase de bloqueos completada

- BlockerPanel por proyecto en administración, modales crear/editar con actividad, descripción y responsable opcional. Resolver/reabrir y filtro para ver resueltos.
- Servicio serializable limita operaciones al proyecto y rechaza actividades completadas para crear/reabrir. Resolver registra timestamp y conserva el cronograma.
- ActivityPlanningService rechaza completar actividades con bloqueos abiertos. MilestoneService rechaza confirmar requisitos bloqueados.
- Actividades muestran ejecución, salud del cronograma y conteo de bloqueos por separado. Se alerta el total abierto en la iniciativa. Milestones marcan riesgo por bloqueos requeridos y priorizan atraso si la fecha estimada excede objetivo.
- Modal de actividad muestra aviso de bloqueos abiertos. No se generan retrasos arbitrarios ni se cambian fechas al crear/resolver un bloqueo.
- Se corrigieron claves de componentes hermanos para distinguir paneles/modales por tipo e ID.
- Sin migración: se usa Blockers existente.
- 35 pruebas xUnit pasaron y programa temporal SQL verificó persistencia, edición, alcance, resolución/reapertura, protección de actividad/milestone y render estático del panel. Datos de prueba limpiados por GUID; sin prueba visual automatizada.

## Dashboard TV implementado

- /tv/{Id}, acceso Ver en TV en administración sin perder selección (nueva pestaña).
- Encabezado, objetivo, entrega real/estimada, desviación y estado general. Datos reales de SQL; sin datos ilustrativos insertados.
- SVG con barras agrupadas por proyecto, baseline punteada, HOY, milestones y conectores dentro de la página. Aviso de dependencias fuera de vista.
- Timeline rota 8 filas cada 15 segundos. Milestones y bloqueos pendientes rotan dos por tarjeta. Se actualizan consultas cada 30 segundos.
- Mantiene última lectura ante errores con advertencia de desactualización. Timer cancelado al salir. Pantalla completa mediante API del navegador con F11 como alternativa.
- Actividades ocultas participan en cálculos y alertas. Riesgo global alerta por bloqueos o estimados vencidos; no calcula ruta crítica.
- 46 pruebas xUnit pasaron. Programa temporal de integración SQL y HtmlRenderer verificó dashboard y segunda página, recarga y caso inexistente; fixtures limpiados. Sin verificación visual de navegador, fullscreen ni timer real en browser por indisponibilidad de CUA.
- Sin migración nueva.

## Tiempos de proveedor implementados

- Captura y edición distinguen horas laborables de días naturales continuos. Se aceptan días decimales con paso de 0.01.
- Plan y pronóstico tienen tipos separados. La lista muestra Duración planeada y Duración estimada con sus unidades.
- Motor usa 24 h/día para espera, conserva horas parciales entre tipos y aplica calendario receptor al volver a trabajo.
- Selector de tipo mantiene el valor numérico mostrado; se debe revisar duración al cambiar unidad.
- TV indica Espera y usa fracciones de día de 24 h comunes en barras para alinear dependencias de tipos distintos; baseline usa tipo/duración originales.
- Migración ActivityDurationTypes aplicada con default laboral para registros existentes y restricción de valores. Fechas/duraciones anteriores verificadas intactas.
- 56 pruebas pasaron. Integración SQL y HTTP comprobó cambios de plazo y tipo, baseline conservado y formularios/TV. Datos de prueba limpiados. Modelo sin cambios pendientes; sin verificación visual automatizada.

## Proyectos editables y flexibles

- Botón Editar proyecto visible al seleccionar en dropdown. Modal con nombre, responsable y calendario, cierre/cancelación y refresco sin perder selección.
- ProjectService guarda en transacción y recalcula iniciativa/milestones al cambiar calendario.
- Calendario Flexible / sin horario sin filas horarias. Actividades nuevas usan inicio/fin manuales por defecto; días naturales disponibles para proveedores.
- Tipo ManualDates permite cualquier día con cero horas; RequestedFinish conserva el intervalo solicitado antes de dependencias. La propagación desplaza por días conservando ese intervalo.
- Conversión a flexible transforma solo estimaciones laborales, protege baseline y reales. Volver a horario conserva manuales hasta cambio explícito.
- Migración FlexibleProjectCalendars aplicada y datos existentes verificados intactos. 66 pruebas pasaron. Integración SQL, modal estático y HTTP de actividad verificados; fixtures limpiados. Sin verificación visual automatizada.

## Planificación posterior: usuarios y trabajo por períodos

Se documentaron acuerdos en users-and-work-planning.md. No implementados: autenticación, roles/permisos, historial, notificaciones, actividades independientes ni planes por empleado. El usuario pidió planear antes de implementar y luego autorizó únicamente esta documentación.

Revisión de estructura realizada en modo lectura: Responsible no es usuario autenticado; ProjectId de actividad obligatorio; hay escrituras y lecturas directas en componentes que deberán centralizarse/limitarse para permisos. Los recálculos pueden afectar trabajo ajeno dentro de la iniciativa.

Visibilidad acordada: empleado consulta trabajo asignado y dependencias directas en ambos sentidos, sin editar trabajo ajeno ni acceso transitivo/global. Cambios propios sin aprobación previa y notificaciones al administrador solo para eventos relevantes, agrupando recálculos.

En esta fase documental no se modificó código ni SQL, no se aplicaron migraciones ni se ejecutaron nuevamente las pruebas. Próximo paso: terminar matriz de permisos y decisiones pendientes del documento de planificación.

