# Aplicación local de prueba

Desde la carpeta de trabajo, en una terminal PowerShell de VS Code (Windows PowerShell 5.1 o PowerShell 7):

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Start-LocalTest.ps1
```

El permiso de ejecución se limita a esa terminal y termina al cerrarla; no modifica la política permanente del equipo. Los archivos `.ps1` deben ejecutarse con PowerShell, no directamente como scripts de Git Bash. El arranque carga explícitamente la biblioteca DPAPI correspondiente a la edición de PowerShell.

El comando compila la aplicación real y la inicia en segundo plano con el perfil `PrivateQa` aprobado. Si esa misma instancia ya existe, informa su estado en lugar de duplicarla. Espera una respuesta del login antes de comunicar que arrancó. Abre la dirección de navegador anotada en la configuración privada, elige el usuario sintético autorizado en el selector y entra en **Frentes abiertos**.

Para detenerla:

```powershell
.\scripts\Stop-LocalTest.ps1
```

La parada comprueba PID, hora de creación y ejecutable; solicita cierre normal únicamente a esa instancia. No mata procesos por nombre/puerto y no modifica la base. Volver a ejecutar el primer comando conserva los frentes, historial, permisos, credencial existente y certificado. El perfil normal de Development y sus tareas de depuración no seleccionan este entorno automáticamente.

## Preparación privada del operador

El coordinador debe preparar una sola vez `.local-test/approval.json` y `.local-test/runtime.json` bajo la autorización registrada en el issue. La carpeta está ignorada por Git. Arrancar no crea ni actualiza la aprobación y no enrola otro destino si los valores cambian.

Ambos archivos contienen de forma independiente la misma tupla aprobada: `ProfileId`, `ConnectionString`, `ConnectorId`, `PersonId`, `AllowedFrontIds`, `TokenSha256`, `UiUrl`, `ApiUrl`, `PfxPath`, `PfxPasswordDpapiPath`, `PublicCertificatePath`, y los cuatro indicadores booleanos `ExplicitlyEnabled`, `SyntheticDataOnly`, `SelectorEnabled`, `JarvisEnabled`. Los indicadores deben ser verdaderos. Se comparan destino, identidad, acceso, digest y rutas antes de crear contextos o habilitar el selector. No copiar los valores privados a documentación, GitHub, launch.json ni archivos rastreados.

El booleano opcional `AllowAllOwnerFronts` debe coincidir en runtime y aprobación: omitido equivale a `false`; una propiedad presente debe ser `true` o `false`, sin comillas ni null. Con `true`, Jarvis puede gestionar todas las tarjetas actuales y futuras del propietario activo configurado sin agregar IDs. Con `false` conserva los IDs explícitos y el acceso derivado de sus propias creaciones. Cambiarlo requiere autorización y parada/arranque; ejecutar Start sobre una instancia existente no recarga su configuración. La validación de tipo/coincidencia sucede incluso antes de informar que la instancia ya está encendida, y un rechazo no compila ni arranca otro ambiente. Los demás valores privados y protecciones permanecen fijados.

La conexión debe ser la del fixture sintético existente autorizado, con autenticación integrada, sin adjuntos, failover ni credenciales SQL. No se infiere aprobación de una cadena y una bandera: se exige la tupla independiente. No hay creación de base, semilla, reparación ni aplicación de migraciones en el arranque; falta de esquema/aprobación provoca rechazo sin alternativa hacia la base habitual.

Se conserva únicamente el digest de la credencial de Jarvis que ya funciona; el token original no se persiste ni rota al reiniciar. Las direcciones privadas se conservan: UI HTTP exclusivamente loopback y API HTTPS exclusivamente loopback. La API sigue rechazando Bearer por HTTP y cookies como credencial del conector. El selector, login POST y revalidación de cookie comparten la decisión validada; su cookie usa un nombre separado del acceso normal. El guard histórico del fixture anterior no cambia.

El PFX local cifrado se conserva entre arranques. Su contraseña UTF-8 se protege en un archivo binario con `ProtectedData.Protect(..., CurrentUser)`; el launcher la descifra solo para el entorno del hijo y restaura su propio entorno inmediatamente. No hay contraseña en argumentos/logs. Windows usa un contenedor de clave temporal al cargar el PFX, sin `PersistKeySet`; el cierre normal dispone el certificado. No se instala confianza global. El certificado público se exporta al lugar privado ya utilizado por Jarvis y permanece estable mientras se conserve el mismo PFX. El cliente valida ese PEM con su configuración privada o `--cacert`.

Los logs y el estado de proceso permanecen en `.local-test/`, sin cuerpos/cabeceras de autenticación ni digest. El perfil rechaza endpoints Kestrel o URL/puertos adicionales configurados desde el entorno, y mantiene comprobación loopback por solicitud. No añade CORS, proxies confiables, LAN, túneles ni reglas de firewall.

## Actualización de Frentes abiertos

La página consulta sus datos cada **30 segundos** mientras está abierta, sin recargar el documento ni navegar. Así puede mostrar el estado, último avance y próximo paso guardados por Jarvis. Las lecturas respetan el propietario de la sesión y no generan historial.

El sondeo omite un turno si hay guardado, recarga explícita u otra lectura en curso. Conserva errores/conflictos de cada ficha hasta su resolución explícita. Si una consulta periódica falla, quedan las últimas fichas válidas y el siguiente turno reintenta. Al salir se cancela el ciclo; una lectura tardía no actualiza una página eliminada.

Compilación/pruebas automatizadas no sustituyen la comprobación de arranque/reinicio ni la revisión de Edgar observando un cambio real aparecer en el siguiente turno. La aprobación del perfil local no autoriza bases habituales, más permisos, publicación o despliegue.
