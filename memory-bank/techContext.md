# Tech context

## FEAT-003: configuración aislada

`Authentication:Google:Enabled` y `Authentication:Google:ClientId`/`ClientSecret` habilitan Google; los valores reales se proporcionan fuera del repositorio. `Authentication:Selector:Enabled` y `Authentication:Selector:SyntheticDataOnly` solo habilitan el selector con entorno `PrivateQa`, cliente loopback y cadena SQL Server que apunte exactamente a la instancia `(localdb)\RaceIT_PM_SyntheticQA` y base `RaceIT_PM_SyntheticQA`. La instancia, la base y los datos ficticios del canario se crearon bajo autorización separada; no son la base del dueño. Sin configuración ambos accesos quedan cerrados. La migración `GoogleAccess` se aplicó solo a ese canario; no ejecutar `database update` contra la base del dueño en esta fase. El bootstrap privado de operador usa `--issue-bootstrap-invitation` y las variables `RACEIT_BOOTSTRAP_PERSON_ID`, `RACEIT_BOOTSTRAP_EMAIL`, `RACEIT_BOOTSTRAP_ISSUER`, `RACEIT_BOOTSTRAP_SUBJECT`, `RACEIT_BOOTSTRAP_EXPIRES_UTC` con expiración UTC; no se ha ejecutado con identidad real.

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

## Compilar y probar

Desde la raíz con un SDK .NET 10:

~~~text
dotnet build RaceIT.ProjectManager.sln
dotnet test RaceIT.ProjectManager.sln
~~~

En Codex Cloud, verifica que el entorno tenga el SDK .NET 10. La suite no necesita SQL Server ni acceso al SQL Express de Windows.

## Arranque e integración SQL en Windows

Solo para desarrollo local con una base de desarrollo revisada. La app puede sembrar datos en Development: no la inicies contra una base existente o compartida sin verificar el destino. Codex no ejecuta estos pasos SQL sin autorización explícita. Cualquier creación/aplicación de migraciones o intervención sobre la base se decide y se ejecuta de forma manual.

~~~powershell
dotnet run --project src/RaceIT.ProjectManager

# Solo al autorizar una actualización SQL y confirmar el destino:
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet tool restore
dotnet ef database update --project src/RaceIT.ProjectManager
~~~

VS Code: F5 con RaceIT.ProjectManager (Blazor). HTTP http://localhost:5208. Si el proceso bloquea la DLL, usa una salida de compilación separada: `dotnet build RaceIT.ProjectManager.sln -p:BaseOutputPath=bin/verification/`.


## Pruebas

- 69 pruebas xUnit en `tests/RaceIT.ProjectManager.Tests` (incluyen reglas de planeación, permisos, diseño semanal con carriles uniformes, agrupación del pool, riesgo, plantillas, horas de bloque y validación de jornada diaria), sin requerir SQL Server.
- `dotnet test RaceIT.ProjectManager.sln` no depende de la base.

## Verificación visual

Si el entorno no tiene navegador o interfaz disponibles, reporta la revisión visual como pendiente. No presentar compilación/HTTP como prueba visual de interacción. `app.css` se enlaza con `@Assets["app.css"]` en `App.razor`; en Development el nombre no lleva huella de contenido, así que tras editar CSS conviene pedir una recarga forzada (Ctrl+F5) antes de juzgar un cambio visual, para no confundirlo con una hoja de estilos en caché.

### Jornada confirmada de 45 h (2026-10-20)

El usuario confirmó 8 h de lunes a viernes, 5 h el sábado y 0 el domingo. Sustituye la jornada ilustrativa de 7.5 h/día. Esta fue la regla inicial de creación para capacidad 45 h; desde el 2026-10-23, la jornada se captura explícitamente en Personas y no cambia automáticamente con la bolsa. DataSeeder reutiliza la regla inicial; semilla del modelo actualizada. Migración WorkweekEightFive cambia jornadas anteriores de 7.5 h en personas de capacidad 45 h sin modificar bloques ni registros. Aplicada en SQL local mediante transacción y registrada en __EFMigrationsHistory; lectura posterior confirmó Edgar/Pablo con 8/8/8/8/8/5/0. Build 0 errores/advertencias y 29 pruebas existentes correctas; revisión visual pendiente.
