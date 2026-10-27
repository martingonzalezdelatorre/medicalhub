# Seguridad: controles y revisión (E8)

## Controles por capa
| Capa | Control | Verificación automática |
|---|---|---|
| Autorización | Política por defecto: sesión completa (sin MFA pendiente). Políticas con nombre por rol; nunca `[Authorize(Roles=…)]`. Todo endpoint declara su autorización. | `CoberturaAutorizacionTests`: ningún endpoint sin `[Authorize]`/`[AllowAnonymous]`; lista blanca de públicos; todos los `GET` protegidos devuelven 401 sin sesión. |
| Multiempresa | Filtro global por organización en toda entidad `IPerteneceAOrganizacion`; guardia de escritura; org solo del token. Ids de otra sociedad → 404. | Pruebas de aislamiento en cada módulo y una que comprueba que ninguna entidad se queda sin filtro. |
| Autenticación | Hash adaptativo, bloqueo, MFA TOTP, token de acceso 15 min, refresco rotatorio con detección de reutilización. **En la web el refresco va en una cookie `HttpOnly; SameSite=Strict` (ver abajo).** | `AutenticacionTests`, `MfaTests`, `TotpTests`. |
| Abuso | Límite de login por IP (10/min) y **límite global por usuario/IP (600/min)**; cuerpo máximo 1 MB; sin cabecera `Server`. | `Kestrel_limita_el_cuerpo…` |
| Cabeceras | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Cache-Control: no-store`, **`Content-Security-Policy` restrictiva, `Permissions-Policy`, `Cross-Origin-Resource-Policy`**; HSTS en producción. La web añade su propia CSP (nginx). | `La_api_envia_cabeceras_de_seguridad…` |
| Datos | Sin datos clínicos; ausencias sin motivo médico; exportaciones auditadas; depuración por retención. | `RetencionTests`, `AusenciasTests` (privacidad). |
| Secretos | Fuera del repositorio; *User Secrets* en desarrollo; las pruebas ignoran secretos y variables del desarrollador. | Revisión de código y `.gitignore`. |
| Cadena de suministro | Versiones fijadas; CI falla con paquetes vulnerables conocidos. | Paso «Paquetes con vulnerabilidades» del CI. |

## Sesión de la web: cookie HttpOnly
El token de refresco (la credencial de larga duración, 7 días) **no llega nunca al JavaScript** de la web:

- La API lo entrega en la cookie `mh_refresh`: `HttpOnly`, `Secure` (salvo desarrollo local con HTTP), `SameSite=Strict`, `Path=/api/autenticacion` y caducidad igual a la del token. El cuerpo de la respuesta lo devuelve vacío.
- Es **opt-in por petición** con la cabecera `X-Sesion-Cookie: 1`, que añade el cliente web. Móvil y escritorio no la envían y siguen guardando el token en el almacén seguro del dispositivo (sin cambios para ellos).
- **CSRF:** la cookie solo se usa si la petición trae esa cabecera personalizada (un formulario o una imagen de otro sitio no puede añadirla; un `fetch` con ella exige pasar el CORS) y además es SameSite=Strict. Una petición con cookie pero sin cabecera recibe 401.
- **CORS con credenciales:** solo para los orígenes de `Cors__OrigenesPermitidos` (en desarrollo, sin lista, solo `localhost`). Con la web y la API bajo el mismo origen (`/api`, despliegue recomendado) no hace falta CORS.
- Rotación y detección de reutilización idénticas: reutilizar una cookie ya rotada invalida toda la familia y borra la cookie. Cerrar sesión la borra y revoca el token.
- Al arrancar, la web intenta renovar la sesión con la cookie (no puede saber si existe porque es HttpOnly); si no hay cookie válida, muestra el login.
- Configuración: `Seguridad__CookieRefrescoSegura` (por defecto `true`). Si la web y la API están en sitios distintos (dominios no relacionados), SameSite=Strict impide enviar la cookie: use el mismo sitio (mismo dominio o subdominios).

## Hallazgos de esta revisión y estado
| # | Hallazgo | Estado |
|---|---|---|
| 1 | La cadena de conexión con la contraseña de `sa` estaba en `appsettings*.json` versionados. | **Corregido**: movida a *User Secrets*; los ficheros del repositorio quedan sin credenciales. **Acción del usuario:** rotar esa contraseña (ya se compartió) y no usar `sa` en la aplicación. |
| 2 | Sin límite global de peticiones. | **Corregido** (600/min por usuario o IP, configurable). |
| 3 | Sin límite de tamaño de petición. | **Corregido** (1 MB). |
| 4 | Faltaban CSP y Permissions-Policy en la API. | **Corregido**. |
| 5 | Sin depuración de datos antiguos. | **Corregido** (servicio de retención configurable). |
| 6 | Nada impedía añadir un endpoint sin autorización explícita. | **Corregido** (prueba automática). |
| 7 | Las pruebas podían heredar configuración del desarrollador (riesgo de tocar un servidor real). | **Corregido** (configuración en memoria con máxima precedencia). |
| 8 | La aplicación debía conectarse con un usuario de mínimo privilegio. | **Preparado** (`deploy/sql/crear-usuario-aplicacion.sql`); no verificado con autenticación SQL real. |

## Riesgos residuales y decisiones aplazadas
- **Un XSS en la web sigue pudiendo actuar como el usuario mientras dure la pestaña** (por ejemplo, llamar a la API con el token de acceso que está en memoria). La cookie impide **robar** la sesión de larga duración, no que se abuse de ella mientras el script malicioso se ejecuta; por eso se mantiene la CSP estricta y el token de acceso de 15 minutos.
- **Limitador de peticiones en memoria:** válido con una sola instancia de la API; con varias instancias habría que usar un almacén compartido o el limitador del proxy.
- **Sin escáner de contenido ni WAF.** Recomendado delante de la API si se expone a Internet.
- **Franja horaria nocturna y plazos de retención** son valores por defecto, no decisiones legales.
- **Pruebas de penetración** externas: no realizadas.
