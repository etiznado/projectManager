---
id: "FEAT-004"
title: "Repartir una actividad entre días en Mi semana"
status: "Draft"
ui_review: "required"
---

# Repartir una actividad entre días en Mi semana

## Objetivo y alcance

- Planear una misma actividad en días elegidos de una semana. Ejemplo: «Actualizar inscritos», 15 minutos de lunes a sábado, 90 minutos semanales y nada el domingo.
- Conservar una sola actividad, su responsable, proyecto, estimación e historial. Repartir tiempo planeado no registra tiempo trabajado ni cambia el estado por sí solo.
- Hoy Mi semana ubica los bloques secuencialmente según sus horas y la jornada configurada; una barra que cruza días no equivale a seleccionar fechas. La jornada diaria y la bolsa semanal se muestran por separado.
- Propuesta de v1, pendiente de aprobación: un responsable, una misma duración para todos los días seleccionados, dentro de una sola semana; no repetir automáticamente en otras semanas. Mantener la planificación actual de un solo día y los bloques ya existentes.

## Criterios de aceptación

- [ ] Antes de guardar, mostrar los días seleccionados, minutos por día y total calculado. Con 15 minutos de lunes a sábado, la vista previa y el plan guardado suman 90 minutos, con cero el domingo. Un día marcado cuenta una sola vez.
- [ ] Mostrar cada tramo en su día explícito, sin trasladarlo silenciosamente a otro. Al seleccionar un día sin jornada o con capacidad insuficiente, mostrar el conflicto de forma clara antes de guardar; acordar si se bloquea o se permite con aviso.
- [ ] Los tramos cuentan una sola vez en las horas planeadas de la semana y del día. Planear o editar el reparto no aumenta las horas registradas o completadas ni cambia la estimación original.
- [ ] Tras recargar o cambiar de semana y volver, conservar días y minutos. Cambiar o quitar un día ajusta los totales sin duplicar tramos; cancelar no guarda cambios. No guardar si no se eligen días o el tiempo no es positivo.
- [ ] Todos los tramos abren la misma actividad; las listas, el proyecto y el historial no la duplican. El flujo existente de un día conserva su comportamiento.
- [ ] Pendiente de decisión: si cada tramo puede marcarse completado por separado y cómo eso se distingue de completar la actividad entera. No asumir que completar un tramo completa la actividad ni que adapta automáticamente los demás.

## UI y vista previa

Prueba privada e interactiva con datos ficticios: elegir lunes a sábado, introducir 15 minutos, revisar el total, guardar y recargar; cambiar un día y revisar capacidades y totales. Comprobar una actividad de un solo día en el flujo existente. Jarvis revisa y Edgar decide sobre la versión exacta.

## Verificación

- Pruebas y comandos: `dotnet build RaceIT.ProjectManager.sln`, `dotnet test RaceIT.ProjectManager.sln`; cubrir cálculo, persistencia, edición y no duplicación. Para UI usar únicamente una instancia aislada con datos ficticios y autorización vigente.
- Evidencia por criterio: pendiente.
- Límites o bloqueos: falta acordar si el exceso de jornada se bloquea o avisa, y cómo se completa un tramo frente a toda la actividad.

## Handoff y decisiones

- Decisión de alcance del dueño (referencia verificable y versión): pendiente de aprobación. Confirmar duración uniforme de v1, ausencia de recurrencia automática, política de capacidad y reglas de completado.
- Plan técnico (Architect): pendiente.
- Cambios de Developer (revisión exacta): pendiente.
- QA (PASS / FAIL / BLOCKED y evidencia): pendiente.
- Vista previa y decisión de UI del dueño (si aplica; versión exacta): pendiente.
- Reviewer: pendiente.
- PR/revisión final, aprobación del dueño y evidencia de merge: pendiente.

Consulta [las transiciones y puertas](README.md).
