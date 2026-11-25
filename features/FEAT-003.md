---
id: "FEAT-003"
title: "Agregar inicio de sesión con Google"
status: "Ready"
ui_review: "required"
---

# Agregar inicio de sesión con Google

## Alcance aprobado para esta implementación (2026-11-21)

La instrucción del dueño para esta ejecución prevalece sobre frases antiguas de «propuesta pendiente» y «pendiente antes de Ready» más abajo. Google complementa el acceso aprobado; no abre registro. Solo un Administrador activo con sesión Google confiable puede emitir una invitación privada para una Persona activa y un correo Google previsto. El servidor verifica el correo de Google y vincula por `issuer` y `sub` estables durante el consumo único y transaccional. La primera invitación de Administrador se emite únicamente mediante un proceso privado de operador, sin endpoint público de emisión. El selector provisional exige habilitación explícita en `PrivateQa`, datos sintéticos y conexión de loopback; esa sesión no puede emitir invitaciones. No se aprobaron revocación, reemisión ni cambios generales de ciclo de vida de cuentas. No se ejecutaron invitaciones reales, credenciales, migraciones en la base del dueño, despliegue ni prueba con Google real.

## Handoff de implementación aislada

- Architect real: plan entregado sin archivos modificados; identificó selector anónimo sin guardia y `DataSeeder` automático que sobrescribía Persona 1.
- Developer real: código de Google, invitaciones, bootstrap privado, auditoría, migración y pruebas en este worktree; revisar `git diff` para la versión exacta sin commit.
- En la revisión anterior `03716799dbdefd66ca46c9657784393e557c0ed713b00b66b2817139fb112bab`, QA independiente aprobó restore, build limpio y 87/87 xUnit. Se aplicó `GoogleAccess` solo en una LocalDB sintética nueva. Jarvis comprobó el selector y la UI privada con personas ficticias; Edgar aprobó **solo la vista visual**. No se probó Google real ni una migración sobre datos existentes.
- Reviewer encontró que una invitación podía persistir tras cancelar Google y que el intento concurrente perdedor podía lanzar una excepción. `pm_developer` corrigió el estado temporal por propósito de intento y añadió manejo de rechazo concurrente con auditoría; `pm_qa` independiente dio PASS offline a la revisión `368681aed2b53159fb2f913470acc65c3bd9c0ea069b460013f8744a1d96347f` (build limpio, 90/90 xUnit). Los recorridos Google/HTTP, la carrera SQL Server y la revisión visual de esta nueva revisión siguen pendientes. La demo publicada anterior conserva la huella anterior.
- El comando `--issue-bootstrap-invitation` es exclusivo de operador y requiere las variables privadas `RACEIT_BOOTSTRAP_PERSON_ID`, `RACEIT_BOOTSTRAP_EMAIL`, `RACEIT_BOOTSTRAP_ISSUER`, `RACEIT_BOOTSTRAP_SUBJECT` y `RACEIT_BOOTSTRAP_EXPIRES_UTC`, además de Google configurado. La cuenta exacta se coteja por issuer/sub y el primer consumo cierra permanentemente el bootstrap. No se ejecutó con una identidad real.
- QA sintética requiere una conexión exacta a la instancia LocalDB `(localdb)\RaceIT_PM_SyntheticQA` y a la base `RaceIT_PM_SyntheticQA`; la cadena de la base `projectManager` del dueño queda rechazada aun con flags de QA. La base y los datos de prueba son exclusivos de este canario, no del entorno del dueño.
- Incidente de verificación: al retirar una primera versión de la migración, `dotnet ef migrations remove --force --no-build` consultó la base SQL local configurada (`SELECT 1`, `OBJECT_ID` y `__EFMigrationsHistory`) sin autorización para tocar SQL. Fueron lecturas; no se ejecutó una actualización de datos o esquema allí. Posteriormente se corrigió el snapshot, QA comprobó el modelo offline y la migración se aplicó solo a la LocalDB sintética autorizada.

## Objetivo y alcance

