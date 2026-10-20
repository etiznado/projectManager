# Guía para trabajar en RaceIT.ProjectManager

## Contexto persistente

Antes de trabajar, leer memory-bank/README.md y seguir su orden de lectura, en especial activeContext.md y progress.md. Consultar código y migraciones para comprobar el estado real. La solicitud actual del usuario prevalece sobre notas históricas.

Al completar cambios relevantes, actualizar activeContext.md, progress.md y los documentos técnicos o de producto afectados. Distinguir lo implementado de lo propuesto, y lo probado de lo pendiente. No guardar secretos ni credenciales en el memory bank.

## Acuerdos esenciales

- Compañía RaceIT; namespace raíz RaceIT.ProjectManager.
- .NET 10, Blazor Interactive Server, EF Core y SQL Server. Desarrollo local primero.
- Distinguir trabajo en horas laborables, espera en días naturales de 24 horas y fechas manuales para proyectos flexibles sin horas; preservar el tipo del plan y del pronóstico por separado.
- Conservar el plan original; el usuario decidió mantener horas planeadas sin edición por ahora.
- Edición de actividades en modal; conservar iniciativa/proyecto seleccionados y refrescar lista al cerrar.
- No iniciar trabajo pendiente solo porque aparezca en el memory bank; respetar el alcance de la solicitud.
- No sobrescribir datos capturados por el usuario con datos de ejemplo.

Comandos y particularidades del entorno en memory-bank/techContext.md.


