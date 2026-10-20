# Active context

Actualizado: 2026-10-19 (America/Denver).

## Punto de continuidad

La solicitud actual es actualizar documentación con las decisiones de proyectos sin iniciativa y TV por empleado. No implementar estas ampliaciones ni aplicar migraciones en esta actualización. Los acuerdos de producto no convierten los pendientes en funcionalidades disponibles.

## Decisiones más recientes, pendientes de implementar

- Permitir proyectos sin iniciativa. Ejemplo: Cajones de herramienta contiene diseñar, comprar material, fabricar e instalar; no crear una iniciativa artificial para alojarlo.
- Conservar tres formas de organizar trabajo: iniciativa con proyectos; proyecto sin iniciativa con varias actividades; actividad independiente sin proyecto, como cambiar aceite al Tundra.
- Un proyecto sin iniciativa tendrá responsable, calendario, actividades, seguimiento y entrega estimada; aparecerá en Mi trabajo. El empleado podrá crearlo asignado a sí mismo y agregar actividades propias. El administrador podrá reasignarlo o vincularlo posteriormente con una iniciativa.
- Actividades de proyecto usan su calendario y cuentan para su entrega. Las independientes requieren fechas o calendario propios, detalle aún por definir, y no afectan la entrega de un proyecto.
- Conservar TV por iniciativa y agregar TV por empleado para consulta del administrador: trabajo activo asignado, combinando proyectos con/sin iniciativa y actividades independientes, agrupados por proyecto.
- TV por empleado mostrará fechas, avance, bloqueos y entrega estimada/estado por proyecto. Empezar sin exigir planes semanales o quincenales. Se contempla filtro de un proyecto para dedicarle una pantalla completa; diseño y ruta pendientes.
- Mantener permisos mínimos: empleados consultan lo asignado y contexto limitado de dependencias directas; TV completa solo para administrador. El alcance de cálculo de proyectos sin iniciativa y sus dependencias requiere diseño antes de migrar.

Detalle en users-and-work-planning.md y productContext.md.

## Implementado y verificado previamente

- Blazor Interactive Server .NET 10, EF Core/SQL Server; captura/edición, dependencias y propagación, milestones, bloqueos, TV por iniciativa, horas laborables, espera natural y proyectos flexibles.
- Login dummy /login por selección sin contraseña; Administrador sintético y responsables existentes como Empleados. Sesión cookie y POST con antiforgery para entrada/salida. Registro y autenticación real diferidos por el usuario.
- Cinco grupos en WorkAccess: Consultar mi trabajo, Gestionar mi trabajo, Administrar proyectos, Consultar todo y Administrar usuarios. Empleado recibe los dos primeros; Administrador, los cinco. La asignación vigente determina edición, no la autoría. Los grupos no significan que todos sus flujos estén terminados.
- Administración/Home/TV restringidos a Administrador; /mi-trabajo consulta proyectos/actividades asignados y dependencias directas; editor de actividades propias. Responsable de proyecto no puede editar actividades ajenas. Empleado no reasigna ni cambia visibilidad TV ni vínculos con trabajo ajeno.
- Última verificación reportada: build sin errores/advertencias, 71 pruebas (66 anteriores y 5 de autorización), HTTP con SQL local de login admin/empleado, restricción de Administración/TV y logout. Sin prueba visual ni interacción Blazor automatizada.
- Última migración existente: FlexibleProjectCalendars. Project.InitiativeId y ProjectActivity.ProjectId siguen siendo obligatorios; no hay todavía proyectos sin iniciativa ni actividades independientes. TV actual solo tiene /tv/{Id} por iniciativa.

## Prioridad sugerida, no ejecución automática

Completar Gestionar mi trabajo: crear actividades propias desde Mi trabajo, bloqueos propios, crear proyectos propios (incluidos sin iniciativa) y actividades independientes; después TV por empleado. Antes de implementar altas, considerar el modelo opcional acordado para evitar iniciativas/proyectos artificiales. El orden detallado debe ajustarse a la siguiente solicitud del usuario.

Pendientes adicionales: editar nombre de proyecto propio, habilitar iniciativas para creación por empleado, selección de personas por ID en formularios administrativos, cuentas, historial/notificaciones y planes por períodos. No existe botón Nueva actividad para empleado actualmente.

## Límites y entorno

Conservar datos reales, asignaciones y plan original protegido; no editar horas planeadas ni implementar aprobación/desbloqueo del plan. Repositorio confirmado: C:\Users\etizn\source\repos\projectManager. Comprobar procesos y git en cada sesión. No se ejecutaron pruebas nuevas en esta actualización documental ni se cambió código o SQL.

Ruta crítica/holguras, riesgo propagado, feriados, esfuerzo opcional y concurrencia siguen pendientes. Hosting fuera del alcance actual.
