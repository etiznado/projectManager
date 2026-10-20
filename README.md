# RaceIT.ProjectManager

Herramienta de gestión de proyectos de RaceIT, con administración y dashboard para TV Full HD.

## Stack

- .NET 10 / ASP.NET Core.
- Blazor Web App con Interactive Server.
- SQL Server Express y Entity Framework Core 10.
- Namespace raíz: RaceIT.ProjectManager.

## Desarrollo local

Requiere el SDK de .NET 10.

```powershell
dotnet build RaceIT.ProjectManager.sln
dotnet run --project src/RaceIT.ProjectManager
```

La consola muestra la URL local. También se puede abrir RaceIT.ProjectManager.sln en Visual Studio.

## Alcance acordado

- Iniciativas con múltiples proyectos y actividades principales.
- Plan original, pronóstico y fechas reales separados.
- Dependencias entre actividades, incluso de distintos proyectos.
- Milestones con requisitos y fechas objetivo, estimadas y reales.
- Calendario por proyecto: horas laborables por día; puede incluir sábado de 5 horas.
- Duración en horas laborables, distinta del esfuerzo del equipo.
- Actividades pendientes, en curso o completadas; bloqueos independientes.
- Salud del cronograma: en tiempo, en riesgo o atrasado.
- Vista TV de una iniciativa con timeline y desviación de entrega.

## Estado

Conexión local configurada en appsettings.Development.json para localhost\SQLEXPRESS, base projectManager, con autenticación de Windows. TrustServerCertificate se utiliza solo para desarrollo local. La página inicial permite comprobar la conexión sin crear tablas. La migración InitialProjectModel crea las tablas del dominio y los calendarios de 40 y 45 horas semanales. Incluye captura y edición de actividades en modal. Incluye motor de dependencias y propagación del pronóstico; incluye dashboard TV; autenticación sigue pendiente.

## Ejecutar y depurar en VS Code

Abrir la carpeta raíz del repositorio. Instalar la extensión C# Dev Kit de Microsoft (incluye el depurador C#).
En Run and Debug seleccionar `RaceIT.ProjectManager (Blazor)` y presionar F5.
La tarea compila en Debug y abre el navegador en http://localhost:5208 al iniciar el servidor.
Los breakpoints permiten depurar el código C# del servidor. Shift+F5 detiene la aplicación.



## Migraciones

Desde la raíz del repositorio:

```powershell
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/RaceIT.ProjectManager
```

Las migraciones se aplican explícitamente, no al iniciar la aplicación. Detener la depuración antes de compilar o generar migraciones.

El modelo incluye Initiatives, Projects, Activities, ActivityDependencies, WorkCalendars, WorkCalendarDays, Milestones, MilestoneRequirements, Responsibles y Blockers. Las relaciones usan borrado restringido para evitar eliminar entregables accidentalmente. Los responsables pueden quedar sin asignar durante la planificación.

Las fechas usan DateOnly y las duraciones horas decimales. El motor de programación deberá conservar el consumo parcial de cada jornada al encadenar actividades. El estado general y las desviaciones se calcularán, no se guardan como valores independientes. El editor preserva el plan original. Las dependencias validan ciclos y alcance de iniciativa; los milestones tienen confirmación manual de cumplimiento.

## Captura inicial

Abrir `/administracion` desde el enlace en Inicio. Permite crear y seleccionar iniciativas y proyectos, asignar un responsable por nombre y elegir calendario. Al agregar una actividad se calcula el fin desde el inicio y la duración en horas; el pronóstico inicial es igual al plan. Al crear una actividad se calcula independientemente; sus dependencias se configuran después desde Editar. Los inicios en días no laborables se rechazan. La creación guarda los datos en SQL Server y muestra las actividades del proyecto seleccionado.

Esta versión permite creación, consulta y edición de actividades con actualización de estado. Los milestones se administran en su panel, debajo de las actividades. Los calendarios se seleccionan entre dos horarios y Flexible / sin horario; aún no se editan desde la interfaz.

