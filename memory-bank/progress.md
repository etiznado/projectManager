# Progress

## Publicación autorizada tras aceptación — issue #16 (2026-11-25)

Edgar probó y aceptó el resultado/UI, anunció disponibilidad para commit/merge y finalmente ordenó commit, push y apertura de PR a main. Comprobados rama `codex/feat-016-jarvis-api`, diff, exclusión de archivos privados y coincidencia exacta de los veintiséis archivos con la revisión final posterior a QA. Se mantienen Reviewer/QA r7 PASS, 145/145 pruebas y 15/15 casos de preflight; sin cambios de implementación que justifiquen repetirlos. Solo se agregan estas notas de aceptación/publicación antes del commit. El resultado real de commit/push/PR se registra en el issue canónico. No se aplica la migración a producción ni se configura/publica ese ambiente; no se cambian datos, credenciales, certificados ni procesos locales. No hay cierre/completed automático ni merge por esta instrucción de abrir PR. Historial previo conservado.

## Acceso del propietario activado y verificado — issue #16 (2026-11-25)

Sobre el candidato `owner-wide-r1`, [Reviewer r7](https://github.com/etiznado/projectManager/issues/16#issuecomment-7182314793) sin correcciones y [QA r7](https://github.com/etiznado/projectManager/issues/16#issuecomment-7182393626) PASS independiente: seis grupos cubren AC-01–AC-24 con las condiciones de modo de requisitos r4. Build 0 errores y aviso NU1900; 145/145 pruebas y matriz aislada de launcher PowerShell 5.1 de 15/15. El coordinador habilitó únicamente el nuevo modo en runtime/approval, conservando los quince valores privados anteriores y los activos TLS. Listado real con la misma credencial incluyó #1/#2/#4/#5; #2 aceptó estado, avance y próximo paso, y otra escritura tras un segundo reinicio, conservando su contenido y generando exactamente cuatro eventos atribuibles UTC. QA SQL confirmó todas las tarjetas del propietario incluso sin grants, compatibilidad restringida por creación, revisión 4 de #2, historial previo intacto, mismos valores visibles y dieciséis tablas ajenas iguales. Proceso propio y listeners verificados; aplicación disponible para continuar las pruebas. API sin credencial devuelve 401 y tarjeta de otro propietario 404. No se modificaron datos mediante SQL, modelos/migraciones, planificación, certificados ni credencial; no hubo seed/reset. Cobertura de refresco/creación conservada en la suite y evidencia histórica; no se afirmó una nueva revisión visual. Esta nota solo añade evidencia después de QA. Aceptación UI de Edgar pendiente, sin commit/push/PR, merge, despliegue o completed.

## Acceso de Jarvis a todos los frentes del propietario — issue #16 (2026-11-25)

Implementación de Developer conforme a [record r30](https://github.com/etiznado/projectManager/issues/16#issuecomment-7182114813), requisitos/diseño r4: `AllowAllOwnerFronts` opcional/false por defecto, consulta central por propietario cuando se habilita, rama restringida previa conservada. Modo privado validado independientemente, tipado y propagado; preflight de PowerShell precede al retorno idempotente y pruebas de puertos quedan después. Build con salida separada: 0 errores, un aviso NU1900 por consulta de vulnerabilidades NuGet inaccesible. Suite completa aislada: 145/145 PASS, sin fallos ni omitidas; HTTP/SQLite cubre tarjetas existentes y futuras sin grants/procedencia, tres mutaciones/historial, fronteras por propietario/activo/autenticación, forja de permiso, validación, conflicto, rollback y modo restringido; perfil prueba JSON real omitido/false/true, discrepancia/string/null y configuración efectiva. Pendientes revisión independiente, QA del launcher y verificación operativa con la misma credencial tras activar solo el modo autorizado y reiniciar. No se cambió ambiente privado ni se ejecutó SQL por Developer. Se preservan correcciones PowerShell y cambios manuales de UI, modelo, migraciones e historia; sin commit/push/PR ni despliegue.

## Corrección del arranque en PowerShell 5.1 (2026-11-25)

Reproducido en sesión limpia el tipo ProtectedData no disponible; cargar System.Security lo habilita en Windows PowerShell 5.1. El launcher ahora selecciona explícitamente la biblioteca según Desktop/Core y registra la ruta resuelta del ejecutable en vez de depender de Process.Path recién creado. Prueba operativa real mediante powershell.exe: compilación sin errores (un aviso NU1900), arranque, segundo arranque idempotente, parada y reinicio correctos. Verificación adicional de descifrado DPAPI en PowerShell 7, sin exponer el contenido. Identidad guardada y listeners coincidentes, login 200 y API protegida 401; perfil, credencial, grants, certificado y archivos protegidos conservados. Aplicación encendida para continuar pruebas. No se editó código .NET ni se repitieron sus 136 pruebas anteriores; la comprobación de esta corrección fue el ciclo real de los scripts en la versión afectada. Sin cambios SQL, migraciones, commit, push o despliegue.

## ID visible en Frentes abiertos (2026-11-25)

Implementado por petición directa de Edgar: título de tarjeta con formato `#123 · Título`, usando el ID SQL existente y estilo secundario para el número. No se renumeran posiciones ni se cambia persistencia. `dotnet build RaceIT.ProjectManager.sln -m:1 -p:OutputPath=bin/verification/` correcto, 0 errores y dos avisos NU1900 por falta de acceso al índice de NuGet para consultar vulnerabilidades. `dotnet test RaceIT.ProjectManager.sln -m:1 --no-build --no-restore -p:OutputPath=bin/verification/`: 136/136 PASS fuera del sandbox; primer intento dentro abortado por timeout de conexión a testhost a los 90 segundos. `git diff --check` correcto. Verificación visual pendiente; no se reinició la aplicación.

## API privada para Jarvis — issue #16 (2026-11-25)

Creación verificada y cargada en el ambiente de prueba existente: [Reviewer r6](https://github.com/etiznado/projectManager/issues/16#issuecomment-7181451342) y [QA r6](https://github.com/etiznado/projectManager/issues/16#issuecomment-7181549753), candidato `card-creation-r1` sin correcciones. Seis grupos cubren AC-01–AC-18; build 0 errores/advertencias y 136/136 pruebas. POST autorizado devolvió 201; la nueva tarjeta apareció automáticamente en 24,965 s. Las tres actualizaciones funcionaron sobre ella; los IDs no concedidos devolvieron 404. Reinicio propio conservó listado y escritura con la misma credencial. SQL independiente ejecutó la comparación binaria real de actor: creador sin grants explícitos autorizado, identidad distinta solo por mayúsculas excluida. Persistieron exactamente un evento de creación y cuatro mutaciones UTC; tarjetas anteriores y dieciséis tablas ajenas intactas. Configuración/TLS idénticos en sus cinco archivos y mismos grants. Se dejaron la aplicación ejecutándose y la pantalla reconectada; no hubo migraciones, semilla, reset, cambio de ambiente ni nuevos permisos privados. Solo se agrega esta evidencia documental tras QA; aceptación UI de Edgar pendiente y sin commit/push/PR, merge, despliegue o completed. Historial previo conservado a continuación.

Enmienda de creación de tarjetas, autorizada en record r24 y definida por requisitos/diseño r3: implementación de Developer limitada al endpoint/DTO, servicio, pruebas y notas/contrato. POST valida título/contexto/estado/textos y rechaza campos de servidor; guarda una tarjeta con revisión 1 y un único evento de creación en el mismo SaveChanges. Listado y todas las actualizaciones comparten el filtro por propietario actual más ID explícito o creación atribuida al mismo conector/propietario. Los IDs/grants del perfil y demás valores privados permanecen intactos. Suite completa: 136/136 PASS; seis grupos nuevos cubren defaults/límites/evento UTC, rechazo de null/forja/autenticación/inactivo, acceso durable con contexto/servicio nuevo y sin grants explícitos, exclusión de otras procedencias/propietarios/reasignación, y fallo real de INSERT del historial con rollback de la tarjeta, preservación de historia anterior y reintento. Build sin errores/advertencias. Sin SQL activo, cambios de procesos/configuración/certificado, modelo, migración o UI durante Developer; Reviewer/QA y carga coordinada de la revisión pendientes. Historial previo conservado a continuación.

Ampliación de aplicación completa/refresco: [QA r5](https://github.com/etiznado/projectManager/issues/16#issuecomment-7180849269) PASS sobre `integrated-local-app-r2`, tras [Developer r5](https://github.com/etiznado/projectManager/issues/16#issuecomment-7180696733) y [Reviewer r5](https://github.com/etiznado/projectManager/issues/16#issuecomment-7180725421). Build 0 errores/advertencias y 130/130 pruebas: 106 anteriores, 7 de refresco/ciclo y 17 de perfil. El arranque real detectó un defecto no cubierto por la primera revisión: EF añade ApplicationName a la conexión. Se corrigió únicamente su normalización diagnóstica en el guard de apertura y se añadió regresión; comparación de aprobación, destino, autenticación y otras opciones siguen fijadas. El perfil de prueba completo se inició, detuvo y reinició mediante scripts; datos/historial y certificado iguales, segundo arranque idempotente y mismo token útil después del reinicio. La pantalla real con selector privado mostró los tres campos escritos por API; una segunda escritura cronometrada apareció automáticamente en 28,289 s, sin navegación/recarga. Historial final = 21, exactamente el inicial 17 más cuatro comandos API explícitos; polling no escribió. Se mantuvieron los otros frentes sin cambios y el proceso/página quedaron abiertos para revisión de Edgar. Configuración privada ya preparada en `.local-test/` ignorado, con verificador existente y TLS/PFX protegido por CurrentUser-DPAPI; no contiene token plano. Un comando de arranque y otro de parada documentados en `docs/local-test.md`. No hubo nuevas migraciones/bases/semilla/reset, expansión de permisos, cambios en la base habitual, commit/push/PR, merge o despliegue. `ui_review: required`; aceptación de Edgar pendiente. Esta adición solo registra evidencia y no altera el código revisado.

Enmienda de aplicación local repetible y sondeo: requisitos r2/diseño r2 derivados del pedido expreso de Edgar (record r17). Implementados perfil validado con tupla de aprobación independiente, configuración/TLS privados ignorados y estables, launcher oculto de la aplicación real con espera acotada del login, parada por identidad y cierre normal; sin creación/reset de datos. El perfil rechaza entorno/banderas/destino/acceso/endpoints ajenos y usa cookie separada; preserva el selector legacy y autenticación normal. El componente de Frentes abiertos inicia un solo PeriodicTimer de 30 segundos tras render interactivo, comparte exclusión con carga/guardado/recarga, mantiene último resultado válido ante fallo y cancela al disponer. Build de Developer sin errores/advertencias; 129/129 pruebas (106 anteriores, 16 de guard del perfil y siete de ciclo/operaciones del sondeo). Launchers validan sintaxis; archivos privados comprobados ignorados. Sin arranque/parada de la instancia existente ni SQL durante esta fase. Falta Reviewer/QA sobre el candidato integrado y prueba real de reinicio/credencial/página con aceptación UI de Edgar. No se agregaron migraciones ni cambió el servicio/historial aprobado. Evidencia previa se conserva a continuación.

Handoff operativo local solicitado por Edgar: `local-host-r1` compilado y revisado sin correcciones ([Developer r3](https://github.com/etiznado/projectManager/issues/16#issuecomment-7179737918), [Reviewer r3](https://github.com/etiznado/projectManager/issues/16#issuecomment-7179772194)); [QA r4](https://github.com/etiznado/projectManager/issues/16#issuecomment-7179828607) PASS. Instancia encendida solo para la misma laptop, sobre los datos sintéticos y con acceso a un frente; validación TLS con certificado público, lectura/escritura autorizadas y rechazos de credencial/alcance/revisión comprobados. No se reejecutó la suite completa para este helper operativo sin cambios del candidato de producción; se conserva el resultado anterior de 106/106. Ciclo limitado y cierre normal con comprobación de identidad del proceso; no se detuvo el servidor solicitado por el usuario. Credencial solo en el chat solicitado, nunca en estas notas ni en GitHub. Conexión efectiva del cliente Jarvis aún no ejecutada. Una publicación inicial con detalles locales fue rechazada automáticamente; se publicó y verificó una versión reducida sin esos datos. Sin cambios de producción, base habitual, commit, push/PR, merge, despliegue ni completed.

Integración SQL Server y navegador: PASS, [QA r3](https://github.com/etiznado/projectManager/issues/16#issuecomment-7179486840), bajo la autorización de Edgar para una base nueva local ([record r12](https://github.com/etiznado/projectManager/issues/16#issuecomment-7179268873)). Destino nuevo verificado y conservado: `localhost\SQLEXPRESS / ProjectManager_FEAT016_QA_20261125_A4F2`, SQL Server 2016, compatibilidad 130. Se aplicaron seis migraciones en esa base solamente; con datos sintéticos previos a la última, la huella de personas/frentes/planificación se mantuvo `d46f67d5b27730b5f16ec8ddd2eab74816e888706d053306039c58ebbab235ee`. API HTTPS con los módulos de producción y SQL real pasó los seis grupos AC, incluido rollback por restricción temporal (retirada), reintento y dos escritores simultáneos con un 200/un 409. La pantalla real cargada con sesión sintética mostró los valores API, guardó como persona, rechazó revisión antigua y recargó correctamente; historial final con 13 eventos, 11 de conector y 2 de persona, UTC confirmado en SQL. Huella de planificación/personas/jornada/actividad/bloques/registros intacta: `a20a19ae787bc609ad57ab9ad29452b812d28bc3750728f82d17ff34786b9910`. Build sin errores/advertencias y 106/106 pruebas. Sin cambios en código/migraciones durante estas comprobaciones; solo se agregan notas y se actualizan los límites del contrato. El fixture privado corrigió únicamente su certificado temporal de Windows, sin instalar confianza, y terminó con sus puertos cerrados; no se borró la base. No se tocaron contenidos de bases existentes ni autenticación productiva. Conector externo real, base habitual y publicación/merge/despliegue/completed continúan pendientes de sus autorizaciones.

Verificación final del coordinador sobre la migración local: [Reviewer r2](https://github.com/etiznado/projectManager/issues/16#issuecomment-7178737044), sin hallazgos/correcciones, y [QA r2](https://github.com/etiznado/projectManager/issues/16#issuecomment-7178782291), PASS del candidato autorizado. Artefacto probado antes de esta adición documental: `0d77177ce49a4505334b1e59f77aca4e7d9e39a737542d4b813edf6ae36facfe`. Se repitieron de forma independiente build limpio, 106/106 pruebas y verificación EF offline de las dos operaciones aditivas, modelo/snapshot alineados y cero intentos de conexión. La creación de migración/Designer/snapshot queda completada; aplicación, upgrade/runtime/concurrencia SQL Server y activación real de Jarvis siguen BLOCKED/no ejecutados por falta de autorización y destino verificado. Esta nota solo añade evidencia; no cambia el código/migración revisados ni autoriza datos, commit, push/PR, despliegue o marcar completed.

Continuación autorizada por Edgar para archivos locales únicamente, [Feature record r8](https://github.com/etiznado/projectManager/issues/16#issuecomment-7178577638): Developer creó `20261126001849_JarvisOpenFrontHistory.cs`, Designer y snapshot mediante EF10.0.13 directamente, sin arrancar Program ni abrir SQL. Un contexto con conexión inerte e interceptor de bloqueo registró cero intentos. Se comprobaron las dos operaciones Up (tabla de historial e índice único, con FK NoAction dentro de la tabla), Down limitado a esa tabla, diff semántico sin alteraciones de entidades/semilla anteriores, snapshot alineado y script idempotente generado solo en archivo local. EF reordenó/normalizó los bloques previamente añadidos manualmente al snapshot; el cambio de esquema sigue siendo exclusivamente la tabla nueva. Build final con `-m:1 -p:OutputPath=bin/verification/ --no-restore`, 0 errores/advertencias; suite con `--no-build --no-restore` y misma salida fuera del sandbox: 106/106 PASS. `git diff --check` correcto. Reviewer/QA de esta revisión pendientes; aplicación SQL y activación real no autorizadas. La evidencia previa siguiente se conserva como historia.

Developer preparó autenticación propia `JarvisConnector`, configuración cerrada por defecto con hash de token y permisos por ID/propietario, cuatro endpoints tipados con revisiones y errores JSON, y una ruta común de guardado con historial transaccional de valores/actor/UTC/revisión. La pantalla conserva firmas y marcado; sus cambios de estado registran actor de persona. Se agregó modelo/mapeo de `OpenFrontChanges` y contrato técnico `docs/jarvis-open-fronts-api.md`; no se crearon migraciones ni se modificó el snapshot por falta de autorización expresa para esa acción. Es un prerrequisito de ejecución antes de usar esta versión con una base existente.

Verificación de Developer: build con `-m:1 -p:OutputPath=bin/verification/ --no-restore`, 0 errores/advertencias; `dotnet test RaceIT.ProjectManager.sln -m:1 -p:OutputPath=bin/verification/ --no-restore` fuera del sandbox, 106/106 PASS. Nueve pruebas nuevas usan registros de autenticación/endpoints reales en TestServer y SQLite `:memory:`: grants/propietario activo/reasignación, cookies y credenciales malformadas/HTTP rechazados, configuración inválida/cerrada, persistencia de tres operaciones y planificación intacta, IDs indiscriminables, historial durable/atribución, validación/conflicto, fallo relacional al insertar historial con rollback/reintento y un escritor perdedor entre lectura/guardado. La primera ejecución detectó normalización de cabeceras por HttpClient en la prueba; se usa envío raw de TestServer para cabeceras malformadas. Restauración del paquete oficial TestHost 10.0.13 requirió acceso fuera del sandbox por DNS; ejecución final correcta. Reviewer/QA independientes pendientes. Sin uso de SQL Server ni autorización/ejecución de migración, publicación, push/PR, despliegue o activación real de Jarvis.

Verificación posterior del coordinador (2026-11-25): [Reviewer r1](https://github.com/etiznado/projectManager/issues/16#issuecomment-7178473951) aprobó sin hallazgos/correcciones; [QA r1](https://github.com/etiznado/projectManager/issues/16#issuecomment-7178519645) verificó el mismo manifiesto de once archivos `23f941d002368112e2a19d5270b4197d7d0dd737e414ebd0fa35559edc3d4c08` y los seis grupos de escenarios. Ambos repitieron build sin errores/advertencias y 106/106 pruebas fuera del sandbox con TestServer/SQLite en memoria. No hay defectos encontrados ni cambios de código posteriores; esta adición solo registra evidencia. PASS del comportamiento automatizado; readiness general BLOCKED por falta del esquema de historial. SQL Server, creación/aplicación de migración, lectura visual contra SQL y activación real de Jarvis no ejecutados. La autorización solicitada para crear únicamente la migración local sigue pendiente; no autoriza aplicación a ninguna base. Sin cambios de UI (`ui_review: none`), estado de completado, commit, push/PR ni despliegue.

Frentes abiertos abre directamente en la cuadrícula. Se compactaron las tarjetas para aprovechar dos filas en escritorio. Compilación correcta y 97/97 pruebas aprobadas. La cantidad de filas completas depende del texto y del tamaño de ventana, y queda pendiente comprobarla visualmente.

## Fondo degradado y modo oscuro por defecto (2026-11-25)

Selector según referencia visual: `MainLayout` usa una cápsula de 126×50 px con sombras interiores, perilla circular a la izquierda en claro/derecha en oscuro, iconos SVG y etiqueta del modo actual en español. Estilos aislados en `MainLayout.razor.css`, retiradas las reglas globales del botón anterior. Semántica `role="switch"`, `aria-checked`, foco visible y movimiento reducido. Build con `-m:1 -p:OutputPath=bin/verification/`: 0 errores/advertencias; 97/97 pruebas con la misma salida fuera del sandbox. Sin nueva prueba de navegador.

Aplicado el degradado radial de Frentes abiertos al fondo del modo oscuro de todo el sitio, compartiendo la variable CSS con esa página. `ThemeState` y la paleta inicial arrancan en oscuro; el modo claro conserva sus colores y fondo sólido mediante la clase `theme-light` del layout. El login también usa el oscuro por defecto. Actualizada la prueba existente de tema para comprobar oscuro inicial, alternancia en ambos sentidos e independencia de una nueva instancia. Build con `-m:1 -p:OutputPath=bin/verification/`: 0 errores/advertencias. Suite con la misma salida, fuera del sandbox: 97/97. Sin arranque de app ni revisión visual de esta versión.

## Perfil local Private QA de VS Code — issue #17 (2026-11-25)

Añadido un perfil `coreclr` dedicado a la base sintética autorizada, con `launchSettingsProfile: null`, ambos entornos explícitos en `PrivateQa`, flags del selector y bind a `127.0.0.1:5244`. Una tarea propia usa `-m:1`, conservando perfil y tarea originales. Verificación estática de JSON, restricciones de destino y conservación de las ediciones manuales; no se ejecuta F5 ni se cambia SQL/autenticación en esta modificación. Evidencia de runtime/QA en el [issue #17](https://github.com/etiznado/projectManager/issues/17).

## Simplificación visual de Frentes abiertos (2026-11-25)

Acceso solicitado por Edgar: enlace «Frentes abiertos» en `MainLayout.razor`, visible en el menú autenticado y dirigido a `/frentes-abiertos`. Build con `-m:1 -p:OutputPath=bin/verification/`: 0 errores/advertencias; suite con la misma salida: 97/97 fuera del sandbox. Sin nueva ejecución de app ni prueba de navegador.

Seguimiento: eliminado el mensaje de confirmación de guardado de estado, conservando errores y actualización del estado confirmado. Build con `-m:1 -p:OutputPath=bin/verification/`: 0 errores/advertencias; 97/97 pruebas con esa salida, fuera del sandbox. La salida normal estaba bloqueada por la app en ejecución y `BaseOutputPath` incluyó un JS de una publicación anterior (BLAZOR106); `OutputPath` permitió verificar sin detener la app. Sin nueva prueba de navegador.

Aplicados los ajustes pedidos por Edgar en `OpenFronts.razor` y su CSS: encabezado sin marca/rombo ni subtítulo, sin filtros y con todos los frentes personales, título sin contorno al recibir el foco de navegación y botones de estado compactos con opciones no seleccionadas grises. Verificado el CSS aislado generado, incluido el tamaño de fuente y el color del estado seleccionado. `dotnet build RaceIT.ProjectManager.sln -m:1`: 0 errores/advertencias; el comando sin `-m:1` falló sin diagnóstico útil. `dotnet test RaceIT.ProjectManager.sln -m:1 --no-build`: 97/97 fuera del sandbox, tras abortar dentro por timeout de conexión a testhost. `git diff --check` correcto. Sin ejecución de app ni verificación visual de esta versión.

## Frentes abiertos persistidos — issue #17 (2026-11-25)

Preparado localmente el guardado SQL de frentes personales y el cambio de estado inline, reutilizando la demo Blazor de cuatro columnas. Servicio separado con validación de propietario activo, estados permitidos y revisión de concurrencia; migración `OpenFronts` solo agrega tabla, índice y restricciones. Sin catálogo fijo, cambios en Mi semana, API de Jarvis ni datos reales. Build final sin errores/advertencias, suite completa 97/97 correcta fuera del sandbox y comprobación EF offline sin diferencias pendientes de modelo. La primera ejecución de pruebas en sandbox abortó por conexión a testhost a los 90 s. Seis pruebas nuevas cubren propiedad, persistencia con contexto nuevo, planificación intacta, identidad inactiva/no confiable, estados/revisión inválidos y rechazo de guardado con reintento. Handoffs y revisión independiente en el [issue #17](https://github.com/etiznado/projectManager/issues/17). SQL desechable, navegador y aceptación de UI pendientes de los roles correspondientes. El expediente histórico FEAT-005 permanece intacto; sin migración en base habitual, push, PR o despliegue.

Verificación posterior del coordinador (2026-11-25): servicio real con proveedor SQL Server en base desechable nueva pasó guardado y lectura con contexto nuevo, rechazo de propietario ajeno/estado inválido/revisión antigua y planificación intacta. En navegador se verificaron guardado, reapertura, cuatro columnas a 1920 px, adaptación a 390 px, filtros/vacío, error de SQL con borrador y filtro conservados, reintento y conflicto entre pestañas. La restricción temporal de prueba fue retirada. El hash de planificación se conservó. Un primer empaquetado tomó una salida Release antigua; se detuvo ese preview y se republicó explícitamente Debug, cotejando su DLL con el build revisado antes de probar UI. Un MDF preexistente impidió la creación con ruta predeterminada; no se reutilizó ni modificó, y se crearon archivos nuevos para esta base. QA independiente repitió build sin errores/advertencias y 97/97 pruebas; aceptación visual de Edgar pendiente. Con cinco fichas y controles hay desplazamiento vertical a 1080p. Sin cambios de código posteriores a Reviewer; solo se añaden estas notas de evidencia. Registro canónico y resultados por rol en #17.

Actualizado: 2026-11-23.

## Scope approval and Codex artifact ownership (2026-11-23)

Updated only workflow instructions in `AGENTS.md`, `features/README.md`, all five `.codex/agents/pm_*.toml` roles, the four `_template` guides, and these affected memory notes. Jarvis's exact-scope approval/ready gate now precedes the complete Codex flow, including Gherkin without a second in-scope approval. Architect retains the verified label transition after Gherkin and before design. Codex must publish and verify its own artifacts or stop and report BLOCKED to Jarvis; no Jarvis artifact fallback. Material scope changes follow Edgar's approval or express feature-specific delegation. Jarvis marks completed only on Edgar's confirmation; required UI, merge, and deployment approvals remain. Earlier workflow entries below are historical and superseded on these points. Historical feature files, including FEAT-007 wherever already recorded, were not changed, nor were app code, monitors, actual GitHub issues, SQL, or migrations. No commit, push, PR, or merge was made.

Verification: instruction consistency review and `git diff --check` passed. Standard build/test commands exited unsuccessfully without an actionable diagnostic; build with `-m:1` passed (0 warnings/errors). Tests with `-m:1` reached vstest but were aborted after its 90-second testhost connection timeout; no test PASS is claimed. Test verification is BLOCKED by the runner connection failure. The GitHub workflow itself was not exercised.

## Architect issue-label claim (2026-11-22)

Updated `AGENTS.md`, `features/README.md`, and `pm_architect.toml` so the Architect changes `ready` to `inprogress` at the start of the authorized Architect phase, confirms the final label state, and reports the claim for the coordinator's issue comment. Build passed with 0 warnings/errors and tests passed 70/70. No issue was supplied and no label operation was run; end-to-end verification remains pending.

## Issue-comment feature workflow (2026-11-22)

Updated `AGENTS.md`, `features/README.md`, the four `features/_template` content guides, and all five `.codex/agents/pm_*.toml` definitions. New feature artifacts and role handoffs go to the supplied GitHub issue as full Markdown comments, with permalink/read-back verification, exact revision references, and append-only new versions after approvals or gate-affecting changes. Existing FEAT records were retained. Build passed with 0 warnings/errors and tests passed 70/70. No issue was provided, so no GitHub comment was posted and the flow has not been tested end to end.

## Jarvis role handoff clarification (2026-11-22)

Updated `AGENTS.md` and `features/README.md` to route Jarvis feature requests through a Codex task that can load the project `pm_*` agent types, with a work-tree and role preflight before claiming a phase. The failed Jarvis surface had neither a usable Git work tree nor role invocation; no feature implementation was attempted there. This local work tree and role availability were confirmed. Verification: `dotnet build RaceIT.ProjectManager.sln` passed with 0 warnings/errors; `dotnet test RaceIT.ProjectManager.sln` passed 69/69. Jarvis launch and end-to-end phase handoff remain untested; no Jarvis configuration or trigger was changed.

## FEAT-006: selector Light/Dark (2026-11-22)

Implementado localmente en la rama aislada: `ThemeState` scoped inicia Light, el encabezado autenticado incluye botón nativo con `aria-pressed`, y `app.css` aplica variables de paleta según el estado del encabezado. El diseño y el plan de seis casos están en [FEAT-006](../features/FEAT-006/README.md). `pm_reviewer` aprobó el diff sin correcciones; `pm_qa` repitió build (0 errores/0 advertencias), suite completa (70/70) y `git diff --check`. Un caso de estado pasó; cinco casos de UI/navegación/recarga/legibilidad quedaron BLOCKED porque no existe fixture SQL sintético ni loopback exclusivo verificados. No se inició la app ni se tocó SQL. Falta QA visual y aprobación de Edgar del candidato exacto; estado `In Progress`.

## Feature workflow documentation (2026-11-22)

Defined the new feature directory and four file templates, the Jarvis handoff order, role write boundaries, owner approval gates, and QA's focused positive/negative test plan. Updated the five `.codex/agents` definitions so Analyst writes requirements, Architect writes design, Reviewer can make scoped code/test corrections, and QA writes and executes the test plan after review. The existing FEAT-001 through FEAT-005 files were not migrated. No Dot automation was configured by this repository change. See [the workflow](../features/README.md).

Tightened the requirements gate: Gherkin acceptance scenarios with unique `@AC-NN` tags, Given/When/Then steps, and normal plus relevant exception coverage are now required before `Ready`; QA's test plan maps cases to those tags. Updated the requirements template, workflow, AGENTS.md, and Analyst/QA role instructions. Existing feature records remain unchanged. Verification: Release build passed with 0 warnings/0 errors and 69/69 tests passed. The standard Debug build could not copy an in-use DLL; a separate `BaseOutputPath` attempt encountered a pre-existing `bin/demo-publish-feat002` Blazor JS module, so verification used Release output.

## Pool agrupado por proyecto (2026-10-23)

Por solicitud del usuario, las actividades disponibles de `/mi-semana` se agrupan por proyecto. Decisiones del usuario: orden alfabético con «Sin proyecto» al inicio, el encabezado muestra solo el nombre del proyecto, el colapso se recuerda solo en memoria del componente y no hay encabezados fijos. Nueva regla pura `PoolGrouping.Group` (`Services/PoolGrouping.cs`) que devuelve `ActivityPoolGroup(ProjectId, Name, Rows)`; conserva el orden de creación dentro de cada grupo y no fusiona proyectos distintos que compartan nombre. En `MyWeek.razor` el pool se dibuja por grupos: encabezado como botón con `aria-expanded` y chevron que rota −90° al colapsar, y tarjetas solo cuando el grupo está expandido. El estado vive en `HashSet<int?> collapsedProjects` (por defecto expandido) y al crear una actividad con el «+» se quita su proyecto del conjunto para que la nueva actividad se vea. Se conserva la lista plana `pool` porque el arrastre la usa para tomar las horas pendientes. La tarjeta ya no repite el nombre del proyecto (quedó en el encabezado). Estilos nuevos en `app.css` (`.pool-group`, `.pool-group-icon`, `.pool-group-items`). `PoolGroupingTests` aporta 6 pruebas (agrupación, orden, «Sin proyecto» al inicio, orden interno, proyectos distintos con el mismo nombre y pool vacío). Por pedido del usuario, el nombre del proyecto en el encabezado del grupo usa el amarillo de `--risk` (el mismo de la métrica «Sin asignar»; el usuario lo ajustó desde `--accent`) y el chevron queda en `--muted`; el hover se resuelve con `filter: brightness(1.25)` para no depender del color. El fondo del encabezado es el mismo amarillo a baja opacidad (`rgba(230, 178, 60, .12)`, radio 6px) en vez de amarillo pleno: así no compite con la métrica «Sin asignar» ni con los banners y badges de riesgo, y el contraste del texto queda en ~6,6:1. Verificado: build 0 errores/0 advertencias y 69/69 pruebas xUnit. Revisión visual en navegador pendiente.


## Carriles uniformes en Mi semana (2026-10-23)

Por decisión del usuario, el timeline de `/mi-semana` reparte el ancho en **carriles de igual tamaño**, uno por día con jornada mayor a cero; los días de 0 h no ocupan espacio. Antes el ancho de cada día era proporcional a sus horas (2 h contra 5 h daban 4,4 % contra 11,1 % del eje), un día de 0 h quedaba como una astilla con la etiqueta recortada contra la línea de "ahora" y, cuando la bolsa superaba la jornada sumada (45 h contra 18 h), el 60 % del timeline quedaba vacío. `WeekLayoutCalculator` incorpora `WorkingDays`, `TrackPercent`, `SplitIntoDays` y `SegmentFillFraction` como reglas puras: la posición de una hora se mapea a (carril + fracción del día), un bloque se dibuja como tramos contiguos (uno por día que toca) y el relleno verde de consumo se reparte de izquierda a derecha. El trabajo que excede la jornada sumada se recorta en el borde con la marca «fuera de jornada» en la fila; un bloque que cae enteramente fuera se ancla como chip al borde derecho para seguir siendo pulsable y arrastrable. Se retiró `AxisTotal`: ya no existe un eje lineal único. Consecuencia asumida y verificada en pruebas: la escala de horas deja de ser lineal (una hora en un día de 2 h ocupa 2,5 veces el ancho de una hora en un día de 5 h). `WeekLayoutCalculatorTests` pasa de 3 a 13 pruebas (jornada mixta 2/2/2/2/0/5/5, tramos, fronteras de día, desbordamiento y relleno). Verificado: `dotnet build` 0 errores/0 advertencias y 63/63 pruebas xUnit con salida alternativa (`BaseOutputPath=bin/verification/`, la DLL de `bin/Debug` estaba bloqueada por el depurador). Primera revisión visual del usuario (2026-10-23): los tramos se veían más delgados a partir del segundo día porque la altura de la barra dependía de su contenido y solo el primer tramo lleva la etiqueta de horas; se fijó `.schedule-bar` con `height: calc(1.2em + .6rem)` (la misma fórmula que usa la pista) y centrado por flex, y la elipsis de la etiqueta pasó a `.schedule-hours`. Segunda revisión visual del usuario (2026-10-23): la etiqueta de horas se dibujaba solo en el primer tramo, y ese tramo puede ser una astilla (un bloque que empieza a 23,79 h tiene su primer tramo en los últimos 0,21 h del miércoles). El texto se recortaba ("solo se mira 4") y el resto de la barra quedaba sin horas, con la etiqueta descentrada respecto al bloque. Ahora `BlockVm` lleva `LeftPercent` y `SpanPercent` (posición del bloque completo en la pista, acotada a la jornada) y la etiqueta es una sola capa `.schedule-hours` posicionada en absoluto, centrada sobre todo el bloque, sin capturar el puntero (`pointer-events: none`) y por debajo de la línea de "ahora". Resto de la interacción en navegador pendiente; no se tocaron datos ni esquema.


## Icono de calendario en fechas editables (2026-10-23)

Los campos de fecha meta de Iniciativas y Proyectos usan el selector nativo `InputDate` con una clase compartida que hace visible el icono interior en el tema oscuro; conservan edición manual. El formulario de Proyectos se reutiliza en Administración y Mis proyectos. Sin cambios de esquema o datos; revisión visual e interacción en navegador pendientes.

## Jornada diaria editable en Personas (2026-10-23)

Por decisión del usuario, cambiar la bolsa semanal no altera las horas de cada día. `WorkdayEditor` se integra en el formulario compartido de alta y edición de Personas: siete controles, jornada nueva predeterminada de 8/8/8/8/8/5/0 y carga de valores existentes al editar. `PersonService` guarda bolsa y jornada por separado; `PersonInput` exige los siete días únicos y de 0 a 24 h por día con hasta dos decimales. La suma diaria puede diferir de la bolsa. Compilación y 53/53 pruebas xUnit correctas; interacción visual y persistencia SQL pendientes. No se alteraron datos de usuario en SQL durante la implementación.

## Creación de proyectos y actividades por Usuario (2026-10-22)

- `/mis-proyectos` permite al empleado listar proyectos asignados y crear proyectos independientes; iniciativa permanece exclusiva de Administrador.
- `ProjectForm` y `ActivityForm` son componentes reutilizados por Administración. Mi semana abre `ActivityForm` en modal y refresca el pool tras guardar, sin programar la actividad automáticamente.
- Servicios: `ProjectService` impide asociar proyectos del empleado a iniciativas y filtra su listado; `ActivityService` impide asociar actividades a proyectos de otro responsable. Conserva validación y transacción de dependencias para uso futuro.
- Verificado: build 0 errores/advertencias y 33 pruebas xUnit correctas. Pendiente: comprobar los nuevos formularios visualmente y con SQL en uso real.

### Dependencias aplazadas en el formulario (2026-10-22)

Se retiró el selector de predecesoras de `ActivityForm` compartido por Mi semana y Administración. La creación desde la interfaz no envía dependencias. Entidad, datos existentes y métodos del servicio se conservan para después. Compilación y 33/33 pruebas correctas con salida alternativa (`BaseOutputPath=bin/verification/`) por bloqueo de la DLL en `bin/Debug`. Revisión visual pendiente.

### Creación en proyecto propio desde Mi semana (2026-10-22)

Corregido el caso Edgar/Pablo: el administrador veía proyectos de otros responsables en el modal de Mi semana y podía guardar una actividad propia allí. `ActivityForm` usa ahora `ListOwnProjectsAsync` y `CreateOwnAsync` en ese contexto; el servidor comprueba el propietario del proyecto. En Administración el administrador mantiene gestión global. No se cambió la actividad «Nueva» ya capturada. Compilación correcta y 38/38 pruebas xUnit (incluye cinco casos nuevos de política); validación visual y SQL pendiente.

### Edición de proyectos (2026-10-22)

`ProjectForm` sirve para crear y editar. Usuario edita nombre y fecha meta de proyectos propios en `/mis-proyectos`; Administrador puede editar además iniciativa y responsable desde `/administracion`. `ProjectService.UpdateProjectAsync` aplica autorización y validación en servidor. No se modificaron datos existentes durante la implementación. Compilación correcta y 42/42 pruebas xUnit, con cuatro casos nuevos de permiso para editar. Interacción visual y persistencia SQL pendientes.

### Administración por secciones (2026-10-22)

Se añadieron botones para mostrar una sola categoría a la vez: Personas, Iniciativas, Proyectos, Actividades o Plantillas. Proyectos se abre por defecto; cada categoría conserva lista y formulario. No hubo migración ni cambios de datos. Compilación correcta y 42/42 pruebas; revisión visual pendiente.

### Confirmación para eliminar plantillas (2026-10-22)

El botón Eliminar de Administración abre un modal con el nombre de la plantilla, Cancelar y «Sí, eliminar». Solo la confirmación llama al servicio; Escape y clic en el fondo cancelan. Se muestran errores dentro del modal. Compilación correcta y 42/42 pruebas existentes; prueba visual pendiente.

### Histórico: editor único de plantilla seleccionada (2026-10-22; sustituido por `TemplateForm`)

Esta etapa intermedia usó un selector y un formulario solo para editar elementos; `TemplateService.UpdateItemsAsync` aplicaba las modificaciones. El formulario único de creación/edición descrito abajo sustituyó ambos. Las actividades generadas siguen independientes. En esa etapa hubo compilación correcta y 46/46 pruebas xUnit; revisión visual y SQL quedaron pendientes.

### Un formulario de creación/edición por sección (2026-10-22; reemplaza el diseño anterior)

Administración muestra el formulario arriba y la lista abajo en los cinco módulos. Editar una fila carga el mismo formulario; guardar o cancelar vuelve al modo de creación. Personas, iniciativas y actividades tienen nuevos métodos de actualización; plantillas ahora comparten `TemplateForm` para crear y editar nombre, categoría y elementos, y se eliminó el formulario antiguo de texto y el selector separado. Proyectos reutiliza `ProjectForm`. Se preservan registros, planes, dependencias y actividades ya generadas. Compilación correcta y 49/49 pruebas xUnit; interacción visual y SQL pendientes.

## Redefinición completa (Etapas 1 y 2)

El usuario decidió descartar el modelo anterior y reconstruir según `Project_Manager_Notas_Diseno.md`. Se eliminaron dominio, servicios, páginas, migraciones y pruebas anteriores; se borró la base `projectManager` y se recreó vacía.

- Modelo de dominio nuevo (10 entidades): `Person`/`PersonWorkday`, `Initiative`, `Project`, `Activity`, `ActivityDependency`, `WeeklyPlan`, `PlanBlock`, `WorkRecord`, `Template`, `TemplateItem`. Sin fechas obligatorias en actividades.
- `ProjectManagerDbContext` con restricciones CHECK, índice único `WeeklyPlan(PersonId, WeekStart)` y semilla del Administrador (Persona Id=1; jornada inicial ilustrativa Lun–Sáb 7.5 h, corregida después a 8/8/8/8/8/5/0 con la migración `WorkweekEightFive`).
- Migración `InitialModel` (20261021010653) en `src/RaceIT.ProjectManager/Migrations/`, aplicada sobre base vacía.
- Login provisional con cookie; `WorkAccess` con roles Administrador/Usuario y métodos estáticos puros para pruebas.
- Servicios: `PersonService`, `ProjectService`, `ActivityService` (crear actividad sin fechas + dependencias con validación de ciclos + horas pendientes) y `PlanningRules`.
- Pantallas base: `/login`, `/mi-semana`, `/panorama`, `/administracion`.

Verificación: build 0 errores/0 advertencias; 19 pruebas xUnit correctas (`PlanningRulesTests`, `WorkAccessTests`). Sin verificación visual automatizada.

## Etapa 3: registro de tiempo (2026-10-21)

- `WorkRecordService`: iniciar/pausar/reanudar sesiones, registro manual, un solo contador activo por persona (iniciar otra actividad pausa la anterior) y completar explícito que cierra sesiones abiertas.
- `PlanningRules.SessionHours`: duración de sesión en horas decimales.
- `ManualTimeInput`: validación de registro manual.
- `/mi-semana`: filas seleccionables + detalle inferior con Iniciar/Reanudar/Pausar/Registrar tiempo/Completar, horas estimadas/registradas/pendientes y registros recientes (más adelante el detalle pasó a mostrarse en modal).
- Verificación: build 0/0 y 21 pruebas xUnit. Sin migración nueva (WorkRecords ya existía en InitialModel). Sin prueba visual ni interacción SQL automatizada.

## Etapa 4: Mi semana (2026-10-21)

- `WeekLayoutCalculator`: regla pura que coloca bloques consecutivos sobre el eje de horas laborables y calcula días, offsets y horas libres.
- `WeeklyPlanService`: obtener/crear plan semanal, agregar/quitar bloques, editar horas y reordenar.
- `/mi-semana`: navegación de semana, métricas (disponibles/planeadas/registradas/sin asignar), pool de actividades, timeline con barras proporcionales y línea "ahora", reordenamiento vertical y detalle inferior con registro de tiempo (más adelante el detalle pasó a mostrarse en modal).
- Reordenamiento vertical por arrastre implementado; la tabla separada y las flechas ↑/↓ se retiraron en favor del drag & drop nativo.
- Verificación: build 0/0 y 29 pruebas xUnit. Sin migración nueva (WeeklyPlans y PlanBlocks ya existían). Sin prueba visual ni interacción SQL automatizada.

## Etapa 5: panorama y riesgo (2026-10-21)

- `RiskCalculator`: regla pura de capacidad compartida — compara el trabajo pendiente de los proyectos con fecha meta contra la capacidad de una sola bolsa semanal; marca conflicto compartido e indicadores individuales.
- `PanoramaService`: carga la bolsa, proyectos e iniciativas de la persona, horas pendientes por proyecto y ejecuta el cálculo de riesgo.
- `/panorama`: métricas (bolsa/pendientes/proyectos/en conflicto), banner de "En riesgo por carga compartida" con exceso de compromiso y grupos iniciativa→proyecto con badges.
- Verificación: build 0/0 y 27 pruebas xUnit. Sin migración nueva. Sin prueba visual automatizada.

## Etapa 6: plantillas y continuaciones (2026-10-21)

- `TemplateService`: plantillas de carreras con semilla por defecto (Antes/Durante/Después), creación/eliminación y generación de actividades al pool con título "Título — Carrera Año".
- `TemplateRules`: regla pura de títulos generados.
- Continuaciones: entidad `Continuation` (migración `Continuations` aplicada), `ActivityService.ContinueActivityAsync` y `ListContinuationsAsync`; botón "Continuar" en el detalle de la actividad.
- Historial de semanas: disponible navegando con ← / → (los planes y registros persisten por semana); no hay movimiento automático de pendientes.
- Limitación documentada: la contabilidad de horas entre continuaciones y su efecto en el riesgo queda pendiente (aplazada por el usuario).
- Verificación: build 0/0 y 29 pruebas xUnit. Migración `Continuations` aplicada. Sin prueba visual automatizada.

## Etapa 7: Initiatives (antes "TV") (2026-10-21)

- La vista de solo lectura de iniciativas y proyectos se llama **Initiatives** (en inglés, por decisión del usuario), ruta `/initiatives`.
- Reutiliza `PanoramaService`; auto-refresco cada 30 s, botón de pantalla completa (`wwwroot/display.js`) y sello de última actualización.
- Se eliminó `/panorama` y el enlace del menú pasó a "Initiatives".
- Cuentas/roles: cubiertos por el login provisional con roles Administrador/Usuario y el alta de personas por el administrador; el login real sigue diferido.
- Verificación: build 0/0 y 29 pruebas xUnit. Sin prueba visual automatizada.

Pendientes diferidos (no bloquean): login real, drag & drop del pool, contabilidad de continuaciones, timeline visual con líneas punteadas y riesgo semanal.

## Datos de prueba (2026-10-21)

`DataSeeder` corre al arrancar en Development (idempotente): renombra el admin a **Edgar**, crea al empleado **Pablo** y, si no hay iniciativas, siembra Data Logger (PCB, Case interno, Selección de batería), el proyecto independiente Cajones de herramientas, actividades con dependencias, plan semanal, registros de tiempo y una continuación. Verificado con arranque local (login 200 y log "Datos de prueba sembrados").

## Arrastrar y soltar en Mi semana (2026-10-20)

- Implementado: arrastre nativo de tarjetas del pool a una fila del timeline/tabla (inserta antes) o a la zona final, incluso en un plan vacío. Usa las horas pendientes, o 1 h si se requiere reestimar; las horas siguen siendo editables.
- Implementado: reordenamiento vertical de bloques arrastrando la barra o el indicador ⠿ de la tabla. Conserva botones Agregar y ↑/↓ como alternativa sin arrastre. El eje horizontal se recalcula por orden y horas; no cambia sesiones ni registros.
- WeeklyPlanService inserta el nuevo bloque y ajusta posiciones en un solo SaveChanges; rechaza destinos inexistentes y órdenes con IDs duplicados. Sin migraciones ni datos de ejemplo nuevos.
- Verificado: build sin errores/advertencias y 29 pruebas existentes correctas. Pendiente: probar interacción de arrastre en navegador y persistencia SQL en uso real; no se afirma validación visual.
- Pendientes restantes: login real, contabilidad de continuaciones, timeline de Initiatives con líneas punteadas y riesgo semanal.

### Pool de la semana seleccionada (2026-10-20)

El pool excluye actividades que ya tienen al menos un bloque en el plan de la semana seleccionada. Al quitar el último bloque, la actividad abierta vuelve al pool; bloques en otras semanas no la ocultan. Se aplica al refrescar después de agregar, quitar o cambiar de semana.

### Filas integradas del timeline (2026-10-20)

Por solicitud del usuario se retiró la tabla inferior, la columna editable Horas y los controles ↑/↓. Cada actividad y su proyecto se muestran a la izquierda de su barra, alineados en una misma fila como el render expandido. La barra muestra solo horas; selección desde título/barra abre el mismo detalle. Se conserva quitar del plan y reordenamiento por arrastre. Las horas planeadas se conservan en datos, pero ya no tienen editor en esta vista. Cuadrícula diaria y encabezado comparten el eje proporcional. Build verificado sin errores/advertencias; revisión visual en navegador pendiente.

### Ancho completo (2026-10-20)

Se eliminó el máximo de 1180 px del contenedor principal compartido (.app-body). Las páginas aprovechan el 100% del ancho disponible conservando el padding lateral de 1.25rem. Cambio CSS comprobado en fuente; revisión visual en navegador pendiente.

### Pool sin botón Agregar al plan (2026-10-20)

Por solicitud del usuario se eliminó el botón Agregar al plan de las tarjetas y su manejador sin uso. La incorporación al plan se realiza mediante arrastre; pulsar el título sigue abriendo el detalle. Cambio revisado en fuente; sin verificación visual nueva.

### Columna Actividad más compacta (2026-10-20)

El encabezado del timeline ahora es Actividad. Su columna se redujo del 32% al 25%, con mínimo de 160 px, dejando más espacio al eje semanal. Cambio revisado en fuente; revisión visual pendiente.

### Filas más compactas (2026-10-20)

Se redujo la altura mínima de las filas del timeline de Mi semana de 88 a 72 px por solicitud del usuario. Cambio CSS revisado en fuente; revisión visual pendiente.

### Soltar dentro de Actividad (2026-10-20)

Se retiró el recuadro externo para agregar al final. El encabezado Actividad y el espacio vacío bajo sus filas aceptan actividades/bloques para agregarlos al final; las filas existentes conservan insertar antes. El espacio final forma parte de la cuadrícula, incluso en semana vacía. Revisión visual pendiente.

### Cabecera compacta de Mi semana (2026-10-20)

Se retiró el título visible Mi semana (se conserva PageTitle). Navegación semanal y métricas comparten una sola fila sobre pool/timeline; la barra permite desplazamiento horizontal si el ancho no alcanza. Compilación verificada sin errores ni advertencias; revisión visual pendiente.

### Timeline sin título ni instrucción (2026-10-20)

Se retiraron el título Plan de la semana y el texto de ayuda de arrastre del panel del timeline. La cuadrícula comienza directamente con Actividad y los días. Cambio de marcado revisado en fuente; sin nueva verificación visual.

### Altura disponible y scroll de paneles (2026-10-20)

Mi semana usa el alto restante del viewport después de la cabecera, navegación y métricas. Pool y timeline se estiran a la misma altura, con scroll vertical independiente al desbordarse. La cuadrícula llena el panel aun sin bloques: el espacio final de Actividad crece para aceptar arrastres. Encabezados del timeline fijos durante scroll. Plantillas y detalle siguen debajo del área principal, accesibles desplazando la página. Build sin errores/advertencias; verificación visual e interacción en navegador pendientes.

### Encabezados compactos del timeline (2026-10-20)

Se redujo el padding vertical de Actividad y los días, el interlineado de los días y la separación de sus horas. La fila conserva altura automática según el contenido. Cambio CSS revisado en fuente; sin verificación visual nueva.

### Actividades con filas compactas (2026-10-20)

Se redujo el mínimo de las filas de actividades a 44 px y el padding/interlineado de su contenido. Crecen cuando el título requiere más líneas; barras más compactas centradas. El espacio final sigue llenando el alto restante. Cambio revisado en fuente; revisión visual pendiente.

### Timeline sin scroll interno (2026-10-20)

Por solicitud del usuario, el panel del timeline deja de tener scroll interno: conserva el alto mínimo disponible y se extiende hacia abajo cuando sus actividades lo requieren. La página permite recorrer ese contenido. El pool conserva su alto disponible y scroll independiente. Cambio CSS revisado en fuente; comprobación visual pendiente.

### Compactación completa del timeline (2026-10-20)

Encabezado y actividades usan tracks max-content para evitar que las filas automáticas absorban altura sobrante. Actividades con mínimo de 40 px y menor padding, interlineado y tipografía; encabezado también compacto. Solo la fila vacía final absorbe el alto libre del panel. Sin scroll interno del timeline. Verificación visual pendiente.

### Corrección de compactación (2026-10-20)

Se restauraron el tamaño anterior de barras y la tipografía de encabezados/actividades. Se conserva únicamente la compactación del espacio interior y el ajuste de filas al contenido, sin estirarlas. Cambio CSS revisado en fuente; sin verificación visual nueva.

### Padding de 2 px en timeline (2026-10-20)

Encabezados y celdas de Actividad usan 2 px de padding. Se eliminaron el mínimo de altura de actividad, padding anidado del título y separación adicional del proyecto. Barras conservan su tamaño; su pista reserva su altura más 4 px. Tracks explícitos se generan como una única cadena de estilo. Revisión visual pendiente.

### Scroll del pool con tema oscuro (2026-10-20)

Barra del pool delgada, con pista del color del panel y control del color de los bordes; contraste mayor al pasar el mouse donde el navegador admite ese estilo. Conserva desplazamiento con rueda y touchpad. CSS scrollbar-width/color y fallback WebKit. Cambio revisado en fuente; revisión visual pendiente.

### Pool siempre igual al timeline (2026-10-20)

Se eliminó el máximo de altura del pool basado en viewport. El pool usa contención de tamaño y stretch: su contenido no determina la altura compartida, que sigue al timeline, incluso cuando este crece hacia abajo. Conserva scroll interno y estilo oscuro. Cambio CSS revisado en fuente; verificación visual pendiente.

### Icono del pool junto al título (2026-10-20)

El indicador de arrastre de cada tarjeta del pool comparte línea con el nombre de actividad mediante una cabecera flex. El título puede envolver si no cabe. Cambio de marcado/CSS revisado en fuente; revisión visual pendiente.

### Pool colapsable (2026-10-20)

Mi semana incorpora enlace visual << en el encabezado del pool y pestaña >> para expandirlo cuando está cerrado, con nombres accesibles. El timeline ocupa el espacio liberado; el estado local se mantiene al refrescar datos/cambiar semana durante la misma instancia de página. No se persiste entre recargas. Revisión visual pendiente.

### Padding del timeline a 5 px (2026-10-20)

Se aumentó el padding compartido de encabezados/celdas del timeline de 2 a 5 px; la pista reserva 10 px adicionales alrededor de la barra sin cambiar su tamaño. Cambio CSS revisado en fuente; revisión visual pendiente.

### Iconos para colapsar el pool (2026-10-20)

Los controles del pool usan SVG de doble chevrón en lugar del texto >>/<<, conservando etiquetas accesibles y comportamiento. Cambio de marcado/CSS revisado en fuente; revisión visual pendiente.

### Jornada confirmada de 45 h (2026-10-20)

El usuario confirmó 8 h de lunes a viernes, 5 h el sábado y 0 el domingo. Sustituye la jornada ilustrativa de 7.5 h/día. Esta fue la regla inicial de creación para capacidad 45 h; desde el 2026-10-23, la jornada se captura explícitamente en Personas y no cambia automáticamente con la bolsa. DataSeeder reutiliza la regla inicial; semilla del modelo actualizada. Migración WorkweekEightFive cambia jornadas anteriores de 7.5 h en personas de capacidad 45 h sin modificar bloques ni registros. Aplicada en SQL local mediante transacción y registrada en __EFMigrationsHistory; lectura posterior confirmó Edgar/Pablo con 8/8/8/8/8/5/0. Build 0 errores/advertencias y 29 pruebas existentes correctas; revisión visual pendiente.

### Etiqueta Completadas (2026-10-20)

Por solicitud del usuario, la métrica Registradas (total) ahora se titula Completadas. Solo cambió la etiqueta: sigue sumando las horas registradas de todas las actividades/semanas, no únicamente actividades terminadas ni la semana seleccionada. Cambio de texto revisado en fuente.

### Orden de métricas (2026-10-20)

Las métricas de Mi semana se muestran en el orden solicitado: Disponibles, Planeadas, Sin asignar, Completadas. Solo cambia el orden visual; cálculos sin cambios. Revisión en fuente, sin verificación visual nueva.

### Colores de métricas (2026-10-20)

Valores de Planeadas en azul, Sin asignar en amarillo y Completadas en verde, usando los colores existentes del tema. Disponibles conserva su color. Cambio de marcado/CSS revisado en fuente; revisión visual pendiente.

### Relleno verde en barras (2026-10-20)

Barras conservan longitud/etiqueta de horas planeadas y añaden relleno verde de tiempo registrado / estimación total actual de la actividad (limitado a 0–100%); actividad completada explícitamente llena la barra. Múltiples bloques de la misma actividad usan la misma proporción. Es indicador de consumo de esfuerzo, no medición independiente de avance físico ni finalización automática; se conserva la regla de revisar estimación al agotarse. Los datos actuales no incluyen porcentaje de avance físico. Verificación visual pendiente.

### Editar horas planeadas del bloque (2026-10-22)

Aclarado: la barra del timeline ya muestra las horas planeadas del bloque (`PlanBlock.PlannedHours`); coincidían con las pendientes porque al arrastrar del pool se inicializan con `RemainingHours`. Se conserva esa lógica de arrastre (el reordenamiento vertical solo cambia la posición). Se añadió en el detalle de la actividad un editor de las horas planeadas del bloque de la semana visible, con máximo = horas pendientes de la actividad (estimación − registradas). Validación en `BlockHoursInput` (máximo dinámico) y en `WeeklyPlanService.SetBlockHoursAsync` (servidor). Nuevo `BlockHoursInputTests` (4 pruebas). Build 0 errores/0 advertencias y 33 pruebas xUnit correctas; revisión visual en navegador pendiente.

### Detalle de la actividad como modal (2026-10-22)

El detalle de la actividad (antes al fondo de la página) ahora se abre como modal sobre un fondo oscurecido, con encabezado, botón ✕ y botón Cerrar. Cierra con clic en el fondo, con ✕, con Cerrar o con Escape; el enfoque al modal se hace con `ElementReference.FocusAsync` (sin JS propio). El contenido no cambió: estado, métricas, Iniciar/Pausar/Reanudar, Registrar tiempo, Completar, Continuar, edición de horas del bloque, registros y continuaciones; el error de las acciones del modal se muestra dentro del modal. Estilos nuevos en `app.css` (`.modal-backdrop`, `.modal`, `.modal-head`, `.modal-close`). Build 0 errores/0 advertencias y 33 pruebas xUnit correctas; revisión visual en navegador pendiente.

### Ajustes del modal y barras en curso (2026-10-22)

Se quitó el botón Cerrar del modal (quedan la ✕, el clic en el fondo y Escape). El nombre del proyecto en el modal usa la clase `.project-name` (color acento). Las barras se distinguen por estado de trabajo: amarillo brillante (`schedule-bar.running`, variable `--running`, con brillo) si la actividad tiene una sesión abierta (contador activo) y amarillo pálido (`schedule-bar.in-progress`, variable `--paused`) si está iniciada pero pausada. Se añadieron `Status` y `HasOpenSession` a `BlockVm` y `WorkRecordService.ListOpenSessionActivityIdsAsync` para conocer las sesiones abiertas. Build 0 errores/0 advertencias y 33 pruebas xUnit correctas; revisión visual en navegador pendiente.


## FEAT-001: unidad de horas en Mi semana (2026-11-20)

Autorizada por Edgar para la copia local. El resumen del modal usa `Format` sin añadir una segunda unidad literal; no cambian números, límites ni guardado. Build correcto (0 errores/advertencias), 69/69 pruebas. Revisión visual de Jarvis en Windows con SQL aislado y datos sintéticos pendiente; FEAT-001 sigue In Progress. Ver [expediente FEAT-001](../features/FEAT-001.md).

## FEAT-002: destinos visibles de arrastre (2026-11-20)

Implementada localmente la señal «Insertar antes» en toda fila poblada y «Agregar al final» en el encabezado y la cola izquierda. La cuadrícula vacía inferior derecha no acepta drop. Reviewer detectó un problema potencial al cruzar hijos de la fila; Developer lo corrigió con comprobación geométrica y QA repitió la revisión estática. Build con salida separada: 0 errores/0 advertencias; `git diff --check` sin errores. `dotnet test` no dio resultado: `vstest` no conectó con `testhost` tras 90 s. QA: BLOCKED para criterios interactivos y persistencia; Reviewer: aprobación técnica limitada. Estado `In Progress`; falta revisión de Jarvis en Chrome con SQL aislado autorizado y datos sintéticos. Revisión exacta y evidencia en [FEAT-002](../features/FEAT-002.md).

## FEAT-003 Google (worktree aislado, 2026-11-21)

Implementación preparada: proveedor oficial de Google, vínculo único por emisor/sujeto, invitación con hash de token, vencimiento, consumo transaccional y auditoría sin secretos; emisión por Administrador activo con sesión Google y bootstrap por proceso de operador sin ruta web de emisión. Selector limitado a `PrivateQa` sintético y loopback; `DataSeeder` deja de ejecutarse automáticamente. La revisión anterior `03716799dbdefd66ca46c9657784393e557c0ed713b00b66b2817139fb112bab` pasó QA independiente (build limpio, 87/87 xUnit), migración en LocalDB sintética nueva y revisión UI privada de Jarvis; Edgar aprobó solo la vista visual. Reviewer solicitó cambios por estado de invitación tras cancelación y excepción del intento concurrente perdedor; `pm_developer` entregó ambos y `pm_qa` independiente dio PASS offline a la corrección `368681aed2b53159fb2f913470acc65c3bd9c0ea069b460013f8744a1d96347f` (build limpio, 90/90 xUnit). Esa revisión no tiene prueba Google/HTTP, carrera SQL Server ni aprobación visual propia. Continúan pendientes la prueba real de Google por Edgar, migración con datos existentes y cualquier despliegue autorizado por separado.
