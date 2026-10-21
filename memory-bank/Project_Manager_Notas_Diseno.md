# Project Manager — Resumen del MVP para implementación

20 de octubre de 2026. Documento de diseño consolidado; no implica que estas funciones ya existan. Sustituye las propuestas anteriores cuando hubo cambios. La sesión no incluyó implementación.

## Objetivo y contexto

Aplicación para mostrar en una TV de 32 pulgadas el estado de iniciativas y proyectos y el plan semanal del empleado, con avance real y alertas de riesgo. El administrador y el empleado deben poder entender cómo va el trabajo.

Existe, según el usuario, una aplicación funcional en Blazor con .NET 10 y conexión a SQL en su computadora. Ese código no se revisó aquí. Antes de modificarlo, contrastarlo con este documento. Construir por etapas pequeñas y validar en uso real.

## Estructura del trabajo

- Iniciativa → proyecto → actividad.
- Un proyecto puede pertenecer a una iniciativa o ser independiente.
- Una actividad puede pertenecer a un proyecto o ser una tarea suelta.
- Crear una actividad no exige fechas de inicio y fin: se registra su esfuerzo estimado y sus dependencias antes de programarla.
- Proyectos e iniciativas tienen fechas meta cuando corresponda. Una entrega a cinco semanas no significa cinco semanas continuas de trabajo.
- Las dependencias entre actividades representan también relaciones entre proyectos. No se acordó agregar otro mecanismo independiente de dependencias entre proyectos.

Ejemplos: Data Logger agrupa PCB, case interno, case externo y selección de batería. Los cajones del trailer son un proyecto independiente con cierre externo y organización Gridfinity. Aceite, reparaciones y conector Starlink son actividades sueltas. La operación de carreras se genera desde plantillas de antes, durante y después.

## Separar estimación, plan y trabajo real

| Concepto | Significado |
| --- | --- |
| Estimación total actual | Esfuerzo total esperado de la actividad |
| Horas de un bloque semanal | Parte del esfuerzo que se planea realizar en una posición de la semana |
| Tiempo registrado | Trabajo humano realmente realizado |
| Horas pendientes | Estimación actual menos tiempo trabajado, mientras la actividad siga abierta |
| Terminación | Confirmación explícita de que el trabajo quedó terminado |
| Fecha meta | Entrega deseada del proyecto o iniciativa |

Una actividad de 12 h puede recibir solo 4 h de trabajo planeado esta semana. El resto sigue pendiente.

Si una actividad estimada en 10 h ya consumió 4 h y ahora faltan 9 h, se cambia su estimación total a 13 h. El proyecto actualiza sus totales y el cálculo de riesgo. Los registros se suman por proyecto e iniciativa sin duplicarlos.

**Regla crítica:** consumir las horas estimadas no termina automáticamente una actividad ni demuestra el mismo porcentaje de avance. Si se agota la estimación y sigue abierta, señalar que se requiere revisar la estimación; no presentarla como resuelta. El tratamiento exacto del riesgo mientras falta reestimar requiere concretarse.

## Dos formas de planeación

| Forma | Uso |
| --- | --- |
| Semanal | Empleado: elige actividades, asigna horas y ordena bloques en Mi semana |
| Por entregas | Usuario: controla iniciativas y proyectos por fechas meta y horas pendientes, sin calendario semanal obligatorio |

El empleado tiene una sola disponibilidad de 45 h semanales compartida entre proyectos, tareas sueltas y operación. La distribución diaria determina el dibujo de los bloques. Lunes a sábado con 7.5 h diarias es un ejemplo visual, no una jornada definitiva confirmada.

Para el usuario, una sola bolsa de horas semanales de referencia cubre TODO su trabajo. Decide cómo repartirla. No se calcula desde el ritmo histórico, porque su dedicación varía mucho, ni exige una jornada fija.

La forma de planear es independiente del rol y usa los mismos proyectos, actividades y registros.

## Mi semana: interfaz del empleado

### Pool de actividades disponibles

- Incluye actividades de proyectos, sueltas y generadas por plantillas.
- Tarjetas con título, proyecto si aplica, horas y puntos como indicador de arrastre.
- Arrastrar una tarjeta incorpora trabajo al plan semanal.
- Se colapsa a la izquierda dejando una pestaña pequeña con icono para restaurarlo a su tamaño original.
- Sin texto en la pestaña y sin botón flotante adicional.
- Los iconos de las pestañas no se repiten como iconos decorativos en las tarjetas.

### Timeline y orden

