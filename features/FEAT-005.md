---
id: "FEAT-005"
title: "Evaluar la organización de los frentes de Edgar"
status: "Ready"
ui_review: "none"
---

# Evaluar la organización de los frentes de Edgar

## Objetivo y alcance

- Evaluar cómo organizar el seguimiento personal de Edgar en Project Manager y cómo Jarvis lo coordina, para que Edgar pueda ver de un vistazo en la TV fija de su oficina los frentes abiertos que importan, sin mantener otro tablero a mano.
- Levantar el estado actual de Initiatives, Panorama, Projects, Activities y expedientes de features: qué registra cada uno, qué usa Edgar hoy, dónde vive el estado autorizado y cómo Jarvis lo lee o lo actualiza. Distinguir las capacidades verificadas de las ideas, el trabajo local no publicado y las fuentes ausentes.
- Fuera de alcance: implementar una solución, escribir código, cambiar datos, agregar conectores o automatizaciones, sincronizar Trello, publicar, desplegar o redefinir Initiatives. No cambiar Mi semana de ningún integrante, ni sus horarios, capacidad, permisos, reglas o planificación semanal. Tampoco cambiar el flujo semanal de Edgar; cualquier cambio futuro se propone por separado.

## Criterios de aceptación

- [ ] El informe mapea cada componente y el trabajo de Jarvis: fuente de verdad y enlace, dueño del dato, último hecho significativo, próximo paso y dónde se duplicaría el estado. Separa estado comprobado, riesgo calculado y estado desconocido. No confunde un refresco de pantalla con progreso.
- [ ] Usa como muestra las conversaciones y la evidencia de ayer y hoy: importación de RaceIT.Legacy, FEAT-003, diagnósticos de Ensenada y Azure como casos a evaluar. La consulta corta ya resuelta sobre Hakko se excluye de los frentes activos. Confirma qué sigue abierto según la evidencia, sin inferir cierre por falta de información.
- [ ] Compara tres opciones: conservar la organización actual, agregar una proyección ligera separada sin duplicar estado, o hacer cambios selectivos de organización. Explica beneficio, costo de mantenimiento, riesgos y huecos de cada una, con recomendación y razones.
- [ ] En las propuestas, el trabajo de código conserva como fuente autorizada la feature vinculada a su proyecto; la TV no mantiene un estado manual paralelo. Un frente no técnico puede quedar independiente sin una iniciativa obligatoria.
- [ ] Define un piloto pequeño que se pueda revisar sin implementar UI: para cada frente, mostrar solo un estado basado en evidencia y un próximo paso; responsable o fecha de revisión solo si aporta. La TV sería de solo lectura, legible a distancia y sin datos sensibles, enlaces privados ni detalles que no deban quedar a la vista. Los frentes resueltos no permanecen en la vista activa.
- [ ] Entrega la recomendación con brechas verificadas, el piloto propuesto y una matriz explícita de qué conservar, cambiar o aplazar. Documenta las fronteras de regresión de Mi semana y demás flujos antes de proponer cualquier UI. No elige ni construye una solución en este expediente.

## UI y vista previa

`ui_review: none` porque este expediente es solo un estudio. No se diseña ni se entrega una UI implementada. Una nueva pantalla o cambios visibles requerirán una propuesta separada y la revisión de UI aplicable.

## Verificación

- Método: revisión de solo lectura de fuentes, expedientes, vistas y reglas actuales. Registrar en el informe la revisión consultada y sus enlaces o referencias privadas en el canal apropiado.
- Evidencia: matriz de componentes/fuentes, contraste de los casos de la muestra, alternativas, piloto y fronteras de regresión. No insertar datos sensibles ni enlaces privados en un documento publicable.
- Límites: no ejecutar build, iniciar la aplicación ni acceder a SQL para crear este borrador. La realización del estudio, sus conclusiones y cualquier implementación quedan pendientes de dirección posterior.

## Handoff y decisiones

- Origen del borrador: solicitud del dueño, 2026-11-21.
- Decisión de alcance y criterios para `Ready`: Aprobación del dueño registrada en seguimiento privado, 2026-11-21. No se autoriza iniciar automáticamente, abrir PR, fusionar ni desplegar.

Consulta [las transiciones y puertas](README.md).
