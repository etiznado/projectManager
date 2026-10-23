# Usuarios y planificación

Última actualización: 2026-10-23 (America/Denver). Refleja el modelo vigente de esfuerzo y plan semanal; el modelo anterior (matriz de permisos, TV por empleado, notificaciones) quedó descartado.

## Personas y sesión

- `Person`: Name, Email (opcional), Role (Administrador/Usuario), PlanningMode (Weekly/ByDeliverables), WeeklyCapacityHours, IsActive y Workdays.
- Login provisional: POST `/session/login` selecciona una persona activa por Id (sin contraseña), crea la cookie de sesión y redirige según rol (Administrador -> `/administracion`, Usuario -> `/mi-semana`). POST `/session/logout` cierra la sesión.
- El login real y el proveedor de identidad siguen pendientes (no elegido).

## Permisos

`WorkAccess` centraliza la autorización. Enum `WorkPermission`: `ReadOwnWork`, `ManageOwnWork`, `ManageProjects`, `ReadAll`, `ManageUsers`.

- Administrador: todos los permisos.
- Usuario: `ReadOwnWork` y `ManageOwnWork` (lo suyo y lo asignado).

Usuario puede crear proyectos independientes en `/mis-proyectos` y actividades propias desde Mi semana; solo Administrador crea iniciativas o asocia un proyecto a una iniciativa. Los servicios verifican estas reglas al guardar y filtran los proyectos disponibles al empleado. Las predecesoras no se capturan en el formulario de actividad por ahora.

Usuario puede editar nombre y fecha meta de un proyecto que tiene asignado. Solo Administrador puede editar también su iniciativa o responsable. `UpdateProjectAsync` comprueba propiedad y campos permitidos en servidor; cambiar responsable de proyecto no reasigna actividades existentes.

Métodos clave: `IsAdmin`, `PersonIdOf`, `Allows`, `RequireAuthenticated`, `RequireAdmin`, `RequireOwn`.

## Planificación por persona

- Jornada por persona: `PersonWorkday` (PersonId + DayOfWeek + WorkingHours). Capacidad semanal por defecto 45 h.
- Jornada confirmada: 8 h lun-vie, 5 h sáb, 0 h dom (45 h/sem). Aplicada a Edgar y Pablo.
- Administración permite configurar la jornada de lunes a domingo en el mismo formulario de alta y edición de Personas. La bolsa semanal es independiente: cambiarla no modifica las horas diarias y la suma puede diferir de ella. Una persona nueva inicia con 8/8/8/8/8/5/0, editable antes de guardar.
- `WeeklyPlan` único por (PersonId, WeekStart); `PlanBlock` referencia una actividad con posición y horas planeadas.
- Un responsable por proyecto y por actividad, inicialmente quien los crea; reasignación por el administrador.

## Semilla de desarrollo (DataSeeder)

- Edgar (Administrador, ByDeliverables) y Pablo (Usuario, Weekly).
- Iniciativa "Data Logger" (PCB, Case interno, Selección de batería), proyecto independiente "Cajones de herramientas", actividades sueltas (cambio de aceite, impresora) y una continuación.

## Pendientes

- Login real (proveedor de identidad no elegido).
- Contabilidad de continuaciones y tolerancias de desfase de riesgo.
