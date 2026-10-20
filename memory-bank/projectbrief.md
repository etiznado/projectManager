# Project brief

## Identidad

- Compañía: RaceIT.
- Repositorio: projectManager, remoto https://github.com/etiznado/projectManager.git.
- Solución y namespace raíz: RaceIT.ProjectManager.
- Idioma de interacción e interfaz: español. El usuario prefiere el término milestones.

## Objetivo

Herramienta de project management de alto nivel para mostrar en una TV de 32 pulgadas Full HD (1920 × 1080) el cronograma y la salud de una iniciativa. Debe permitir reconocer si la entrega va en tiempo, en riesgo o atrasada, y cuánto se desvía del plan.

## Alcance acordado

- Iniciativa → proyectos → actividades. Ejemplo: Data Logger → PCB, case y batería.
- Actividades secuenciales y paralelas con fechas y dependencias, incluso entre proyectos de una iniciativa.
- Administración desde computadora; pantalla TV de consulta con actualización automática como objetivo.
- Una iniciativa por pantalla y solo actividades principales visibles. Una vista de varias iniciativas o grupos colapsables es una posibilidad posterior.
- Timeline tipo Gantt, línea de hoy, comparación contra plan original, próximos milestones y bloqueos relevantes.
- Desarrollo local primero. Hosting fuera del alcance actual por decisión del usuario.

## Último acuerdo

Las horas y fechas planeadas permanecen sin edición por ahora. No implementar aprobación de plan ni desbloqueo de la línea base: se propuso, pero el usuario pidió dejarlo como está.

## Ampliación acordada, pendiente de implementación (2026-10-19)

Permitir proyectos sin iniciativa y actividades independientes sin proyecto. Ejemplo: Cajones de herramienta es un proyecto con varias actividades, sin iniciativa artificial. Añadir TV por empleado para el administrador, combinando trabajo activo de proyectos con/sin iniciativa y actividades independientes; conservar la TV por iniciativa. La primera TV por empleado no dependerá de planes por períodos. Detalles y estado real en activeContext.md y users-and-work-planning.md.
