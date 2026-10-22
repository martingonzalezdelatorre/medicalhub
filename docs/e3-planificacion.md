# E3 — Cuadrantes, turnos y asignaciones

## Modelo: turno ≠ necesidad de cobertura ≠ asignación
- **Cuadrante**: planificación de un servicio para un periodo (≤ 93 días). Estados: `Borrador` → `Publicado` → `Archivado`.
- **Turno**: instancia de un tipo de turno en un día. **Puede existir sin ningún profesional.**
- **Necesidad de cobertura**: cuántos profesionales (y de qué categoría, opcional) necesita un turno. Un turno puede tener varias (p. ej. 1 adjunto + 1 residente).
- **Asignación**: un profesional concreto en una plaza de una necesidad. Las canceladas se conservan como historial.
- **Versión**: instantánea inmutable (JSON) que se guarda cada vez que se publica.

## Horarios, noches y cambios de hora
Los tipos de turno se definen en hora local del centro. Cada turno guarda su inicio y fin en **UTC**, calculados con la zona IANA del centro. El fin se resuelve sobre el reloj local, no sumando una duración fija, de modo que:

| Caso (Europe/Madrid) | Resultado |
|---|---|
| Noche 22:00–08:00 normal | 10 h |
| Noche del 24→25/10/2026 (fin del horario de verano) | **11 h** |
| Noche del 28→29/03/2026 (inicio del horario de verano) | **9 h** |
| Guardia «24 h» que incluye el retroceso / el adelanto | 25 h / 23 h |
| Hora inexistente (02:30 del 29/03) | se desplaza a 03:30 |
| Hora repetida (02:30 del 25/10) | primera ocurrencia |

El turno pertenece al día en que **empieza**; el calendario muestra «(+1)» si termina al día siguiente. Los solapamientos se calculan sobre UTC (turnos contiguos, como una noche que termina a las 08:00 y una mañana que empieza a las 08:00, no se solapan).

## Asignación transaccional y concurrencia
`AsignacionesServicio.AsignarAsync` se ejecuta en una transacción y toma **bloqueos de aplicación** (`sp_getapplock`) por necesidad y por profesional, en orden fijo (sin interbloqueos). Dentro de ese bloqueo comprueba:
1. elegibilidad de dominio (activo, servicio activo, autorizado, categoría admitida, competencias vigentes);
2. categoría exigida por la necesidad;
3. que no esté ya asignado y que queden plazas libres;
4. que no se solape con ninguna otra asignación activa suya (en cualquier servicio o centro).

Resultado: 10 aceptaciones simultáneas de una plaza única asignan a **una** persona (el resto recibe 409), y un mismo profesional pedido a la vez en dos turnos solapados solo acepta uno (hay pruebas que lo verifican, repetidas varias veces). Un índice único filtrado `(necesidad, profesional)` actúa como respaldo. Los códigos: 422 = regla de negocio incumplida, 409 = estado o concurrencia, 404 = no existe en su sociedad.

## Publicación, validación y versiones
`GET /api/cuadrantes/{id}/validacion` devuelve incidencias con severidad:
- **Bloqueantes** (impiden publicar): cuadrante vacío, asignación que ya no es elegible (profesional desactivado, habilitación caducada…), solapes.
- **Advertencias** (revisables): plazas sin cubrir, turnos sin necesidades.

Al publicar se incrementa la versión y se guarda la instantánea. Un cuadrante publicado **no se edita**: hay que **reabrirlo** (vuelve a borrador; la versión publicada anterior se conserva) y volver a publicar (versión +1). Los borradores que nunca se publicaron se pueden eliminar; los publicados se **archivan**. El motor de reglas configurable de E4 ampliará estas comprobaciones y añadirá las excepciones justificadas.

## Calendario y vistas
`GET /api/turnos?desde&hasta[&centroId&servicioId&profesionalId&categoriaId&cuadranteId&soloSinCubrir]` (máx. 100 días). La app ofrece **día, semana, mes y agenda**; en pantallas estrechas la agenda es la vista por defecto y la cuadrícula semanal se degrada a lista vertical. Color de cobertura: verde completo, naranja parcial, rojo sin cubrir.

## Quién ve qué
| | Admin. sociedad | Coordinador | Resp. centro | Profesional |
|---|---|---|---|---|
| Crear/editar/publicar/asignar/exportar | ✔ | ✔ | — | — |
| Ver calendario, cuadrantes y versiones | ✔ | ✔ | solo sus centros | — |
| Mi planificación (`/api/mi-planificacion`) | — | — | — | solo sus turnos de cuadrantes **publicados**, sin ver a los compañeros |

## Plantillas y exportación
- **Plantilla**: se guarda la estructura (turnos y necesidades por día relativo, **sin profesionales**) y se crea un cuadrante nuevo desde cualquier fecha.
- **CSV** (`/api/cuadrantes/{id}/exportar.csv`): UTF-8 con BOM y separador `;` (Excel en español). Las celdas que empiezan por `= + - @` se neutralizan (inyección de fórmulas), solo lo exportan administrador y coordinador y **cada exportación queda registrada** como evento de seguridad. En web se descarga el archivo; en otras plataformas el CSV se copia al portapapeles (pendiente de una descarga nativa).

## Datos de demostración
La herramienta `MedicalHub.DatosDemo` crea ahora además la semana siguiente: **Reanimación publicada** (noches de lunes a viernes cubiertas, fin de semana sin cubrir) y **Quirófano en borrador** con una vacante cada mañana, tardes completas y fines de semana vacíos.

## No incluido (según el plan)
Motor de reglas configurable, descansos y límites de horas, excepciones justificadas (E4); disponibilidad y ausencias (E5); sustituciones y notificaciones (E6/K); horas planificadas vs. realizadas (E7). Tampoco hay arrastrar y soltar en el calendario ni una pantalla de comparación entre versiones (se consulta cada instantánea por API).
