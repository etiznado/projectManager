# Memory bank · RaceIT.ProjectManager

Última actualización: 2026-10-19 (America/Denver).

Contexto persistente del proyecto. Describe acuerdos y estado comprobado; no sustituye al código ni convierte propuestas en requisitos aprobados.

## Orden de lectura

1. projectbrief.md: objetivo y alcance.
2. activeContext.md: última decisión y trabajo inmediato.
3. progress.md: implementado, verificado y pendiente.
4. productContext.md: uso y reglas del negocio.
5. systemPatterns.md: estructura y decisiones de implementación.
6. techContext.md: entorno, ejecución y base de datos.

Actualizar los archivos afectados al completar un cambio relevante o cuando el usuario cambie un acuerdo. Distinguir requisitos, implementación, verificación y propuestas. No almacenar contraseñas, tokens ni cadenas con credenciales. No afirmar que un flujo está verificado visualmente si solo se compiló.

## Continuidad de la planificación actual

Últimas decisiones documentadas: proyectos sin iniciativa, actividades independientes y TV por empleado para el administrador, sin exigir planes por períodos. Estas ampliaciones están pendientes de implementación. La última solicitud solo autorizó actualizar documentación.

Después de activeContext.md, leer users-and-work-planning.md. El usuario autorizó una primera implementación con login dummy por selección sin contraseña. Consultar los apartados nuevos al principio de esos documentos para distinguir acceso y permisos básicos ya implementados del autoservicio, historial, notificaciones y períodos pendientes.

Mensaje sugerido para un chat nuevo desde la carpeta del repositorio:

> Lee AGENTS.md y el memory bank, especialmente activeContext.md y users-and-work-planning.md. Revisa el alcance implementado del acceso dummy y los pendientes de permisos antes de continuar. No asumas que toda la matriz está implementada.