## Edición de actividades

En la lista de actividades, seleccionar Editar. Permite cambiar nombre, responsable, visibilidad para TV, estado, inicio y duración estimados, y fechas reales. El fin estimado se recalcula con el calendario del proyecto. El plan original se muestra como referencia y no se modifica.

Pendiente no admite fechas reales; En curso requiere inicio real sin fin; Completada requiere ambas fechas reales. Se rechazan fechas reales futuras y fin anterior al inicio. La edición se abre en un modal. Guardar cierra el modal y refresca las actividades. Cancelar, la X o Escape cierran sin guardar. La iniciativa y el proyecto permanecen seleccionados; al cerrar se vuelve a consultar la lista. Los cambios recalculan el pronóstico de la iniciativa según sus dependencias.


## Contexto persistente

Consultar [memory-bank/README.md](memory-bank/README.md) para los requisitos, decisiones técnicas, estado actual y pendientes. [AGENTS.md](AGENTS.md) establece cómo mantener este contexto.


## Dependencias y pronóstico

En Editar actividad, marcar las actividades de las que depende. Pueden pertenecer a diferentes proyectos de la misma iniciativa. Guardar conserva el plan original y recalcula las sucesoras; desmarcar elimina la dependencia. La vista previa muestra el inicio/fin calculados. Inicio solicitado es la fecha más temprana permitida, independiente del resultado de las dependencias.

Se rechazan ciclos y autorreferencias. Todas las predecesoras deben terminar antes de iniciar la sucesora pendiente. El motor conserva horas parciales y utiliza el calendario receptor. Las actividades sin vínculos pueden avanzar en paralelo; no hay nivelación de recursos. Las iniciadas conservan su inicio real y las completadas liberan a sus sucesoras el día posterior al fin real, ya que no se captura hora real. El resumen muestra el máximo fin real/estimado y diferencia contra objetivo en días naturales; aún no calcula ruta crítica ni riesgo.

La migración DependencyScheduling conserva el inicio estimado existente como inicio solicitado y agrega desplazamientos internos de horas laborables. Para aplicar en otro entorno, ejecutar el comando de migraciones indicado arriba.

## Pruebas

```powershell
dotnet test RaceIT.ProjectManager.sln
```

Suite de 66 pruebas de cronograma, milestones, bloqueos, TV, tipos de duración y proyectos flexibles, sin SQL Server. La integración SQL se verificó aparte con registros temporales identificados y limpiados al terminar.

## Milestones

Seleccionar iniciativa y proyecto en administración. En el panel Milestones, usar Nuevo milestone, capturar nombre/objetivo y seleccionar actividades requeridas (al menos una). Se pueden combinar proyectos de la misma iniciativa. La fecha estimada se calcula con el último fin requerido y se actualiza al editar actividades.

Cuando todos los requisitos están completados, usar Confirmar e indicar fecha real y nombre de quien acepta la entrega. No puede ser una fecha futura ni anterior al último fin real. La definición queda protegida hasta Reabrir. Reabrir una actividad requerida o mover su fin real después de la aceptación retira la confirmación del milestone automáticamente. Los nombres de confirmadores son captura manual; todavía no hay autenticación ni historial de aprobaciones.

Los formularios se abren en modal, conservan las selecciones y refrescan el panel/resumen al guardar. No se requiere una migración adicional para esta función.

## Bloqueos

En administración, seleccionar proyecto y usar Nuevo bloqueo. Capturar actividad, descripción y responsable opcional. Los bloqueos abiertos se pueden editar o resolver; activar Mostrar también los resueltos para consultar los cerrados o reabrirlos si la actividad sigue sin completar.

