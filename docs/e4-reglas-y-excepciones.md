# E4 — Motor de reglas configurable y excepciones

## Principio: la sociedad define sus límites
El sistema **no incluye límites legales por defecto**. Descansos, máximos de horas o de guardias dependen del colectivo y de su relación contractual (convenio, estatuto, acuerdos), así que cada sociedad los configura y los **revisa**. Las reglas de demostración llevan «(ejemplo demo)» y un aviso en el fundamento.

## Qué evalúa el motor
**Intrínsecas** (no se configuran ni admiten excepción): profesional inactivo, servicio inactivo y **solapamiento** de turnos del mismo profesional (en cualquier servicio o centro). Son imposibilidades, no preferencias.

**De elegibilidad** (por defecto: *bloqueantes con excepción posible*; la sociedad puede rebajarlas a advertencia, endurecerlas o desactivarlas): autorización para el servicio, categoría admitida en el servicio, competencias requeridas vigentes, categoría exigida por la necesidad.

**Límites configurables** (no se aplican hasta que la sociedad los crea): descanso mínimo entre turnos, máximo de horas en N días, máximo de guardias en N días, máximo de días consecutivos, incompatibilidad entre dos servicios (margen de horas).

**Del cuadrante** (por defecto: advertencias): cobertura mínima (% de plazas cubiertas por necesidad) y asignaciones pendientes de aprobación.

Cada regla puede aplicarse a toda la sociedad o a un servicio. Si la sociedad crea alguna regla de un tipo con comportamiento por defecto, **solo cuentan las suyas activas** (así puede anular una regla base). Los límites usan ventanas móviles de días y los turnos cuentan en el día en que empiezan.

## Severidad, excepciones y autorización
| Configuración de la regla | Efecto al asignar |
|---|---|
| Advertencia | Se asigna; se informa de la advertencia y reaparece al validar el cuadrante |
| Bloqueante sin excepción | 422 `regla_incumplida`; no se puede asignar ni con justificación |
| Bloqueante con excepción | 422 `requiere_excepcion` con el detalle; se asigna si se envía una **justificación (≥ 10 caracteres)** |
| …y «solo administrador» | Si el usuario es coordinador: 403 `requiere_administrador` |

Cada excepción queda registrada de forma inmutable: asignación, profesional, regla incumplida, mensaje de la infracción, **justificación**, **quién la autorizó** y cuándo (`GET /api/excepciones-reglas`, con filtros). Si la asignación se cancela, la excepción se conserva como historial («asignación anulada»). También se puede **autorizar a posteriori** (`POST /api/asignaciones/{id}/excepciones`), por ejemplo cuando la sociedad endurece una regla con el cuadrante ya elaborado.

## Validación y publicación
Las infracciones entre pares (descanso, ventanas…) se atribuyen a la **asignación más reciente** del par: es la que se hizo teniendo en cuenta a la otra y la que lleva la excepción si la hubo; así cada infracción se cuenta una sola vez y la validación coincide con lo evaluado al asignar. Una infracción bloqueante con excepción autorizada **no impide publicar** y se muestra con su justificación y autor; sin excepción, bloquea.

## Revisión por la sociedad
Cada regla guarda su **fundamento** (texto libre: convenio, acuerdo o norma), quién la revisó y cuándo. Cualquier modificación la devuelve a «pendiente de revisión». Cambios y altas de reglas y excepciones quedan en la auditoría.

## Permisos
Configurar reglas: administrador de sociedad. Consultar reglas y excepciones, y autorizar excepciones: administrador y coordinador (salvo reglas marcadas «solo administrador»). Responsable de centro y profesional no acceden.

## Decisiones y límites
- Las reglas se almacenan con parámetros JSON validados por tipo; añadir un tipo nuevo es añadir una evaluación al motor (lógica pura, cubierta por 25 pruebas unitarias).
- Las excepciones no se pueden editar ni borrar.
- Disponibilidad declarada y ausencias aprobadas (reglas «fuera de disponibilidad» y «ausencia») se añadirán en E5, cuando existan esos datos.
- Pendiente de decidir con la sociedad: si una excepción debe poder tener caducidad o doble autorización (cuatro ojos).
