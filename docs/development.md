# Entorno de desarrollo

Guía de trabajo local y límites de verificación. Para el perfil sintético repetible y las credenciales privadas, consultar [arranque local](local-test.md). Para alcance, aprobaciones y evidencia de una feature, consultar su issue y el [flujo](../features/README.md).

## Stack y estructura

- SDK .NET 10; solución `RaceIT.ProjectManager.sln` y target `net10.0`.
- ASP.NET Core / Blazor Interactive Server, EF Core SQL Server y Design. Las versiones concretas están en los `.csproj`.
- Aplicación: `src/RaceIT.ProjectManager`; pruebas xUnit: `tests/RaceIT.ProjectManager.Tests`.
- Migraciones: `src/RaceIT.ProjectManager/Migrations`, junto a su snapshot.
- SQL Server Express para integración local en Windows. La configuración privada determina el destino; no inferirlo por nombres de proyecto o ejemplos.

## Compilar y verificar

Desde la raíz:

```text
dotnet build RaceIT.ProjectManager.sln
dotnet test RaceIT.ProjectManager.sln
```

La suite usa reglas puras y fixtures sintéticos de EF/HTTP; no necesita la instancia de SQL Server del usuario. En Codex Cloud usar el SDK .NET 10 sin asumir acceso a Windows. El número de pruebas y sus resultados pertenecen a la evidencia de cada ejecución, no a esta guía.

Si MSBuild paralelo falla sin diagnóstico útil, repetir con `-m:1`. Cuando una instancia tenga bloqueada la salida normal y esté autorizado compilar, elegir una salida separada, sin detenerla:

```text
dotnet build RaceIT.ProjectManager.sln -m:1 -p:OutputPath=bin/verification/
dotnet test RaceIT.ProjectManager.sln -m:1 -p:OutputPath=bin/verification/ --no-build
```

Mantener la misma salida entre build y test. No lanzar el segundo comando con binarios anteriores tras un build fallido. Si el sandbox impide restaurar paquetes o conectar con testhost, informar el error y usar el mecanismo de permisos necesario; no atribuir PASS a una ejecución sin resultados.

Una edición exclusivamente documental puede verificarse con revisión de enlaces/referencias y `git diff --check`, sin compilar ni ejecutar pruebas de la aplicación. Si Edgar pide conservar una instancia de prueba, no reiniciarla ni modificar sus binarios, archivos servidos, configuración o datos. Un cambio Razor en fuente no se refleja en una instancia iniciada desde DLL hasta compilar y reiniciar; ambos pasos requieren respetar la autorización vigente.

## Cambios únicamente visuales

Aplicar por defecto la [vía abreviada de cambios visuales](../features/README.md#visual-only-fast-path) para ajustes de presentación de una UI existente: colores, tipografía, espaciado, bordes, dimensiones o barras de scroll, conservando el comportamiento. No repetir el ciclo de roles ni la suite completa de la solución, ni añadir pruebas que repliquen CSS.

Revisar el diff, compilar el proyecto o sus assets cuando haga falta (por ejemplo, CSS aislado o Razor) y comprobar en navegador el resultado, los tamaños relevantes y las interacciones afectadas. Informar los checks realmente ejecutados y cualquier comprobación pendiente; los resultados anteriores no acreditan la nueva revisión. Cambios de acciones, eventos, validación, permisos, servicios o persistencia siguen el flujo normal.

Se conserva la revisión UI requerida de Edgar. Las autorizaciones de datos, SQL, arranque/reinicio y publicación siguen vigentes; reutilizar las ya otorgadas para el mismo destino y operación.

## Base local y migraciones

La conexión EF se recibe como `ConnectionStrings:ProjectManager`. Puede proceder de configuración de entorno o del perfil privado validado; comprobar siempre servidor y catálogo efectivos antes de conectar. No publicar cadenas con credenciales, tokens, certificados privados ni archivos de `.local-test/`.

No crear/aplicar migraciones ni crear, borrar, recrear o modificar bases o datos sin autorización explícita. Una autorización para la base sintética no permite intervenir la base habitual ni producción. Preparar archivos de migración y aplicar el esquema son permisos distintos; confirmar ambos y el destino cuando sean necesarios. Revisar el delta antes de aplicarlo y conservar los registros existentes.

El arranque actual no llama a `DataSeeder` ni migra automáticamente; el modelo EF contiene datos iniciales que pueden formar parte de una migración. Revisar siempre el código de arranque y las operaciones de la migración, sin asumir que iniciar la app o actualizar esquema carece de efectos.

Solo con destino y operación autorizados, el arranque normal se realiza con:

```text
dotnet run --project src/RaceIT.ProjectManager
```

Para el ejercicio privado, usar los scripts y las condiciones de [local-test.md](local-test.md); no sustituirlos por el perfil Development. El launcher conserva la credencial y el certificado, no crea bases ni repara el esquema y dispone de parada dirigida a su propio proceso. No detener procesos por nombre o puerto.

## Identidad y configuración

Google requiere habilitación y `ClientId`/`ClientSecret` externos al repositorio. El selector sintético se limita a `PrivateQa` y al destino autorizado por `AuthenticationGate` o el perfil privado. Bootstrap de invitaciones y configuración del conector son tareas del operador; la disponibilidad del código no habilita una identidad real.

Las claves Google son `Authentication:Google:Enabled`, `Authentication:Google:ClientId` y `Authentication:Google:ClientSecret`. El selector requiere `Authentication:Selector:Enabled` y `Authentication:Selector:SyntheticDataOnly`, además de las comprobaciones de entorno, cliente y destino. El guard dedicado sin perfil privado acepta únicamente la instancia `(localdb)\RaceIT_PM_SyntheticQA` y el catálogo `RaceIT_PM_SyntheticQA`; el perfil repetible valida su propia tupla independiente. Ninguno autoriza usar otras bases.

El comando de operador `--issue-bootstrap-invitation` recibe `RACEIT_BOOTSTRAP_PERSON_ID`, `RACEIT_BOOTSTRAP_EMAIL`, `RACEIT_BOOTSTRAP_ISSUER`, `RACEIT_BOOTSTRAP_SUBJECT` y `RACEIT_BOOTSTRAP_EXPIRES_UTC`. Proporcionar valores reales únicamente en el entorno privado autorizado y manejar el token emitido como secreto; no copiarlo a Git, documentación o logs. Esta descripción no autoriza emitir una invitación ni modificar una cuenta.

La autenticación y los permisos del conector se documentan en las guías de [Frentes abiertos](jarvis-open-fronts-api.md) y [planificación](jarvis-planning-api.md). No guardar el token original en código, logs o documentación; conservar la credencial existente salvo autorización de rotación.

## Verificación visual

Build, pruebas unitarias y respuestas HTTP no acreditan interacción visual. Si no se hizo revisión de navegador sobre la revisión correspondiente, reportarla como no ejecutada. El perfil de prueba y los datos sintéticos requieren sus propias autorizaciones; no arrancar contra una base existente para improvisar un preview.

`app.css` se enlaza con `@Assets["app.css"]` en `App.razor`; al verificar cambios CSS considerar la caché del navegador. Los renders de [referencia de diseño](product-design.md#renders-de-referencia) son ejemplos, no evidencia de QA de la versión actual.
