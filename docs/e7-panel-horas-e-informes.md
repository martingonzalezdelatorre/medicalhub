# E7 — Panel, control horario e informes

Todo se calcula en el servidor (el cliente solo presenta) y respeta el aislamiento por sociedad y el alcance del usuario (el responsable de centro solo ve sus centros).

## Panel de inicio (`GET api/dashboard`)
Un único endpoint devuelve los bloques que corresponden al usuario:
- **Gestión** (administrador, coordinador y responsable de centro): cobertura de los próximos 14 días (plazas totales y cubiertas, sin contar cuadrantes archivados), cuadrantes en borrador y los 8 próximos turnos con plazas libres. Administración y coordinación añaden los **pendientes**: vacantes abiertas, ausencias por decidir, sustituciones por aprobar, disponibilidades por aprobar y excepciones autorizadas en los últimos 30 días. El responsable de centro no recibe los pendientes.
- **Profesional** (cualquier usuario con ficha vinculada): próximos 5 turnos, horas planificadas del mes, ofertas pendientes y ausencias solicitadas.
Si el panel no puede cargarse, la pantalla de inicio sigue mostrando el perfil.

## Control horario
- **Planificadas**: duración del turno asignado (las asignaciones canceladas no cuentan).
- **Registradas**: `RegistroHoras` (inicio y fin reales, ≤ 24 h) por asignación, registrado por administración o coordinación solo cuando el turno ya ha empezado. Puede corregirse; la entidad queda auditada.
- **Ajustes**: `AjusteHoras` en minutos (±1 a ±720), con **motivo obligatorio de al menos 10 caracteres** y autor. Son inmutables y requieren un registro previo.
- **Efectivas** = registradas + ajustes.
- **Nocturnas**: minutos entre las 22:00 y las 06:00 **locales del centro**, respetando los cambios de hora (una noche de otoño de 11 h tiene 9 h nocturnas; una de primavera de 9 h, 7 h). La franja es fija en este MVP; hacerla configurable queda como mejora.

## Informes
| Informe | Contenido |
|---|---|
| Horas (`api/informes/horas`) | Por profesional: turnos, guardias, planificadas, nocturnas, registradas, ajustes, efectivas y totales. |
| Cobertura (`api/informes/cobertura`) | Por servicio: plazas, cubiertas, libres, % de cobertura, vacantes abiertas y excepciones del periodo. |
| Registro (`api/horas`) | Asignación a asignación, para registrar y ajustar horas. |

Filtros: periodo (máximo 366 días), servicio y centro (y profesional en horas). Lectura: administración, coordinación y responsable de centro (con su alcance). **Exportación CSV** (`…/horas.csv`, `…/cobertura.csv`) solo para administración y coordinación: UTF-8 con BOM, separador `;`, horas con coma decimal, protección contra inyección de fórmulas y evento de seguridad `ExportacionDatos` en cada descarga.

Cada profesional consulta sus propias horas (`GET api/mis-horas`), visibles en «Mi disponibilidad».

## API
`GET api/dashboard` · `GET api/horas` · `GET api/mis-horas` · `PUT api/asignaciones/{id}/horas` · `POST api/asignaciones/{id}/horas/ajustes` · `GET api/informes/horas[.csv]` · `GET api/informes/cobertura[.csv]`

## Interfaz (Flutter)
- *Inicio*: tarjetas de cobertura, pendientes (enlazan a su pantalla) y turnos sin cubrir; para el profesional, próximos turnos y horas del mes.
- *Informes* (administración, coordinación y responsable): pestañas Horas, Registro de horas (registrar y ajustar) y Cobertura, con selector de periodo y servicio y botón de CSV para los gestores.

## Pruebas
Backend: dominio 94, aplicación 35, integración 168 (cálculo en hora local, registro y ajustes con rastro, validaciones, permisos por rol, aislamiento entre sociedades, CSV con BOM, cobertura y panel por rol). Flutter: 78 tests, de los que fallan 4 de login por un cambio ajeno (fondo animado) descrito en el resumen de E6.

## Limitaciones conocidas
- Sin exportación a Excel nativo ni PDF (el CSV abre en Excel); el diseño permite añadir otros formatos sobre los mismos informes.
- Las horas nocturnas no distinguen festivos ni categorías horarias configurables.
- El profesional no registra sus propias horas: lo hace la coordinación.
- La verificación en navegador con datos de demostración de las pantallas de E7 queda pendiente.
