# Memory bank · RaceIT.ProjectManager

Última actualización: 2026-10-21 (America/Denver).

Contexto persistente del proyecto. Describe acuerdos y estado comprobado; no sustituye al código ni convierte propuestas en requisitos aprobados.

## Orden de lectura

1. Project_Manager_Notas_Diseno.md: diseño vigente del MVP (fuente de verdad).
2. activeContext.md: última decisión y trabajo inmediato.
3. progress.md: implementado, verificado y pendiente.
4. systemPatterns.md: estructura y decisiones de implementación.
5. techContext.md: entorno, ejecución y base de datos.


Actualizar los archivos afectados al completar un cambio relevante o cuando el usuario cambie un acuerdo. Distinguir requisitos, implementación, verificación y propuestas. No almacenar contraseñas, tokens ni cadenas con credenciales. No afirmar que un flujo está verificado visualmente si solo se compiló.

## Continuidad de la planificación actual

Redefinición vigente: se descartó el modelo anterior (fechas/Gantt, milestones, bloqueos, TV por iniciativa) y se reconstruye según `Project_Manager_Notas_Diseno.md` (esfuerzo, plan semanal, registro de tiempo y alertas de riesgo).

Estado real en `activeContext.md` y `progress.md`: implementadas las Etapas 1–7 (confirmadas por el usuario), con login provisional. Arrastrar y soltar implementado posteriormente; ver pendientes y límites de verificación en activeContext.md y progress.md. Los documentos `projectbrief.md`, `productContext.md` y `users-and-work-planning.md` describen el modelo descartado; solo `projectbrief.md` conserva identidad vigente (compañía, repositorio, namespace, idioma).

Mensaje sugerido para un chat nuevo desde la carpeta del repositorio:

> Lee AGENTS.md y el memory bank, empezando por `Project_Manager_Notas_Diseno.md` y `activeContext.md`. El modelo anterior quedó descartado; no asumas que milestones, bloqueos o el motor de fechas existen.