- Días y horas de jornada, barras proporcionales a horas planeadas y línea de ahora.
- Vacío muestra la cuadrícula sin barras.
- **Última decisión: reordenamiento vertical solamente.** Reemplaza la idea previa de arrastrar horizontalmente a cualquier día.
- El sistema acomoda los bloques consecutivamente desde el inicio de la semana laboral, según su orden, duración y jornada. El segundo comienza cuando termina el primero.
- Un bloque puede continuar al siguiente día laboral sin sumar la noche como trabajo.
- Reordenar cambia el plan, no inicia trabajo ni modifica el tiempo registrado.
- Una actividad puede tener varios bloques: por ejemplo, 5 h de una actividad de 15 h, otra actividad en medio y las 10 h restantes más abajo.

### Columna Actividad / Contexto

- Muestra título, proyecto y contexto de cada fila.
- Se colapsa independientemente del pool.
- Agregar dos flechas en la derecha del encabezado para colapsarla; al cerrarla queda una pestaña pequeña con icono para expandirla.
- Expandida: barras con horas solamente; títulos en la columna.
- Colapsada: barras con horas y el título que quepa, truncado con puntos suspensivos.
- Con ambos paneles colapsados, el calendario aprovecha prácticamente todo el ancho.

### Detalle inferior

Hacer clic en una fila o en su barra selecciona la misma actividad y abre su detalle abajo: título completo, proyecto, estado, estimación, horas registradas y pendientes; controles para iniciar, pausar, reanudar, registrar tiempo y completar según corresponda.

Es el detalle de la actividad seleccionada, no una lista de actividades terminadas.

## Registro de trabajo

- Iniciar guarda el inicio real de una sesión; pausar la cierra y reanudar abre otra.
- Permitir registrar y corregir tiempo manualmente para reducir fricción y reparar olvidos.
- Se propuso un solo contador activo por persona; iniciar otra actividad pausa el anterior.
- Marcar terminada es una acción separada.
- La espera de una impresión 3D no suma automáticamente horas humanas. Se puede trabajar en otra actividad mientras tanto.
- Empezar a las 11 en vez de las 9 no basta para declarar atraso: revisar su efecto sobre los compromisos y la disponibilidad restante.

## Panorama personal y alertas

Timeline general con iniciativas, sus proyectos, fechas meta, horas pendientes y línea de hoy. No inventar fechas de inicio obligatorias para actividades sin programar.

### Riesgo por capacidad compartida

Comparar, para cada fecha meta relevante, el trabajo pendiente que debe terminar para entonces con la capacidad de referencia disponible hasta esa fecha. Considerar el conjunto y las diferentes fechas; no conceder la bolsa completa a cada proyecto por separado ni contar dos veces las horas de una iniciativa y sus proyectos.

Ejemplo: 20 h pendientes, dos semanas restantes y referencia de 8 h por semana: caben 16 h, sobran 4 h de compromiso.

Mostrar una etiqueta amarilla En riesgo y una explicación breve. Cuando varios proyectos compiten por una bolsa insuficiente, señalar a TODOS los involucrados como En riesgo por carga compartida. No reservar horas a uno silenciosamente: no hay prioridades ni asignación automática en el MVP.

Ejemplo de render: batería necesita 6 h antes del 27 de octubre y PCB 30 h antes del 3 de noviembre. Ambos exigen 36 h frente a 24 h de referencia disponibles hasta el 3 de noviembre. Ambos muestran el conflicto compartido.

El usuario decide dedicar más horas, mover fechas o posponer proyectos. Las fechas meta no cambian automáticamente. Esta alerta usa una referencia declarada; no garantiza la terminación. No se diseñó un motor completo de ruta crítica y esperas.

### Riesgo semanal

Comparar trabajo pendiente con horas disponibles restantes y compromisos de la semana. Una actividad puede requerir atención sin comprometer automáticamente toda la semana si todavía hay margen.

Conservar plan frente a ejecución para que el desfase no desaparezca al recalcular. Se conversó sobre tolerancias configurables, pero no se cerró un valor ni una fórmula detallada. El ejemplo de una hora de tolerancia no es un requisito definitivo.

## Historial de semanas y continuaciones

- No mover pendientes automáticamente a la semana siguiente.
- Conservar en la semana anterior lo planeado, lo trabajado y lo pendiente.
- Para otra semana, crear manualmente una continuación vinculada a la actividad anterior.
- Poder consultar semanas anteriores para una retrospectiva mensual y ver compromisos que se extendieron. No se acordó un módulo separado de retrospectivas.

