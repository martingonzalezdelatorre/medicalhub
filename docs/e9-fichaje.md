# E9 · Fichaje con geolocalización y pausas

El profesional ficha su propia jornada desde la app (sección **Fichaje**): entrada, pausa, reanudar y salida. Al salir, la jornada se vuelca a `RegistroHoras` de su asignación y las pausas se descuentan con un ajuste negativo («Pausas registradas en el fichaje»), de modo que informes, CSV y «Mis horas» no cambian.

## Reglas
- La entrada se asocia a la asignación activa del profesional que empieza en las próximas 2 h o está en curso y aún no tiene horas registradas ni fichaje cerrado. Sin turno no se puede fichar (409).
- Una sola jornada abierta por profesional (también garantizado por índice único filtrado). Una jornada abierta más de 24 h se descarta y no genera horas.
- Se puede salir estando en pausa (la pausa se cierra con la salida).
- Si el fichaje de una asignación ya tiene horas registradas por la coordinación, la salida las actualiza.

## Geolocalización
- La app pide la ubicación **solo al fichar entrada y salida**. Si no se puede obtener (permiso, GPS, señal), el profesional decide si ficha sin ella; queda como «sin ubicación».
- Cada centro puede tener una **geovalla** circular (`PUT api/centros/{id}/geovalla` con `latitud`, `longitud`, `radioMetros` de 20 a 5000; todo a `null` la quita). Solo el administrador de la sociedad la define.
- Fuera de la geovalla el fichaje **se admite pero se marca**; la coordinación lo revisa en `GET api/fichajes/fuera-de-zona`. Se concede un margen igual a la precisión del GPS (máx. 100 m).
- Las coordenadas se anonimizan a los `Retencion__UbicacionesFichajeDias` (90 por defecto; 0 desactiva). No se copian a la auditoría de cambios.

## API
| Método y ruta | Política |
|---|---|
| `GET api/fichajes/estado` | UsuarioSociedad (profesional con ficha) |
| `POST api/fichajes/entrada` · `salida` (cuerpo: `latitud`, `longitud`, `precisionMetros`) | UsuarioSociedad |
| `POST api/fichajes/pausa` · `reanudar` | UsuarioSociedad |
| `GET api/mis-fichajes?desde&hasta` | UsuarioSociedad |
| `GET api/fichajes/fuera-de-zona?desde&hasta` | LecturaSociedad (con el alcance del usuario) |
| `PUT api/centros/{id}/geovalla` | AdministradorSociedad |

## Pendiente
- La app no tiene pantalla para definir la geovalla (solo API).
- Revisión de fuera de zona solo por API; sin pantalla en Informes.
- Valorar con el DPO la EIPD formal y la comunicación a los profesionales (ver `docs/rgpd.md`).
- Probar en dispositivo real (Android/iOS): permisos de ubicación y precisión del GPS.