- Agregar «Continuar con Google» sin eliminar el selector provisional de personas. Ambos flujos deben conservar la identidad interna y los permisos existentes cuando estén habilitados.
- Hoy `/login` muestra personas activas; `POST /session/login` valida el ID activo y emite la cookie `RaceIT.Session` con la identidad y el rol interno Administrador/Usuario. El selector no verifica contraseña ni identidad. El cierre de sesión ya existe.
- Propuesta pendiente de aprobación: Google solo para personas activas ya existentes, con vínculo explícito autorizado por el administrador y verificado contra el identificador estable de Google. No vincular solo por coincidencia de correo ni permitir que una sesión del selector reclame una identidad Google.
- Decisión del dueño: conservar el selector sin contraseña para que Jarvis ejecute pruebas funcionales y de UI sin una cuenta de Google ni intervención de Edgar. Solo se habilita expresamente en local o en un entorno de pruebas dedicado, privado y aislado con datos ficticios.
- Decisión de Edgar: solo un Administrador activo y autenticado en Project Manager por el flujo de confianza puede aprobar un vínculo Google. El servidor verifica esa autorización; la aprobación no sustituye la prueba de identidad de Google.
- Decisión de incorporación: en Personas, un Administrador autorizado crea o elige una Persona activa y le asigna su rol local. El Administrador, autenticado por el flujo de confianza, emite deliberadamente una invitación para la cuenta Google prevista; emitirla constituye la aprobación y no requiere una segunda revisión. No hay registro abierto. La invitación vence, es de un solo uso y no sustituye la verificación independiente de Google. Aceptación y vínculo por el identificador estable se guardan juntos, de forma atómica.
- Decisión de Edgar: Edgar será el primer Administrador y el primer aprobador de invitaciones. Vincular su Persona local existente si la hay. Edgar ya eligió la cuenta de Google y la autenticará personalmente. El identificador exacto se proporcionará por separado durante el alta inicial de confianza y no debe guardarse en el repositorio. El alta inicial se hará mediante una invitación con vencimiento y de un solo uso, emitida exclusivamente por un operador autorizado desde el proceso privado y de confianza del despliegue, y solo si aún no existe un Administrador activo vinculado. La invitación se limita a la cuenta Google seleccionada, verificada independientemente, y a la Persona local existente cuando corresponda. Edgar se autentica personalmente; al aceptar con éxito se vincula la identidad, se consume la invitación y se cierra el alta inicial de forma atómica. No se ha ejecutado. Jarvis solo podrá preparar esa invitación cuando la función exista y haya autorización en ese momento.
- El alta inicial no puede iniciarse desde el selector sin contraseña ni desde un endpoint público de emisión. No se elige al primer visitante ni se verifica la identidad por correo solamente. Registrar eventos de emisión, aceptación y rechazo para auditoría, sin tokens, credenciales ni otros secretos.
- En cualquier entorno accesible públicamente, deshabilitar tanto el formulario como su endpoint: ocultarlo no basta y no puede ser alternativa a Google. Configurar ambas opciones por separado.
- Fuera de alcance: registro público, alta automática, otras identidades, rediseño de roles, migración o cambios de producción, despliegue y creación de credenciales. No guardar secretos en el repo.

## Criterios de aceptación propuestos

- [ ] Una identidad Google vinculada a una persona activa inicia sesión con el mismo ID interno y acceso vigente; probar Administrador y Usuario. La cuenta Google no concede ni cambia roles.
- [ ] El servidor permite aprobar un vínculo solo a un Administrador activo con sesión autenticada por el flujo de confianza. Un Usuario, un administrador inactivo, una solicitud sin sesión o una sesión del selector no pueden aprobarlo, incluso por solicitud directa: no se crea ni modifica el vínculo ni cambia el acceso.
- [ ] La Persona activa acepta una invitación vigente con la cuenta Google prevista y verificada; la invitación se consume y el identificador estable queda vinculado de forma atómica. Conserva la Persona y el rol local, sin segunda revisión ni registros duplicados.
- [ ] Una cuenta incorrecta o no verificada, invitación vencida o ya utilizada, o emisión no autorizada no concede acceso ni cambia vínculos o roles. Reintentos simultáneos no pueden usar una misma invitación más de una vez ni duplicar accesos.
- [ ] Una misma identidad estable de Google no da acceso a dos personas internas. Si ya existe un vínculo, un intento conflictivo no lo reasigna ni abre una sesión para otra persona.
- [ ] Solo un operador de despliegue autorizado puede emitir la invitación inicial desde el proceso privado y solo si no existe un Administrador activo vinculado. El selector, un visitante y una solicitud pública no pueden emitirla ni reclamarla.
- [ ] Edgar se autentica con la cuenta Google prevista y verificada; una aceptación válida consume la invitación, vincula la identidad estable a la Persona prevista y cierra el alta inicial atómicamente. Cuenta equivocada, invitación vencida o reutilizada, o reintento concurrente no concede rol ni sesión ni altera la asociación. Los eventos se auditan sin secretos.
- [ ] Cancelar Google, no autorizar, recibir una respuesta inválida, usar una cuenta no vinculada o una persona inactiva no inicia sesión, no crea ni vincula personas y no abre el selector como alternativa inesperada. El mensaje de error no revela cuentas internas.
- [ ] Jarvis puede verificar en forma autónoma el acceso de Usuario y Administrador, las rutas protegidas y el cierre de sesión por el selector local/aislado, solo con datos ficticios, sin Google ni Edgar. El inicio sigue limitado a personas activas.
- [ ] En un entorno públicamente accesible no aparecen el selector ni la lista de personas y el POST directo no abre sesión, incluso si Google está deshabilitado, falla o está mal configurado.
- [ ] Edgar realiza la prueba manual con Google real: confirma que la cuenta vinculada abre la misma identidad interna, con sus permisos, y permite cerrar la sesión. Se registra aparte; las pruebas de Jarvis no sustituyen esa aceptación.
- [ ] Con Google deshabilitado, no aparece su botón y ningún endpoint de Google inicia una sesión. Cuando está habilitado y el selector no, solo Google puede iniciar una sesión. Sin un método habilitado, el acceso falla de forma segura.
- [ ] Sin sesión no se puede acceder por URL directa a las áreas protegidas. Usuario no obtiene Administración ni acceso a datos de otra persona; Administrador conserva el acceso autorizado.
- [ ] Cerrar sesión invalida la sesión local de ambos flujos; volver o recargar una ruta protegida no la restaura ni expone datos. No se requiere cerrar la cuenta Google global.