**Limitación pendiente y aplazada por el usuario:** se habló de evitar duplicar horas del proyecto entre continuaciones, pero se decidió no resolver todavía la contabilidad de estas ni su efecto en el riesgo. No presentar una solución definitiva como acordada. Documentar esta limitación al revisar el código.

## Plantillas de carreras: incluidas en el MVP

1. Elegir plantilla, por ejemplo antes, durante o después.
2. Capturar nombre de carrera y año.
3. Pulsar Crear actividades.
4. Generar las actividades en el pool con sus horas estimadas y el nombre de carrera y año incorporados al título.
5. El empleado las acomoda en su semana como cualquier otra actividad.

Ejemplo: Revisar loggers — Carrera X 2026.

Las actividades generadas son copias independientes. Editar una plantilla afecta futuras generaciones, no las carreras anteriores.

No se requieren fechas automáticas por reglas como siete días antes o dos días después. El botón genera cuando se solicita; no se acordó generación recurrente por calendario.

## Acceso y responsables

| Rol | Permisos acordados |
| --- | --- |
| Administrador | Ver y editar todo; crear cuentas, asignar roles y reasignar responsables |
| Usuario | Ver y gestionar lo suyo y lo asignado; crear sus proyectos y actividades |

Sin registro público: el administrador crea las cuentas. No hay un rol distinto para quien trabaja sin horario; eso es una configuración de planeación.

Se propuso un responsable por proyecto y por actividad, inicialmente quien los crea, con reasignación por el administrador. No se diseñaron múltiples responsables. Concretar la visibilidad del contexto cuando solo se asigna una actividad de un proyecto ajeno; no abrir otros proyectos privados por defecto.

El proveedor de identidad, inicio de sesión y recuperación de contraseña no se eligieron. La definición de roles no equivale a tener implementada la autenticación.

## Conceptos de datos para revisar

Orientación conceptual, sin imponer tablas, migraciones ni arquitectura nuevas:

| Concepto | Datos principales |
| --- | --- |
| Persona | Identidad, rol, forma de planear, bolsa semanal y jornada cuando aplique |
| Iniciativa | Nombre, fecha meta y proyectos |
| Proyecto | Nombre, iniciativa opcional, responsable y fecha meta |
| Actividad | Título, proyecto opcional, responsable, estimación actual, estado y dependencias |
| Plan semanal | Persona, semana e historial de compromisos y ejecución |
| Bloque | Referencia a actividad, posición y horas planeadas |
| Registro de trabajo | Persona, actividad y tiempo de sesión o captura manual |
| Plantilla y elementos | Títulos y horas de actividades a generar |
| Continuación | Vínculo al trabajo anterior; tratamiento de horas aún pendiente |

Las fechas de los bloques se calculan con orden, horas y jornada; los registros reales se conservan aparte. Varias partes del plan pueden referirse a una misma actividad.

## Fuera del MVP o sin detalle cerrado

- Prioridades alta/media/baja y recomendaciones automáticas.
- Botón Split junto a Reanudar y Terminar: dividir automáticamente un bloque y repartir horas. Solo nota para después; sin diseñar detalles ahora.
- Contabilidad definitiva entre continuaciones y su efecto en el riesgo.
- Motor avanzado de reprogramación, dependencias y esperas.
- Fechas relativas automáticas para plantillas de carreras.
- Tolerancias exactas de desfase y detalles de autenticación, por concretar durante el diseño técnico.

## Validación funcional y entrega incremental

Comprobar que una actividad sin fechas entra al pool, que el orden vertical acomoda sus horas, que los paneles funcionan de manera independiente y que una fila y su barra abren el mismo detalle. Verificar que registrar tiempo no completa automáticamente, que un conflicto compartido señala a todos los proyectos involucrados, que cambiar de semana conserva el historial y que las plantillas no alteran copias antiguas. Validar los permisos con las dos cuentas.

Revisar primero el código existente, identificar lo que ya funciona y lo que mezcla conceptos, y proponer una primera etapa pequeña. Usarla antes de ampliar el alcance.

## Renders de referencia

Los PNG de entrega ilustran el panorama personal y Mi semana con paneles expandidos y colapsados. Sus datos son ejemplos, no compromisos reales del usuario. Los estados, controles y reglas escritos en este documento son la referencia funcional; los renders no sustituyen las reglas de cálculo ni garantizan medidas exactas de las barras.
`nAcuerdo posterior (2026-10-20): jornada de referencia confirmada de 45 h: lunes a viernes 8 h, sábado 5 h, domingo 0 h. Reemplaza el ejemplo de 7.5 h por día.
