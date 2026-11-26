# Documentación de RaceIT.ProjectManager

Punto de consulta para las reglas del producto, la arquitectura y la operación. Empezar por [AGENTS.md](../AGENTS.md) al trabajar con Codex y leer aquí solo los documentos que correspondan a la tarea.

| Documento | Contenido |
| --- | --- |
| [Diseño funcional](product-design.md) | Modelo de esfuerzo, plan semanal, registro de tiempo, capacidad y acuerdos de interacción |
| [Arquitectura](architecture.md) | Dominio, servicios, identidad, permisos y patrones de implementación |
| [Entorno de desarrollo](development.md) | Stack, build, pruebas, SQL y límites al trabajar con una instancia en uso |
| [Arranque local privado](local-test.md) | Perfil sintético, scripts, TLS, credencial existente y configuración privada |
| [API de Frentes abiertos](jarvis-open-fronts-api.md) | Contrato y autorización del conector existente |
| [API de planificación](jarvis-planning-api.md) | Iniciativas, proyectos, actividades, planes y aprobaciones sensibles |
| [Flujo de features](../features/README.md) | Roles, artefactos, aprobaciones y publicaciones permitidas |

## Fuentes y mantenimiento

- El issue suministrado es el registro canónico del alcance aprobado, criterios de aceptación, cambios de alcance autorizados, bloqueos que requieren a Edgar y un único resumen final. Consultar [issues de Project Manager](https://github.com/etiznado/projectManager/issues) para el estado de cada feature.
- El diseño detallado de una feature, traspasos, revisión y evidencia de ejecución se conservan en Codex o, cuando esté autorizado y sea útil, en el PR.
- El código, las pruebas y las migraciones muestran qué se implementó. Una prueba pasada no acredita otros proveedores, revisión visual, despliegue o activación de una instancia.
- `docs/` conserva documentación duradera. Actualizar únicamente los contratos, reglas o instrucciones que cambien; no añadir bitácoras de fases, conteos de pruebas o una copia del estado de los issues.
- Una limitación descrita no autoriza a implementarla. Consultar el issue y las instrucciones de Edgar antes de iniciar trabajo pendiente.
- Los registros antiguos y documentos reemplazados se pueden consultar en el historial Git. Los expedientes históricos de `features/` se conservan sin cambios.

Esta organización consolida el antiguo contexto del repositorio en diseño, arquitectura y entorno; elimina la obligación de mantener diarios de contexto y progreso. No contiene credenciales ni datos operativos privados.
