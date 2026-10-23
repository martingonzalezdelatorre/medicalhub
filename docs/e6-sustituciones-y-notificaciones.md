# E6 — Sustituciones y notificaciones

Cierra el circuito del escenario piloto: **ausencia aprobada → vacante → oferta a candidatos → el profesional acepta → el coordinador aprueba → cuadrante actualizado**, con aviso a cada parte e historial.

## Ofertas de sustitución
Entidad `OfertaSustitucion` (por vacante y profesional). Estados:

| Estado | Significado |
|---|---|
| Pendiente | Enviada; el profesional aún no ha respondido (o caduca si se fijó un plazo). |
| Aceptada | El profesional acepta; espera la aprobación de la coordinación. **No crea asignación.** |
| Rechazada | El profesional rechaza; la vacante sigue abierta. |
| Aprobada | La coordinación confirma: se crea la asignación y la vacante pasa a *Cubierta*. |
| Denegada | La coordinación descarta una oferta aceptada (motivo obligatorio). |
| Retirada | La coordinación la anula (pendiente o aceptada). |
| Caducada | Venció el plazo sin respuesta (estado derivado: se calcula al leer y se persiste al intentar aceptar). |
| Cerrada | La vacante se cubrió por otra vía (otra oferta aprobada o cobertura directa). |

Reglas de negocio:
- Solo se ofrece a candidatos que podrían ser asignados: sin infracciones, o solo con infracciones que admiten excepción justificada (se usa el mismo cálculo de candidatos que la asignación). El profesional debe estar activo y tener usuario vinculado para poder responder. Máximo 10 destinatarios por envío y una sola oferta abierta por vacante y profesional.
- **Al aprobar se vuelven a evaluar las reglas** (el estado puede haber cambiado desde la aceptación). Si hay infracciones bloqueantes que admiten excepción, la coordinación debe justificarla (≥ 10 caracteres), igual que en cualquier asignación; el resto de ofertas abiertas de la vacante se cierran en la misma transacción.
- Concurrencia: aceptar, aprobar, retirar y denegar se serializan con el bloqueo `necesidad:{id}` (el mismo que la asignación directa); dos aprobaciones simultáneas cubren la plaza una sola vez (la otra recibe 409).
- La cobertura directa («Buscar sustituto») se mantiene para urgencias y cierra las ofertas abiertas.

## Notificaciones in-app (módulo K)
Entidad `Notificacion` por usuario: tipo, título, mensaje, entidad vinculada, fecha y lectura. Se crean en la misma transacción que el hecho que notifican.

| Evento | Destinatario |
|---|---|
| Ausencia solicitada | Administradores y coordinadores |
| Ausencia aprobada / rechazada | Profesional |
| Vacantes abiertas por una aprobación | Administradores y coordinadores |
| Oferta recibida | Profesional |
| Oferta aceptada | Administradores y coordinadores |
| Oferta rechazada | Quien la ofreció |
| Sustitución aprobada / turno asignado | Profesional |
| Sustitución denegada, oferta retirada o cerrada | Profesional |

Privacidad: los textos describen el hecho (quién, qué turno, qué fechas) y nunca incluyen el comentario de la ausencia ni datos de salud. Cada usuario solo lista y marca las suyas (las de otro usuario o sociedad devuelven 404).

## API
| Recurso | Endpoints |
|---|---|
| Coordinación | `GET api/ofertas`, `POST api/vacantes/{id}/ofertas`, `POST api/ofertas/{id}/aprobar|denegar|retirar` |
| Profesional | `GET api/mis-ofertas`, `POST api/mis-ofertas/{id}/aceptar|rechazar` |
| Notificaciones | `GET api/notificaciones[?soloNoLeidas]`, `GET api/notificaciones/resumen`, `POST api/notificaciones/{id}/leer`, `POST api/notificaciones/leer-todas` |

Gestión: administrador de sociedad y coordinador médico. Todo filtrado por organización.

## Interfaz (Flutter)
- Campana con contador en la barra superior y pantalla *Notificaciones* (tocar un aviso lo marca como leído y lleva a la pantalla correspondiente).
- *Mi disponibilidad*: sección «Ofertas de sustitución» con aceptar / rechazar.
- *Ausencias › Vacantes*: «Ofrecer turno» (selección de varios candidatos y plazo opcional), «Buscar sustituto» (directo) y, bajo cada vacante, sus ofertas con aprobar (con justificación si procede), denegar y retirar.

## Pruebas
Backend: dominio 85, aplicación 35, integración 157 (flujo completo, rechazo, cierre de las demás ofertas, cobertura directa, retirar/denegar, revalidación de reglas con excepción, permisos, aislamiento entre sociedades, privacidad de notificaciones, aprobaciones simultáneas). Flutter: 67 tests en total.

## Limitaciones conocidas
- Solo avisos dentro de la aplicación; no hay correo ni notificaciones push. El contador se actualiza al navegar y al actuar, no en tiempo real.
- El plazo de las ofertas se muestra en la hora local del navegador; la caducidad se evalúa en servidor.
- La verificación en navegador con datos de demostración de las pantallas de E6 queda pendiente.
