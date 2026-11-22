# RaceIT Project Manager

Herramienta de RaceIT para planear trabajo por esfuerzo y por semana, registrar el tiempo real y detectar riesgos de capacidad. Usa .NET 10, Blazor Interactive Server, EF Core y SQL Server.

## MVP actual

- **Mi semana** (`/mi-semana`): bolsa de actividades agrupada por proyecto, bloques de trabajo en un timeline semanal y detalle en modal para registrar tiempo o ajustar las horas del bloque.
- **Iniciativas** (`/initiatives`): panorama de iniciativas y proyectos, métricas y alertas de riesgo.
- **Mis proyectos** (`/mis-proyectos`): consulta y edición de proyectos propios; creación de proyectos independientes.
- **Administración** (`/administracion`): personas, iniciativas, proyectos, actividades y plantillas. El acceso por persona y rol es provisional; no es autenticación de producción.

El cronograma anterior por fechas, los milestones, los bloqueos y el dashboard de TV por iniciativa quedaron descartados. El timeline semanal actual pertenece al nuevo modelo de esfuerzo. Consulta el [diseño vigente](memory-bank/Project_Manager_Notas_Diseno.md) y el [estado de implementación](memory-bank/activeContext.md).

## Compilar y probar

Con el SDK .NET 10, desde la raíz del repositorio:

```text
dotnet build RaceIT.ProjectManager.sln
dotnet test RaceIT.ProjectManager.sln
```

La suite no necesita SQL Server. Para ejecutar la app o comprobar integración SQL en la PC Windows, consulta la [configuración local y precauciones](memory-bank/techContext.md#base-local). Codex Cloud puede compilar y probar con el SDK .NET 10, pero no debe asumir acceso a la instancia de Windows.

## Trabajar con Codex

[AGENTS.md](AGENTS.md) es el punto de entrada para Codex. El [memory bank](memory-bank/README.md) conserva decisiones, contexto activo y verificaciones entre tareas. Ante diferencias, contrasta esas notas con el código y las pruebas; no trates una idea pendiente como trabajo autorizado.

Consulta el [flujo de features](features/README.md) para preparar expedientes y coordinar Arquitectura, Desarrollo, QA y Revisión con Codex.
