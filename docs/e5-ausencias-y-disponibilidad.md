# E5 — Ausencias, disponibilidad y vacantes

## Qué incluye
- **Ausencias** (vacaciones, asuntos propios, permiso, formación, baja temporal, otro): el profesional las solicita; la coordinación las aprueba, rechaza o cancela, o las registra directamente (opcionalmente ya aprobadas). Historial inmutable de decisiones. Comentario ≤ 300 caracteres; **no deben incluirse datos de salud ni diagnósticos** (la interfaz lo recuerda).
- **Disponibilidad** (disponible, bloqueo, prefiere, evita): declarada por el profesional con rango de fechas, días de la semana (máscara lunes=1 … domingo=64) y tipo de turno opcional. Solo cuenta cuando la coordinación la aprueba.
- **Vacantes**: al aprobar una ausencia se cancelan las asignaciones futuras solapadas y cada una genera una vacante abierta. Se cubren desde la pestaña *Vacantes*, incluso en cuadrantes publicados, con la lista de candidatos del motor de reglas.
- **Reglas nuevas** (E4): `AusenciaAprobada` (bloqueante, con excepción posible), `DisponibilidadBloqueada` (bloqueante), `PreferenciaEvitar` (advertencia; configurable). Los candidatos muestran notas positivas («Prefiere este turno», «Ha declarado disponibilidad…»).

## API
| Recurso | Endpoints |
|---|---|
| Propias | `GET/POST api/mis-ausencias`, `POST api/mis-ausencias/{id}/cancelar`, `GET/POST api/mi-disponibilidad`, `DELETE api/mi-disponibilidad/{id}`, `GET api/mis-tipos-turno` |
| Gestión | `GET/POST api/ausencias`, `GET api/ausencias/{id}`, `GET …/comprometidos`, `POST …/aprobar|rechazar|cancelar` |
| Disponibilidad | `GET/POST api/disponibilidades`, `POST …/{id}/aprobar|rechazar`, `DELETE …/{id}` |
| Vacantes | `GET api/vacantes`, `POST api/vacantes/{id}/cubrir` |

Gestión: administrador de sociedad y coordinador médico. Aislamiento por organización en todas las consultas; ids de otra organización → 404.

## Interfaz (Flutter)
- *Mi disponibilidad* (profesional): mis ausencias, mis declaraciones, solicitar/cancelar/retirar.
- *Ausencias* (administrador/coordinador): pestañas Solicitudes, Vacantes y Disponibilidad pendiente.

## Pruebas
Backend: dominio 77, aplicación 35, integración 145 (ausencias, disponibilidad, concurrencia de aprobaciones, aislamiento, permisos, privacidad). Flutter: 63 tests.

## Limitaciones conocidas
- Las notificaciones y el flujo de oferta → aceptación → aprobación de sustitutos se añadieron en [E6](e6-sustituciones-y-notificaciones.md).
- La descarga CSV solo funciona en web; no se han verificado compilaciones móviles/escritorio nativas.
- Las pantallas de E5 están probadas con tests de widgets y API falsa; no se ha repetido la verificación manual en navegador con datos de demostración.
