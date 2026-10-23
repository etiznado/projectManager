# Product context

Última actualización: 2026-10-23 (America/Denver). Describe el modelo vigente de esfuerzo y plan semanal (`Project_Manager_Notas_Diseno.md`); el modelo anterior quedó descartado.

## Estructura del trabajo

Iniciativa -> proyecto -> actividad. Un proyecto puede ser independiente (sin iniciativa) y una actividad puede ser suelta (sin proyecto). Las actividades se definen por esfuerzo estimado, sin fechas obligatorias; se programan después en el plan semanal. Las dependencias quedaron aplazadas en el formulario de creación.

## Separación de conceptos

| Concepto | Significado |
| --- | --- |
| Estimación total actual | Esfuerzo total esperado de la actividad |
| Horas de un bloque semanal | Parte del esfuerzo planeado para una posición de la semana |
| Tiempo registrado | Trabajo humano realmente realizado |
| Horas pendientes | Estimación actual - tiempo registrado, mientras la actividad siga abierta |
| Terminación | Confirmación explícita de que el trabajo quedó terminado |
| Fecha meta | Entrega deseada del proyecto o iniciativa |

Regla crítica: consumir las horas estimadas no termina automáticamente una actividad ni demuestra el mismo porcentaje de avance. Si se agota la estimación y sigue abierta, se señala "revisar estimación".

## Dos formas de planeación

| Forma | Uso |
| --- | --- |
| Semanal | Empleado: elige actividades, asigna horas y ordena bloques en Mi semana |
| Por entregas | Controla iniciativas y proyectos por fechas meta y horas pendientes, sin calendario semanal obligatorio |

## Roles y permisos (implementados)

| Rol | Permisos |
| --- | --- |
| Administrador | Ver y editar todo |
| Usuario | Ver y gestionar lo suyo y lo asignado |

Login provisional por selección de persona (sin contraseña) con cookie de sesión. El login real y el proveedor de identidad siguen pendientes.

## Pantallas

- `/login`: selección de usuario (acceso provisional).
- `/mi-semana`: navegación semanal, métricas (Disponibles, Planeadas, Sin asignar, Completadas), pool colapsable con arrastre al timeline, timeline con barras proporcionales (relleno verde de consumo y borde por estado de trabajo) y detalle de la actividad en modal con registro de tiempo y edición de horas del bloque.
- `/mis-proyectos`: Usuario consulta sus proyectos, crea proyectos independientes (sin iniciativa) y edita nombre/fecha meta de los asignados.
- `/initiatives`: vista de solo lectura de iniciativas y proyectos con métricas, banner de riesgo, auto-refresco y pantalla completa.
- `/administracion`: personas, iniciativas, proyectos, actividades y plantillas (solo Administrador), organizadas por secciones; se muestra una categoría a la vez.

Administración permite editar proyectos, incluidos iniciativa y responsable. Cambiar el responsable del proyecto no reasigna automáticamente las actividades, que tienen responsable propio.
Eliminar una plantilla desde Administración requiere confirmación en un diálogo que identifica la plantilla; las actividades generadas anteriormente permanecen.
En cada sección de Administración el formulario de crear/editar está arriba y la lista de entidades abajo. Pulsar Editar carga la entidad en el mismo formulario; Guardar o Cancelar edición vuelve al modo de creación. Plantillas usa un formulario único para nombre, categoría y elementos; debe quedar al menos un elemento. Los cambios afectan futuras generaciones.

Personas incluye siete controles para la jornada diaria, tanto en alta como en edición. La bolsa semanal es independiente: cambiarla conserva las horas diarias, cuya suma puede diferir de la bolsa. Los campos de fecha meta de Iniciativas y Proyectos permiten escritura manual y muestran el selector de calendario nativo del navegador.

Usuario también puede crear actividades desde un modal de Mi semana. Guardarlas las incorpora al pool, sin agregar un bloque al plan. Crear iniciativas y asociar proyectos a iniciativas corresponde al Administrador.

El modal de Mi semana crea trabajo propio: solo ofrece proyectos del responsable actual, incluso si quien lo usa es Administrador. La creación global del Administrador está en Administración.

## Reglas acordadas

- Crear una actividad no exige fechas; guarda su estimación antes de programarla. El uso de dependencias en el formulario quedó aplazado.
- Horas pendientes = estimación - tiempo registrado.
- Las horas planeadas del bloque no pueden superar las horas pendientes de la actividad.
- Terminar es una acción explícita, independiente de horas.
- Dependencias se validan contra ciclos; no desplazan fechas automáticamente.
- Jornada confirmada: 8 h lun-vie, 5 h sáb, 0 h dom (45 h/sem); configurable por persona.
- Un solo contador activo por persona: iniciar otra actividad pausa la anterior.

## Fuera del MVP

Milestones y bloqueos quedan fuera de este MVP. También: prioridades, recomendaciones automáticas, botón Split, motor avanzado de reprogramación, contabilidad definitiva de continuaciones y login real.
