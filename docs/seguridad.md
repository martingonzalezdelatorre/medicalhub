# Seguridad: controles y revisión (E8)

## Controles por capa
| Capa | Control | Verificación automática |
|---|---|---|
| Autorización | Política por defecto: sesión completa (sin MFA pendiente). Políticas con nombre por rol; nunca `[Authorize(Roles=…)]`. Todo endpoint declara su autorización. | `CoberturaAutorizacionTests`: ningún endpoint sin `[Authorize]`/`[AllowAnonymous]`; lista blanca de públicos; todos los `GET` protegidos devuelven 401 sin sesión. |
| Multiempresa | Filtro global por organización en toda entidad `IPerteneceAOrganizacion`; guardia de escritura; org solo del token. Ids de otra sociedad → 404. | Pruebas de aislamiento en cada módulo y una que comprueba que ninguna entidad se queda sin filtro. |
| Autenticación | Hash adaptativo, bloqueo, MFA TOTP, token de acceso 15 min, refresco rotatorio con detección de reutilización. | `AutenticacionTests`, `MfaTests`, `TotpTests`. |
| Abuso | Límite de login por IP (10/min) y **límite global por usuario/IP (600/min)**; cuerpo máximo 1 MB; sin cabecera `Server`. | `Kestrel_limita_el_cuerpo…` |
| Cabeceras | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Cache-Control: no-store`, **`Content-Security-Policy` restrictiva, `Permissions-Policy`, `Cross-Origin-Resource-Policy`**; HSTS en producción. La web añade su propia CSP (nginx). | `La_api_envia_cabeceras_de_seguridad…` |
| Datos | Sin datos clínicos; ausencias sin motivo médico; exportaciones auditadas; depuración por retención. | `RetencionTests`, `AusenciasTests` (privacidad). |
| Secretos | Fuera del repositorio; *User Secrets* en desarrollo; las pruebas ignoran secretos y variables del desarrollador. | Revisión de código y `.gitignore`. |
| Cadena de suministro | Versiones fijadas; CI falla con paquetes vulnerables conocidos. | Paso «Paquetes con vulnerabilidades» del CI. |

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
- **Token de refresco en almacenamiento del navegador (web).** Es el riesgo principal de la versión web: un XSS podría leerlo. Mitigaciones actuales: sesión de acceso de 15 min, rotación con detección de reutilización, CSP estricta en la web servida por nginx. La mejora estructural (cookie `HttpOnly; Secure; SameSite=Strict` bajo el mismo origen, que el despliegue con `/api` en el mismo dominio ya permite) **no se ha implementado** en este MVP porque exige cambios coordinados en API y cliente y pruebas de navegador que no se han podido hacer aquí. Es la primera mejora recomendada.
- **Limitador de peticiones en memoria:** válido con una sola instancia de la API; con varias instancias habría que usar un almacén compartido o el limitador del proxy.
- **Sin escáner de contenido ni WAF.** Recomendado delante de la API si se expone a Internet.
- **Franja horaria nocturna y plazos de retención** son valores por defecto, no decisiones legales.
- **Pruebas de penetración** externas: no realizadas.