## UI y vista previa

Jarvis ejecutó la revisión funcional y de UI de la revisión anterior `03716799dbdefd66ca46c9657784393e557c0ed713b00b66b2817139fb112bab` con el selector en una instancia local privada y datos ficticios; Edgar aprobó solo su aspecto visual. La corrección posterior aún no tiene revisión visual propia. No se usó Azure. Edgar hará la prueba interactiva con Google en una configuración de prueba autorizada; sin ella, la prueba real permanece pendiente/bloqueada. No publicar la vista ni usar datos existentes.

## Verificación

- Pruebas y comandos: `dotnet build RaceIT.ProjectManager.sln` y `dotnet test RaceIT.ProjectManager.sln`; cubrir reglas de acceso y configuración sin SQL externo, más los recorridos privados indicados arriba.
- Evidencia por criterio: la revisión anterior tiene QA offline, canario sintético y revisión visual aprobada; la corrección posterior `368681aed2b53159fb2f913470acc65c3bd9c0ea069b460013f8744a1d96347f` tiene PASS offline de `pm_qa` (90/90 xUnit), pero no recorrido Google/HTTP ni revisión visual propios. Google real y migración de datos existentes siguen pendientes.
- Referencias para el diseño: [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect) y [autenticación social sin ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/social-without-identity?view=aspnetcore-10.0).

## Handoff y decisiones

- Decisión del dueño (conversación de la feature): conservar el selector para QA autónomo de Jarvis y que Edgar verifique el acceso real con Google. Pendiente antes de `Ready`: ya se decidió que solo un Administrador puede aprobar vínculos; se aprobó la invitación emitida por el Administrador sin segunda revisión; quedan por definir plazo de vencimiento, canal de entrega y evidencia concreta, sin imponer infraestructura de correo; el alta inicial privada de Edgar y la prueba de identidad Google quedaron acordadas, sin ejecutar; si revocación, reemisión o sustitución se incluyen y quién las autoriza; en qué entornos se probarán QA aislado y Google real; y aprobar el alcance completo.
- Refinamiento del Requirements Analyst: propuestas de vínculo único, alta inicial independiente del selector y decisiones anteriores. Las propuestas solo se consideran aprobadas cuando la decisión de Edgar queda registrada.
- Plan técnico (Architect): completado para el alcance aprobado.
- Cambios de Developer: `pm_developer` entregó la corrección de hallazgos del Reviewer en la revisión `368681aed2b53159fb2f913470acc65c3bd9c0ea069b460013f8744a1d96347f`; la revisión visual anterior corresponde a `03716799dbdefd66ca46c9657784393e557c0ed713b00b66b2817139fb112bab`.
- QA: `pm_qa` independiente dio PASS offline a la corrección (build limpio, 90/90 xUnit); la revisión anterior tuvo 87/87 xUnit. Pruebas Google/HTTP y SQL Server de la corrección pendientes.
- Vista previa y decisión de UI del dueño: Jarvis comprobó la revisión anterior con datos sintéticos; Edgar aprobó solo esa vista visual. La corrección no hereda esa aprobación.
- Reviewer: solicitó corregir estado de invitación tras cancelación y resultado concurrente perdedor; nueva revisión técnica pendiente tras QA.
- PR/revisión final, aprobación del dueño y evidencia de merge: pendiente.

Consulta [las transiciones y puertas](README.md).

## Cierre de la prueba de acceso

- Validación manual de la compilación de prueba: canje de la invitación inicial, acceso a Administración y nuevo inicio de sesión con Google después de cerrar sesión, sin invitación.
- Por decisión del dueño, los rechazos faltantes en la auditoría se atienden en el siguiente feature junto con la limpieza de la pantalla de inicio de sesión. No se cambió esta implementación.
- La prueba adicional de redención simultánea en SQL Server no se ejecutó y no es requisito de este cierre. Se conservan la regla de un solo uso, sus protecciones y las pruebas existentes.
- No se probó el canje de una segunda persona ni se aprueba un despliegue de producción con esta validación.
