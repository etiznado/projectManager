# Project brief

Última actualización: 2026-10-22 (America/Denver). Alineado con el modelo vigente de esfuerzo y plan semanal (`Project_Manager_Notas_Diseno.md`).

## Identidad

- Compañía: RaceIT.
- Repositorio: projectManager, remoto https://github.com/etiznado/projectManager.git.
- Solución y namespace raíz: RaceIT.ProjectManager.
- Idioma de interacción e interfaz: español.

## Objetivo

Herramienta de project management de alto nivel para mostrar en una TV de 32 pulgadas Full HD (1920 x 1080) el estado de iniciativas y proyectos y el plan semanal del empleado, con avance real y alertas de riesgo. El administrador y el empleado deben poder entender cómo va el trabajo.

## Alcance acordado (modelo vigente)

- Iniciativa -> proyecto -> actividad. Un proyecto puede ser independiente (sin iniciativa) y una actividad puede ser suelta (sin proyecto).
- Crear una actividad no exige fechas de inicio/fin: se registra su esfuerzo estimado y sus dependencias antes de programarla. Proyectos e iniciativas tienen fecha meta cuando corresponda.
- El empleado reparte una bolsa semanal de horas de referencia entre actividades en Mi semana (plan semanal con bloques).
- Registro de tiempo real (sesiones y captura manual), terminación explícita y alertas de riesgo por carga compartida.
- Pantalla de iniciativas/proyectos para TV con actualización automática; administración desde computadora.
- Desarrollo local primero. Hosting fuera del alcance actual por decisión del usuario.

## Último acuerdo

Jornada de referencia confirmada de 45 h semanales: 8 h de lunes a viernes, 5 h el sábado y 0 el domingo; configurable por persona.

## Modelo anterior descartado

Se descartó por completo el enfoque previo (cronograma por fechas/Gantt, milestones, bloqueos, calendarios por proyecto y TV por iniciativa), junto con su código y base de datos. Las notas históricas de este documento (milestones, plan original protegido, TV por iniciativa) ya no aplican.
