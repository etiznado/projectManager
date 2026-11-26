# API privada de planificación de Jarvis

Extensión de la misma aplicación y autenticación Bearer HTTPS de [Frentes abiertos](jarvis-open-fronts-api.md), aprobada en [FEAT-019, issue #23](https://github.com/etiznado/projectManager/issues/23). Agrega iniciativas, proyectos y actividades propios. No cambia `/api/jarvis/v1/fronts`, sus permisos ni su contrato.

## Estado y requisito de esquema

Implementación y pruebas HTTP con SQLite sintético en memoria; no se ha activado esta extensión ni se ha conectado producción. El modelo incorpora exclusivamente las tablas auxiliares `JarvisPlanningStates` y `JarvisPlanningChanges`; las entidades de planificación existentes siguen siendo la fuente de sus campos originales. **La migración y el snapshot no se han creado ni aplicado: requieren autorización separada.** No habilitar `Planning` en una instancia SQL Server antes de preparar, revisar y aplicar ese esquema en un destino expresamente autorizado y verificarlo. No hay migración, atribución masiva de propietarios ni semilla automática.

Las pruebas aisladas no acreditan integración ni concurrencia de SQL Server. No se ha cambiado el perfil operativo privado, credencial, certificado ni proceso local. La extensión permanece cerrada cuando se omite su configuración; las rutas anteriores de Frentes abiertos siguen usando su configuración previa.

## Autorización del operador

La autenticación usa exactamente el conector existente `Authentication:Jarvis`; no se emite otra credencial ni se crea una API de aprobaciones. La sección adicional `Authentication:Jarvis:Planning` se configura fuera del repositorio por un operador después de obtener la aprobación real de Edgar. Se carga una vez al resolver el servicio; cambiarla o revocarla requiere reiniciar la instancia bajo su autorización operativa. La API no puede crear esos permisos ni aceptar una declaración de aprobación del cliente.

Ejemplo **ilustrativo y sintético**, para combinar únicamente con una configuración privada válida del conector:

```json
{
  "Authentication": {
    "Jarvis": {
      "Planning": {
        "Enabled": true,
        "OwnedInitiativeIds": [101],
        "Plans": [
          {
            "Id": "plan-example",
            "ApprovalReference": "Referencia privada de la aprobación de Edgar",
            "InitiativeIds": [101],
            "ProjectIds": [],
            "ActivityIds": [],
            "AllowCreateInitiatives": true,
            "AllowCreateProjects": true,
            "AllowCreateActivities": true,
            "AllowIndependentProjects": true,
            "AllowLooseActivities": true
          }
        ],
        "Approvals": []
      }
    }
  }
}
```

- `Enabled` omitido/false cierra todas las rutas de esta extensión, sin consultar sus tablas auxiliares. Una configuración habilitada inválida no concede acceso.
- `OwnedInitiativeIds` atribuye explícitamente iniciativas existentes al propietario activo configurado del conector. Nunca se deduce esa atribución por tener un proyecto propio. Iniciativas con proyectos o actividades de otros responsables permanecen fuera de acceso, incluso si se incluyen aquí. La atribución de iniciativas existentes se revalida contra esta lista; las creadas legítimamente por esta extensión conservan su propietario en los metadatos.
- Cada plan exige `Id` y `ApprovalReference` no vacíos. Los arreglos de IDs contienen positivos únicos. Una raíz autorizada incluye sus descendientes propios; los elementos creados quedan ligados persistentemente a ese plan. Tener permiso de lectura de un elemento propio no implica permiso de escritura: la escritura exige pertenecer al plan seleccionado.
- Cada permiso de alta es explícito por tipo. Además, proyectos independientes y actividades sueltas requieren sus flags específicos. Un plan puede autorizar raíces existentes sin permitir altas. En una reasociación deben estar autorizados origen, destino y contexto pertinente; no se crea una copia del trabajo.
- El servidor fija el propietario. No se aceptan `personId`, grants, `approved`, `approvedBy`, `planApproved` ni campos fuera del contrato en una solicitud.

La lectura muestra solo trabajo propio. Un proyecto propio puede mostrarse con padre nulo si la iniciativa no es accesible; esto no concede permiso de modificar ese contexto. Se rechazan escrituras que afecten agregados compartidos, un contexto ajeno o trabajo fuera del plan. Las dependencias de salida solo incluyen IDs accesibles. El historial de una entidad reasignada deja de estar disponible; también se omiten eventos cuyas instantáneas revelarían relaciones que ahora son ajenas. Los eventos almacenados se conservan.

## Rutas y contratos

Prefijo común `/api/jarvis/v1`. Se rechazan propiedades JSON adicionales. Los campos indicados como precondiciones son obligatorios para toda actualización:

```json
{
  "planId": "plan-example",
  "expectedRevision": 0,
  "expectedFingerprint": "SHA256 hexadecimal de 64 caracteres recibido en la lectura"
}
```

| Método y ruta | Cuerpo además de las precondiciones | Resultado |
| --- | --- | --- |
| `GET /initiatives`, `/projects`, `/activities` | Ninguno | 200, colección propia por ID |
| `GET /{collection}/{id}` | Ninguno | 200, detalle propio |
| `GET /{collection}/{id}/history` | Ninguno | 200, eventos propios accesibles por revisión |
| `POST /initiatives` | Sin precondiciones; `planId`, `name`, `description?`, `targetDate?` | 201, iniciativa nueva |
| `POST /projects` | Sin precondiciones; `planId`, `name`, `initiativeId?`, `targetDate?` | 201, proyecto nuevo |
| `POST /activities` | Sin precondiciones; `planId`, `title`, `projectId?`, `initialEstimateHours` | 201, actividad nueva Pending |
| `PUT /initiatives/{id}` | `name`, `description?` | 200, nombre/descripción guardados |
| `PUT /projects/{id}` | `name`, `initiativeId?` | 200, campos ordinarios guardados |
| `PUT /activities/{id}` | `title`, `projectId?` | 200, campos ordinarios guardados |
| `POST /{collection}/{id}/advances` | `text` | 200, avance auditado |
| `PUT /activities/{id}/status` | `status`: 0 Pending, 1 InProgress, 2 Completed | 200, estado guardado |
| `PUT /initiatives/{id}/target-date`, `/projects/{id}/target-date` | `targetDate` explícito —fecha ISO o null— y `approvalId` | 200, fecha aprobada |
| `PUT /activities/{id}/dependencies` | `predecessorIds`, `approvalId?` | 200, predecesoras guardadas |
| `POST /initiatives/{id}/close`, `/projects/{id}/close` | `approvalId` | 200, metadato de cierre aprobado |

`collection` es `initiatives`, `projects` o `activities`. No hay DELETE ni operación de cambio de horas o propietario. Las relaciones padre omitidas en edición se conservan; null explícito desvincula si el plan permite trabajo independiente. En edición de iniciativa, `description` omitida/null limpia la descripción. Nombre/título admiten hasta 200 caracteres no blancos; descripción y avance hasta 2000, con avance no blanco. Las horas iniciales están entre 0 y 99 999 999,99, con máximo dos decimales. Solo se capturan al crear: ninguna actualización puede editar `CurrentEstimateHours` ni horas de bloques existentes.

Un avance registra texto, conserva el estado y no añade horas trabajadas. Completar una actividad cierra sus sesiones reales abiertas según el instante UTC del servidor y conserva la estimación y el plan semanal. Cerrar una iniciativa o proyecto no completa descendientes; es metadato explícito de esta API. No se incorpora una indicación visual nueva en Blazor. Las dependencias usan actividades existentes, pueden cruzar proyectos propios autorizados, rechazan ciclos y autorreferencias y no mueven fechas ni duplican actividades. Los IDs repetidos se normalizan como un conjunto.

Cada resultado contiene `kind`, `id`, `name`, `description`, `parentId`, `targetDate`, `currentEstimateHours`, `status`, `isClosed`, `lastAdvance`, `revision`, `fingerprint` y `predecessorIds`; los campos no aplicables son null o un arreglo vacío. Las revisiones empiezan en 0 para registros anteriores sin metadatos y en 1 al crear. Cada actualización aceptada incrementa una revisión, incluso una repetición sin cambio efectivo. Consultar de nuevo después de cualquier modificación antes de componer la próxima solicitud.

## Aprobaciones sensibles concretas

Cambiar una fecha ya capturada, modificar efectivamente dependencias o cerrar iniciativa/proyecto requiere una aprobación específica. Se consideran sensibles todos los cambios efectivos de dependencias. Repetir el mismo conjunto no consume otra aprobación; repetir una fecha/cierre ya aplicada tampoco vuelve a autorizar un cambio efectivo. Una fecha inicial puede formar parte del alta permitida por el plan.

El operador añade a `Approvals` un registro con estos campos:

| Campo | Vinculación |
| --- | --- |
| `Id`, `ApprovalReference` | Identificador estable único y referencia de la decisión real de Edgar |
| `PlanId`, `PersonId` | Plan aprobado y propietario configurado |
| `EntityKind`, `EntityId` | `initiative`, `project` o `activity` y su ID concreto |
| `Operation` | `target-date`, `dependencies` o `close` |
| `ExpectedRevision`, `ExpectedFingerprint` | Estado exacto obtenido antes de aprobar |
| `ValuesSha256` | SHA-256 del JSON normalizado de los valores aprobados |

Los valores normalizados son exactamente `{"targetDate":"2027-03-05"}` o `{"targetDate":null}`, `{"predecessorIds":[1,5]}` con IDs únicos ordenados, o `{"isClosed":true}`. El hash usa JSON sin formato y UTF-8; `JarvisPlanningAccess.ValuesHash` permite calcularlo en código del operador y normaliza los decimales con `G29` y cultura invariante. Una diferencia de escala como `8` frente a `8.00` no cambia el fingerprint al materializar datos de otro proveedor. No se calcula sobre el cuerpo completo ni sobre las precondiciones. El ID enviado por Jarvis solo selecciona un registro del servidor y nunca prueba por sí mismo la aprobación.

La API comprueba entidad, operación, propietario, plan, revisión, fingerprint y hash de valores. Una aprobación consumida no puede reutilizarse para otro cambio: su ID queda asociado a un único evento, con índice único. El consumo forma parte de la misma transacción. No registrar secretos ni credenciales en referencias de aprobación.

## Consistencia, historial y errores

Las lecturas/escrituras de esta extensión usan una transacción serializable que lee propiedad, jerarquía, grafo de dependencias, sesiones y metadatos antes de comprobar permisos y precondiciones. La implementación actual carga ese grafo completo; prioriza consistencia para el uso privado, sin reclamar escalabilidad verificada. Se conserva el bloqueo de esas lecturas hasta confirmar, y la revisión auxiliar tiene control de concurrencia optimista adicional. En SQL Server un deadlock puede hacer fallar una solicitud: no se informa éxito parcial. La concurrencia real de ese proveedor está pendiente de la validación autorizada.

El fingerprint SHA-256 canónico incluye los campos vigentes, relaciones, dependencias ordenadas, metadatos y sesiones abiertas del elemento, además de un hash del contexto de ancestros y descendientes. Una edición interactiva relevante invalida la precondición aunque no haya incrementado la revisión auxiliar. La API nunca serializa navegaciones EF en sus DTOs.

Entidad, metadatos, evento y consumo de aprobación se guardan juntos. Cada evento conserva tipo/ID, propietario, plan y su referencia de aprobación, operación, conector, UTC, revisión y valores anterior/nuevo completos. Las instantáneas no tienen el límite genérico de 200 caracteres ni se truncan. Una validación, conflicto o fallo revierte todas las partes, también un alta que ya había obtenido ID. Tras fallo, recargar estado antes de decidir un reintento. POST de creación no ofrece idempotencia de transporte: una creación confirmada repetida crea otro registro; usar los IDs devueltos para cambios posteriores.

| HTTP | Significado |
| --- | --- |
| 400 | Campo desconocido, datos inválidos, horas iniciales inválidas, autorreferencia o ciclo |
| 401 | Autenticación privada ausente/inválida, cookie sin Bearer, API del conector deshabilitada o transporte no HTTPS |
| 403 | Extensión cerrada, propietario inactivo, plan o aprobación no autorizados |
| 404 | Elemento inexistente/no disponible/ajeno, con respuesta indistinguible |
| 405 | Método no expuesto, incluido DELETE |
| 409 | Revisión o fingerprint obsoletos, o conflicto de concurrencia |
| 503 | Fallo de persistencia/lectura sin detalles internos |

Las respuestas llevan `Cache-Control: no-store`. No se habilitan CORS, rutas de emisión de permisos, nuevas cookies ni acceso en producción. Los resultados de Developer, Reviewer y QA se conservan en Codex; el issue recibe los criterios, decisiones necesarias y un único resumen final.
