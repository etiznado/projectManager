# API privada de Frentes abiertos

Contrato para un conector de Jarvis. Se ejecuta dentro de Project Manager y permanece deshabilitada hasta que un operador configure la identidad. Permite consultar, crear y actualizar frentes autorizados; no permite eliminarlos, administrar usuarios/permisos, cambiar código ni acceder directamente a SQL.

## Requisito de esquema y activación

El modelo agrega `OpenFrontChanges`, el historial de cambios. La migración `20261126001849_JarvisOpenFrontHistory` y su snapshot están preparados. Edgar autorizó después una base local nueva y sintética de QA; allí se verificaron la actualización con datos previos, la API HTTPS, el historial y la pantalla. Evidencia y límites en [QA del issue #16](https://github.com/etiznado/projectManager/issues/16#issuecomment-7179486840). **Esta prueba no autoriza aplicar migraciones ni usar la base habitual del dueño.** No arrancar esta versión contra otra base existente antes de revisar/aplicar el esquema al destino expresamente autorizado: también los cambios de estado desde la pantalla guardan historial. No hay migración automática ni carga de datos de ejemplo al arrancar.

La publicación, conexión de Jarvis y configuración de credenciales reales se autorizan y realizan por separado. No se abre un endpoint de emisión de credenciales ni una pantalla de permisos.

## Configuración privada del operador

Claves bajo `Authentication:Jarvis` (variables de entorno usan `Authentication__Jarvis__...`):

| Clave | Valor |
| --- | --- |
| `Enabled` | `false` por defecto; habilitar solamente tras preparar el esquema y transporte privado HTTPS |
| `ConnectorId` | Identificador estable de 1–200 caracteres ASCII: letras, dígitos, punto, guion o guion bajo |
| `TokenSha256` | SHA-256 hexadecimal de 64 caracteres del token UTF-8; el servidor no necesita el token original |
| `PersonId` | ID positivo del propietario activo autorizado |
| `AllowedFrontIds` | Arreglo explícito de IDs positivos únicos; concede lectura y actualización solo de esos frentes que sigan siendo de ese propietario |
| `AllowAllOwnerFronts` | Booleano opcional, `false` por defecto; `true` permite consultar y actualizar todas las tarjetas actuales y futuras del propietario configurado sin mantener una lista de IDs |

Con `AllowAllOwnerFronts` omitido o `false`, la lista incluye los IDs explícitos y los frentes que este mismo conector creó legítimamente para su propietario configurado, siempre que sigan siendo de ese propietario. La procedencia se verifica en el historial persistido: evento `created`, actor de conector/propietario coincidentes, revisión inicial 1 y valor anterior vacío. El identificador de actor distingue mayúsculas y minúsculas, incluso en bases con collation predeterminada insensible a ellas. En ese modo restringido no se conceden otros frentes del propietario ni frentes por un historial de actualización ordinaria. Sin IDs explícitos puede crear y gestionar sus propias tarjetas nuevas, sin ver otras preexistentes; las listas de grants no se reescriben. Esta autorización derivada sobrevive al reinicio y no desaparece por quitar un ID de la lista explícita.

Con `AllowAllOwnerFronts: true`, Jarvis accede a todas las tarjetas que actualmente pertenezcan a su propietario activo configurado, incluso si existían antes o se crean después por otra vía. No necesita enumerar sus IDs ni acreditar su propia creación. El modo es una decisión del operador; los cuerpos de la API no pueden activarlo. Conserva autenticación, HTTPS, propietario, validaciones, revisiones e historial atómico. Tarjetas ajenas o reasignadas a otro propietario siguen excluidas. La lista explícita se conserva y vuelve a regir al desactivar el modo junto con la procedencia de creación.

Una configuración habilitada inválida impide el arranque. La configuración se toma una vez: revocar o rotar credencial/configuración requiere cambiar la configuración privada y reiniciar la aplicación. Mantener la configuración y el token fuera del repositorio y de los logs; no habilitar registro de cabeceras Authorization ni cuerpos de esta API.

Generar el token con un generador criptográfico usando **al menos 32 bytes aleatorios**, codificados en base64 o base64url. Conservarlo únicamente en el almacén privado del conector; calcular SHA-256 sobre los caracteres exactos del token para configurar el servidor. Se aceptan tokens de 43–512 caracteres base64/base64url. No usar contraseñas memorizables. La comparación de digests usa tiempo constante.

El conector envía una sola cabecera `Authorization: Bearer <token>` por HTTPS. No se admiten HTTP, credenciales en query string, cookies del navegador ni cabeceras múltiples. No se configuran CORS, proxies de confianza o exposición pública nuevos; un proxy que termina TLS necesita una configuración de transporte revisada por separado. La identidad del conector no concede sesión interactiva de usuario.

## Operaciones

Prefijo: `/api/jarvis/v1/fronts`.

| Método y ruta relativa | JSON de entrada | Respuesta correcta |
| --- | --- | --- |
| `GET` colección | Sin cuerpo | `200`, arreglo ordenado por ID |
| `POST` colección | `{"title":"Preparar prueba"}`; opcionales abajo | `201`, nueva ficha guardada, ID generado y revisión 1 |
| `PUT /{frontId}/status` | `{"expectedRevision":0,"status":2}` | `200`, ficha guardada |
| `POST /{frontId}/advances` | `{"expectedRevision":0,"text":"Prueba terminada"}` | `200`, ficha guardada |
| `PUT /{frontId}/next-step` | `{"expectedRevision":0,"text":"Revisar resultado"}` | `200`, ficha guardada |

Estados numéricos: `0` En curso, `1` Necesita decisión, `2` En espera. Cada actualización cambia un solo campo y conserva los demás. Un avance reemplaza el último avance visible; el historial conserva todos los valores anteriores. El próximo paso puede vaciarse con `""`. Registrar un avance posterior requiere texto no blanco; ambos textos admiten hasta 2000 caracteres y se conservan sin truncamiento. Todos los campos indicados para las actualizaciones son obligatorios; las propiedades JSON adicionales se rechazan.

### Crear una tarjeta

Enviar `POST /api/jarvis/v1/fronts` con la misma autenticación ya configurada. Ejemplo completo:

```json
{
  "title": "Preparar prueba",
  "context": "Validación",
  "status": 2,
  "lastAdvance": "Pieza recibida",
  "nextStep": "Montar"
}
```

Solo `title` es obligatorio: texto no blanco de hasta 200 caracteres. `context` admite null o hasta 200 caracteres; omitido queda null. `status` omitido es 0; `lastAdvance` y `nextStep` omitidos quedan vacíos y admiten hasta 2000 caracteres. Null explícito en título/estado/avance/próximo paso es inválido. Se conserva el texto aceptado, sin truncar.

El servidor fija propietario, ID, actor, UTC y revisión inicial 1; no enviar esos campos ni grants. No se requiere `expectedRevision` al crear. Cada POST aceptado crea una tarjeta nueva; las actualizaciones posteriores usan su ID y revisión devueltos. La respuesta 201 contiene el mismo DTO siguiente. Tarjeta y un evento de creación se guardan en una transacción; un fallo no deja tarjeta parcial ni evento huérfano. El historial `created` registra el título como procedencia de creación, no una copia completa de todos los campos iniciales. La página ya abierta incorpora el nuevo ID en su siguiente lectura de 30 segundos.

Ficha de salida:

```json
{
  "id": 1,
  "title": "Frente identificado",
  "context": null,
  "status": 2,
  "lastAdvance": "Prueba terminada",
  "nextStep": "Revisar resultado",
  "updatedAtUtc": "2026-11-25T00:00:00+00:00",
  "revision": 1
}
```

Los IDs/valores del ejemplo son ilustrativos. Jarvis debe consultar la lista, identificar el frente y enviar su revisión actual. Cada comando aceptado incrementa la revisión una vez, incluso si repite el valor. Un reintento con revisión vieja devuelve conflicto; consultar de nuevo antes de decidir el siguiente cambio. Frentes abiertos actualiza sus fichas con una lectura cada 30 segundos mientras está abierto, sin recargar el navegador. Si hay una operación local en curso se omite ese turno; un fallo conserva las fichas válidas y se reintenta después. El [perfil local de prueba](local-test.md) permite usar la aplicación real con la configuración privada ya aprobada.

## Errores e historial

Errores JSON con propiedad `error`, sin redirección al login ni detalles de SQL:

| HTTP | Significado |
| --- | --- |
| `400` | Entrada no válida, revisión omitida/negativa, estado fuera del catálogo, texto inválido, propiedad JSON desconocida |
| `401` | Credencial ausente/no válida, API deshabilitada o transporte sin HTTPS |
| `403` | El propietario configurado ya no está activo/disponible |
| `404` | Frente inexistente, no concedido o de otro propietario; respuesta idéntica |
| `409` | Revisión obsoleta o carrera de actualización; requiere recarga |
| `503` | No se pudo guardar/leer; no se informa éxito ni se exponen detalles internos |

Cada cambio exitoso conserva frente, propietario, clase/ID de actor (`connector` o `person`), campo, valor anterior/nuevo, fecha UTC del servidor y revisión resultante. El cambio del frente y el evento se guardan juntos en una transacción. Un rechazo o una operación fallida no agrega un evento exitoso ni aplica una parte del cambio. No se almacenan credenciales en el historial. Esta API no añade visor ni endpoint de historial.

Las pruebas automatizadas usan datos sintéticos en memoria; la integración adicional de SQL Server y pantalla se verificó en una base local nueva de QA. La base habitual del dueño y la conexión externa real de Jarvis siguen sujetas a sus autorizaciones; pasar estas pruebas no activa el conector.
