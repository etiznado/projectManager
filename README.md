# RaceIT Project Manager

Herramienta de RaceIT para planear trabajo por esfuerzo y por semana, registrar el tiempo real y detectar riesgos de capacidad. Usa .NET 10, Blazor Interactive Server, EF Core y SQL Server.

## MVP actual

- **Mi semana** (`/mi-semana`): bolsa de actividades agrupada por proyecto, bloques de trabajo en un timeline semanal y detalle en modal para registrar tiempo o ajustar las horas del bloque.
- **Iniciativas** (`/initiatives`): panorama de iniciativas y proyectos, métricas y alertas de riesgo.
- **Mis proyectos** (`/mis-proyectos`): consulta y edición de proyectos propios; creación de proyectos independientes.
- **Administración** (`/administracion`): personas, iniciativas, proyectos, actividades y plantillas.
- **Frentes abiertos** (`/frentes-abiertos`): seguimiento personal y actualización de estado, avance y próximo paso mediante la API privada.

El cronograma anterior por fechas, los milestones, los bloqueos y el dashboard de TV por iniciativa quedaron descartados. El timeline semanal actual pertenece al modelo de esfuerzo descrito en el [diseño funcional](docs/product-design.md). La autenticación Google, el selector sintético y los permisos se explican en [arquitectura](docs/architecture.md#identidad-y-permisos); su disponibilidad en código no acredita activación en producción.

## Compilar y probar

Con el SDK .NET 10, desde la raíz del repositorio:

```text
dotnet build RaceIT.ProjectManager.sln
dotnet test RaceIT.ProjectManager.sln
```

La suite no necesita SQL Server. Para ejecutar la app o comprobar integración SQL en la PC Windows, consulta el [entorno de desarrollo y sus límites](docs/development.md) y el [arranque local privado](docs/local-test.md). Codex Cloud puede compilar y probar con el SDK .NET 10, pero no debe asumir acceso a la instancia de Windows.

## Trabajar con Codex

[AGENTS.md](AGENTS.md) es el punto de entrada para Codex. El [índice de documentación](docs/README.md) reúne reglas del producto, arquitectura, contratos y operación. Los [issues](https://github.com/etiznado/projectManager/issues) conservan alcance, criterios, decisiones y resumen final de cada feature; los artefactos detallados y la evidencia permanecen en Codex o en un PR autorizado. Contrastar siempre la documentación con el código y las pruebas; una idea pendiente no es trabajo autorizado.

Consulta el [flujo de features](features/README.md) para coordinar Requirements Analyst, Architect, Developer, Reviewer y QA, con los límites de publicación y aprobaciones correspondientes. No se mantienen bitácoras de features en el repositorio; los expedientes históricos existentes se conservan.