Resolver conserva las fechas y el estado de la actividad. Para reflejar un cambio de entrega, editar su pronóstico. Antes de completar una actividad o confirmar un milestone deben resolverse sus bloqueos abiertos. No se permiten nuevos bloqueos en completadas; reabrir primero la actividad si es necesario.

La lista de actividades separa ejecución y salud del cronograma: atraso tiene prioridad; sin atraso, un bloqueo abierto indica En riesgo. Se muestra el total abierto en la iniciativa sin asumir automáticamente un cambio de entrega. El riesgo aún no se propaga a sucesoras ni se calcula ruta crítica. No se requiere migración adicional.

## Dashboard para TV

1. Seleccionar una iniciativa en administración y pulsar Ver en TV.
2. Se abre /tv/{Id} en otra pestaña; pulsar Pantalla completa o usar F11.
3. La vista consulta SQL cada 30 segundos y rota páginas cada 15 segundos.

Timeline SVG agrupado por proyecto con actividades principales, plan original punteado, línea HOY, milestones y dependencias de la página actual. Todas las actividades y entregas cuentan para métricas, incluso las ocultas. Ocho filas de timeline y dos milestones/bloqueos por tarjeta en cada página, para mantener legibilidad en 32 pulgadas Full HD.

Muestra objetivo, entrega estimada (real si completada), desviación en días naturales y salud global. En riesgo alerta sobre bloqueos abiertos o pronósticos vencidos; no calcula ruta crítica. Ante error conserva última lectura y muestra aviso de desactualización. La vista no modifica datos. No requiere migraciones nuevas.

La lógica y el renderizado estático se verificaron; queda validación visual del modo Full HD, pantalla completa y refresco periódico en el navegador del usuario.

## Trabajo y tiempos de espera

Al crear una actividad, elegir Horas laborables o Días naturales (espera / proveedor). Las horas respetan el calendario del proyecto; los días naturales cuentan 24 horas continuas, incluidos fines de semana. Cambiar el selector conserva el número mostrado y cambia su unidad; revisar la duración.

Ejemplo PCB: Enviar orden (1 hora laborable) → Fabricación y entrega (10 días naturales) → Recibir e inspeccionar (2 horas laborables). Configurar las dependencias desde Editar. Si la llegada cae fuera del calendario receptor, la inspección espera al siguiente intervalo laborable. La regla previa de actividades completadas con fecha real sin hora sigue liberando sucesoras desde el día siguiente.

En edición se puede cambiar el tipo y duración estimados, conservando tipo, duración y fechas del plan original. La lista muestra ambas duraciones con sus unidades; la TV identifica esperas. Días son duración total prevista, no días restantes.

La migración ActivityDurationTypes agrega los tipos y mantiene todas las actividades existentes como laborales sin convertir ni modificar sus fechas/duraciones. Aplicarla con dotnet ef database update en otros entornos.

## Proyectos flexibles y edición de proyectos

Seleccionar un proyecto y pulsar Editar proyecto. El modal permite cambiar nombre, responsable y calendario; conserva iniciativa/proyecto y refresca datos al guardar.

Para trabajo personal sin horario, elegir Flexible / sin horario. Las actividades usan Fechas manuales: inicio y fin, cualquier día, sin horas semanales. Las esperas del proveedor siguen usando Días naturales. Las dependencias desplazan el intervalo de fechas conservando su duración en días; con precisión de fecha una actividad manual termina al final de su día y libera sucesoras el siguiente.

Cambiar un proyecto a flexible convierte sus estimaciones laborales en fechas manuales, conservando el plan original y reales. Las esperas no cambian de tipo. Cambiarlo después a un horario deja sus actividades manuales como están hasta editarlas; no se inventan horas de trabajo.

La migración FlexibleProjectCalendars agrega el calendario sin horas y el fin solicitado, sin cambiar fechas/duraciones existentes. Para registrar una orden realizada el sábado: elegir calendario flexible, editar la actividad con inicio/fin solicitados del sábado, estado Completada e inicio/fin reales de ese sábado.
