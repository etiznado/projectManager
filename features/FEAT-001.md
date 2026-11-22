---
id: "FEAT-001"
title: "Corregir la unidad de horas duplicada en Mi semana"
status: "UI Review"
ui_review: "required"
---

# Corregir la unidad de horas duplicada en Mi semana

## Objetivo y alcance

- Resultado esperado: el detalle de un bloque usa una sola unidad `h` por valor de horas.
- Fuera de alcance: cambiar formato decimal, redondeo, límites de horas, lógica de guardado o datos históricos que ya superan el máximo. Que el bloque de muestra esté por encima del máximo es un hallazgo aparte, no un requisito de esta feature.

## Criterios de aceptación

- [x] Al abrir el modal de una actividad planeada, el resumen no contiene `h h`; por ejemplo, `7.5 h h · máximo 6 h h` pasa a `7.5 h · máximo 6 h`, respetando el separador decimal que ya use la aplicación.
- [x] Verificar un valor entero y otro fraccionario: no cambian los números, el redondeo ni el idioma existente.
- [x] En la base sintética, editar y guardar un valor válido (por ejemplo, 6 h si quedan 6 h); cerrar y volver a abrir tras recargar. El valor persiste y conserva una sola unidad por cifra.

## UI y vista previa

Jarvis abre `/mi-semana` en Chrome contra una instancia temporal en localhost con una base SQL aislada, autorizada y con datos sintéticos. No se publica vista previa. Abrir el modal desde la barra del timeline; documentar el caso entero, el fraccionario y la reapertura tras guardar. El baseline muestra el defecto en `Horas planeadas del bloque`.

## Verificación

- Pruebas y comandos: `dotnet build RaceIT.ProjectManager.sln` y `dotnet test RaceIT.ProjectManager.sln`, sin SQL para las pruebas unitarias. Para UI, solo se permite una base aislada y aprobada con datos sintéticos; nunca usar la base habitual.
- Evidencia por criterio: QA registra revisión exacta, pasos, resultado y capturas sin datos reales.
- Límites o bloqueos: si no está disponible la instancia local aislada, QA informa BLOCKED; no simular una aprobación.

## Handoff y decisiones

- Decisión de alcance de Edgar (referencia verificable y versión): autorización comunicada por el hilo coordinador el 2026-11-20 para FEAT-001 únicamente, con alcance y criterios de este expediente; sin autorización para FEAT-002, SQL, migraciones, commit, push o PR.
- Plan del Arquitecto (fase manual; roles personalizados no seleccionables en esta sesión): quitar solo las dos unidades literales tras Format en MyWeek.razor; conservar formato, validación y persistencia.
- Cambios de Developer (fase manual): MyWeek.razor, una línea; se quitaron las dos ` h` redundantes. Base HEAD bd77ee8c07fd; diff local sin commit.
- QA (fase manual; roles personalizados no disponibles en esta sesión): build PASS (0 errores, 0 advertencias), 69/69 pruebas unitarias. La revisión de UI independiente también pasó con la misma corrección.
- Revisión de UI local (Jarvis, 2026-11-20): PASS en Chrome de Windows, `/mi-semana`, localhost y SQL aislado con datos sintéticos. Una unidad por valor en `7.5 h · máximo 8 h` y `6 h · máximo 6 h`. El navegador rechazó 7 h sobre el máximo de 6 h. Al guardar 5 h, recargar y reabrir se leyó `5 h · máximo 6 h`. Captura observada en la sesión local, sin archivo adjunto. Instancia detenida. Revisión: `MyWeek.razor` SHA-256 `5b0772c25451d6b94c04f0ea564d02e8fd643630559f85c4adbbced7bca2863a` (diff local, sin commit).
- Reviewer (inspección preliminar manual): el diff de implementación es una sola línea de marcado; no cambia `Format`, límites, validación ni guardado. No hubo ejecución de roles personalizados. Pendientes la aprobación de UI de Edgar y la revisión formal; no se abrió PR.
- PR/revisión final, aprobación de Edgar y evidencia de merge: pendiente; la revisión de UI no sustituye la aprobación final.

No cambies de estado solo porque se completó un campo. Consulta [las transiciones y puertas](README.md).
