# Tech context

## Stack comprobado

- SDK .NET 10.0.402; target net10.0.
- ASP.NET Core 10 / Blazor Interactive Server.
- EF Core SqlServer y Design 10.0.13; herramienta local dotnet-ef 10.0.13.
- Referencia explícita System.Security.Cryptography.Pkcs 10.0.13 para evitar avisos de vulnerabilidad de la dependencia transitiva anterior.
- SQL Server Express, SSMS y VS Code con extensiones C# y C# Dev Kit.

## Ubicación y permisos

Repositorio real: C:\Users\etizn\source\repos\projectManager.
El chat conservó cwd C:\Users\etizn\OneDrive\Junk\New project incluso después de cambiar carpeta en el proyecto. Las operaciones sobre el repositorio han requerido sandbox_permissions=require_escalated. Comprobar contexto/permisos de cada sesión y usar workdir explícito. No crear otra solución en la carpeta antigua ni pedir repetidamente que el usuario reinicie Codex: eso no resolvió el acceso; la escalación autorizada sí lo permitió.

## Base local

Servidor localhost\SQLEXPRESS; base projectManager; autenticación integrada de Windows. Configuración en appsettings.Development.json. Encrypt=True y TrustServerCertificate=True solo en desarrollo local. No se requieren contraseñas en archivos.

El usuario creó la base vacía y se aplicaron InitialProjectModel, DependencyScheduling ActivityDurationTypes y FlexibleProjectCalendars. Calendarios iniciales: Id 1 = 40 h/semana (lunes-viernes), Id 2 = 45 h/semana (incluye sábado 5 h). Hay datos reales capturados por el usuario: no asumir base vacía.

## Comandos

```powershell
dotnet build RaceIT.ProjectManager.sln
dotnet run --project src/RaceIT.ProjectManager
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/RaceIT.ProjectManager
```

VS Code: F5 con RaceIT.ProjectManager (Blazor). HTTP http://localhost:5208. Shift+F5 detiene. .vscode/launch.json y tasks.json ya configurados.

Si el proceso de depuración bloquea la DLL, usar salida de verificación para compilar sin interrumpirlo:

```powershell
dotnet build RaceIT.ProjectManager.sln --no-restore -p:BaseOutputPath=bin/verification/
```

La última revisión no tuvo navegador automatizable disponible. No presentar verificaciones de HTTP/compilación como pruebas visuales de interacción.

## Hosting

Usuario tiene HostGator Windows Enterprise Plan; compatibilidad con .NET 10 y conexiones persistentes no confirmada. Usuario decidió no preocuparse por hosting por ahora. No cambiar el stack por ese tema.

## Pruebas persistentes

dotnet test RaceIT.ProjectManager.sln ejecuta 66 pruebas xUnit (cronograma, milestones, bloqueos, TV, tipos de duración y flexibilidad) sin requerir SQL Server. La suite fue agregada a la solución.




## Vista TV

Ruta /tv/{Id}. Refresco 30 s, rotación 15 s. Pantalla completa requiere gesto del usuario o F11. La interfaz está pensada para 1920 × 1080 y también se adapta a ventanas menores. Las fechas de hoy y horas mostradas usan el reloj local del servidor de desarrollo; revisar zona horaria al desplegar.



Calendario Id 3: Flexible / sin horario, IsFlexible=true, sin horas por día. FlexibleProjectCalendars ya aplicada localmente.


## Verificación del acceso dummy

Usar /login tras iniciar con F5. Sesión cookie de ocho horas, no persistente al cerrar navegador; sin contraseña. Para verificar por HTTP se ejecutó una instancia temporal en localhost:5217, fuera del sandbox para usar claves DPAPI y SQL integrado; se detuvo al concluir. El sandbox no pudo descifrar claves DPAPI del usuario y no tuvo acceso al EventLog; no se modificó la configuración de protección de claves para sortearlo.
