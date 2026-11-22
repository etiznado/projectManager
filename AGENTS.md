# Instrucciones para Codex: RaceIT.ProjectManager

## Antes de cambiar algo

- Empieza por [memory-bank/README.md](memory-bank/README.md). Lee el diseño vigente, el contexto activo y el progreso; consulta los patrones y el contexto técnico según la tarea.
- Contrasta la documentación con el código, las pruebas y, si corresponde, las migraciones. Distingue lo implementado, lo probado y lo pendiente. No inicies trabajo pendiente sin que el usuario lo pida.
- Mantén las convenciones del repositorio: namespace `RaceIT.ProjectManager`; .NET 10, Blazor Interactive Server, EF Core y SQL Server.
- Conserva el plan original y no edites las horas planeadas originales de las actividades. Distingue horas laborables, esperas de 24 horas y fechas manuales; no confundas plan con pronóstico. Edita actividades en modal, conserva el contexto seleccionado y refresca la lista al cerrar.

## Verificación y límites

Desde la raíz, con el SDK .NET 10:

```text
dotnet build RaceIT.ProjectManager.sln
dotnet test RaceIT.ProjectManager.sln
```

- Las pruebas de la solución no requieren SQL Server. En Codex Cloud usa build y pruebas sin asumir que existe la instancia local de Windows.
- La app y la integración SQL local usan SQL Server Express en Windows. No inicies la app contra una base existente sin comprobar el destino: en Development puede sembrar datos.
- No crees ni apliques migraciones, ni borres, recrees o cambies bases o datos, sin autorización explícita. Los pasos SQL en el contexto técnico son para ejecución manual.
- Al terminar un cambio material, actualiza solo las notas afectadas (normalmente `activeContext.md` y `progress.md`), preserva el historial y reporta qué verificaste. No guardes credenciales ni reemplaces datos del usuario por datos de ejemplo.

## Flujo de features

Para una feature registrada, usa el [flujo de features](features/README.md) y su expediente. Invoca los roles de Codex por fase; valida QA y la aprobación de la UI si aplica. La aprobación técnica no autoriza publicar ni fusionar un PR.
