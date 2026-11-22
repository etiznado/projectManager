---
id: "FEAT-002"
title: "Hacer visible dónde soltar una actividad en Mi semana"
status: "In Progress"
ui_review: "required"
---

# Hacer visible dónde soltar una actividad en Mi semana

## Objetivo y alcance

- Resultado esperado: durante el arrastre de una actividad desde la bolsa, la UI distingue la inserción antes de una fila existente (toda la fila, incluida la franja del timeline) de la opción de agregar al final (encabezado y zona inferior de la columna izquierda «Actividad»).
- Fuera de alcance: seleccionar una fecha horizontalmente, habilitar la cuadrícula vacía inferior derecha, cambiar distribución por días, horario, ordenamiento, cálculo, ubicación o edición de bloques. Se conserva el comportamiento actual de inserción y reordenamiento.

## Criterios de aceptación

- [ ] Al arrastrar desde la bolsa y pasar sobre cualquier lado de una fila existente, una señal visual breve y clara indica «Insertar antes»; al pasar sobre el encabezado o la zona inferior izquierda indica «Agregar al final». No se presenta ningún día específico como destino.
- [ ] Soltar sobre una fila existente por el lado derecho del timeline inserta exactamente una vez antes de esa fila; soltar en una zona de agregado añade exactamente una vez al final. Verificar el orden y que las horas aumenten solo lo que corresponde a cada actividad sintética, también tras recargar.
- [ ] La zona vacía inferior derecha de la cuadrícula y el exterior no aceptan la actividad; cancelar el arrastre tampoco cambia el plan, total u orden.
- [ ] Los indicadores desaparecen al salir de una zona válida, soltar o cancelar, sin dejar un resaltado al recargar.

## UI y vista previa

Jarvis valida la revisión exacta de la feature en «Mi semana», en Chrome sobre localhost con una base SQL aislada, autorizada y con datos sintéticos. Verificar ambas mitades de una fila existente, las dos zonas de agregado, el área vacía inferior derecha, cancelación y persistencia. No se publica vista previa.

## Verificación

- Pruebas y comandos: `dotnet build RaceIT.ProjectManager.sln` y `dotnet test RaceIT.ProjectManager.sln`, sin SQL para las pruebas unitarias. Para UI, solo se permite una base aislada y aprobada con datos sintéticos; nunca usar la base habitual.
- Evidencia por criterio: QA registra revisión exacta, pasos, resultado y capturas sin datos reales.
- Límites o bloqueos: si no está disponible la instancia local aislada, QA informa BLOCKED; no simular una aprobación.

## Handoff y decisiones

- Decisión de alcance de Edgar: aprobó el 2026-11-20 corregir esta distinción de zonas y ejecutar FEAT-002 con los cuatro roles nativos; sin autorización de commit, push, PR, merge, SQL o comprobación de UI por esos roles. Jarvis hace la comprobación de UI después, por separado.
- Arquitecto `pm_architect` (`/root/feat002_architect`): contrastó el alcance con `MyWeek.razor`, `WeeklyPlanService` y el diseño. Propuso indicadores solo en la fila completa, el encabezado y la cola izquierda; conservar inserción, horas y orden. Señaló la revisión interactiva posterior con Jarvis.
- Developer `pm_developer` (`/root/feat002_developer`): añadió los indicadores «Insertar antes» y «Agregar al final» en `MyWeek.razor` y `app.css`, sin cambiar servicios ni datos. Tras el hallazgo del Reviewer, corrigió la salida de zonas anidadas con `weekDrop.js` y comprobación geométrica. Conservó la corrección preexistente de FEAT-001 en el resumen del modal. Revisión local final (SHA256): `MyWeek.razor` `B91B0406B87FC3B0647A381260E4B78EFEA504EA06F8E1D93968E13C2083992D`; `app.css` `97B3612B3A02B375B35DD63019BCEC1C37024588B9D746B4781FFA0A6A66F7C7`; `weekDrop.js` `E45270DAAC1C7367113139A24E3346ADB3A76E61E9CC1BC1AA90AADF7278EE13`.
- QA `pm_qa` (`/root/feat002_qa`): revisión repetida sobre los hashes finales; **BLOCKED** para aceptación completa por interacción y persistencia aún no verificadas por Jarvis. La inspección estática confirmó las zonas y la limpieza del indicador, sin fallo nuevo. `dotnet build RaceIT.ProjectManager.sln --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=bin/verification/ -m:1 -nr:false -v minimal`: PASS, 0 errores y 0 advertencias. Los intentos de `dotnet test` con salida separada abortaron tras 90 s porque `vstest` no conectó con `testhost`; no hay resultado ni conteo de pruebas. `git diff --check`: sin errores (aviso de conversión LF/CRLF).
- Verificación independiente de Jarvis (no sustituye `pm_qa`): build de Windows y suite sobre la revisión final arriba identificada, 69/69 pruebas aprobadas. El intento del rol nativo quedó bloqueado por la conexión entre `vstest` y `testhost`; la aceptación de UI sigue parcial.
- Revisión de UI local (Jarvis, 2026-11-20), parcial, Windows Chrome y datos sintéticos en SQL aislado: soltar por la mitad derecha de una fila poblada insertó un único bloque de 5 h antes de la primera fila; encabezado y cola izquierda añadieron 2 h cada uno al final, una vez cada uno. Total de 19 a 28 h; orden y total persistieron al recargar. La mitad izquierda también insertó 1 h una sola vez antes de la primera fila. Soltar en la cuadrícula vacía inferior derecha o fuera del destino dejó el plan intacto, sin indicadores residuales después de soltar o recargar. Pendiente: observar que las señales no se interrumpan al cruzar elementos dentro de una fila y confirmar desaparición al salir y al cancelar con Escape. La interacción disponible solo ejecuta arrastres completos. No se declara PASS ni aprobación visual; instancia temporal detenida.
- Reviewer `pm_reviewer` (`/root/feat002_reviewer`): primera revisión detectó riesgo P1 de que `dragleave` de hijos borrara el indicador dentro de la misma fila; Developer corrigió y QA repitió. Segunda revisión confirmó la corrección y dio aprobación técnica limitada, sin nuevos hallazgos. No sustituye la verificación de UI ni resuelve el bloqueo de `testhost`.
- PR/revisión final, aprobación de Edgar y evidencia de merge: pendiente; la revisión de UI no sustituye la aprobación final.

No cambies de estado solo porque se completó un campo. Consulta [las transiciones y puertas](README.md).
