# Tech context

## Stack comprobado

- SDK .NET 10.0.402; target net10.0.
- ASP.NET Core 10 / Blazor Interactive Server.
- EF Core SqlServer y Design 10.0.13; herramienta local dotnet-ef 10.0.13 (restaurada con `dotnet tool restore`).
- System.Security.Cryptography.Pkcs 10.0.13 (evitar avisos de dependencia transitiva).
- SQL Server Express, SSMS y VS Code.

## Base local

- Servidor `localhost\SQLEXPRESS`; base `projectManager`; autenticación integrada de Windows.
- Conexión en `appsettings.Development.json` (Encrypt=True, TrustServerCertificate=True solo en desarrollo).
- Base **recreada vacía** el 2026-10-21 por decisión del usuario; migraciones `InitialModel`, `Continuations` y `WorkweekEightFive` aplicadas; semilla del Administrador (Persona Id=1, jornada 8/8/8/8/8/5/0).
- Migraciones en `src/RaceIT.ProjectManager/Migrations/` (no `Data/Migrations`).

## Comandos

```bash
dotnet build RaceIT.ProjectManager.sln
dotnet test RaceIT.ProjectManager.sln
dotnet run --project src/RaceIT.ProjectManager
export ASPNETCORE_ENVIRONMENT=Development        # requerido para ef y base (conexión en Development)
dotnet ef migrations add <Nombre> --project src/RaceIT.ProjectManager
dotnet ef database update --project src/RaceIT.ProjectManager
dotnet ef database drop --force --project src/RaceIT.ProjectManager
```

VS Code: F5 con RaceIT.ProjectManager (Blazor). HTTP http://localhost:5208. Si el proceso bloquea la DLL, compilar con `dotnet build ... -p:BaseOutputPath=bin/verification/`.

## Pruebas

- 69 pruebas xUnit en `tests/RaceIT.ProjectManager.Tests` (incluyen reglas de planeación, permisos, diseño semanal con carriles uniformes, agrupación del pool, riesgo, plantillas, horas de bloque y validación de jornada diaria), sin requerir SQL Server.
- `dotnet test RaceIT.ProjectManager.sln` no depende de la base.

## Verificación visual

No se dispone de navegador automatizable. No presentar compilación/HTTP como prueba visual de interacción. `app.css` se enlaza con `@Assets["app.css"]` en `App.razor`; en Development el nombre no lleva huella de contenido, así que tras editar CSS conviene pedir una recarga forzada (Ctrl+F5) antes de juzgar un cambio visual, para no confundirlo con una hoja de estilos en caché.

### Jornada confirmada de 45 h (2026-10-20)

El usuario confirmó 8 h de lunes a viernes, 5 h el sábado y 0 el domingo. Sustituye la jornada ilustrativa de 7.5 h/día. Esta fue la regla inicial de creación para capacidad 45 h; desde el 2026-10-23, la jornada se captura explícitamente en Personas y no cambia automáticamente con la bolsa. DataSeeder reutiliza la regla inicial; semilla del modelo actualizada. Migración WorkweekEightFive cambia jornadas anteriores de 7.5 h en personas de capacidad 45 h sin modificar bloques ni registros. Aplicada en SQL local mediante transacción y registrada en __EFMigrationsHistory; lectura posterior confirmó Edgar/Pablo con 8/8/8/8/8/5/0. Build 0 errores/advertencias y 29 pruebas existentes correctas; revisión visual pendiente.
