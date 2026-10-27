# E8 — Guía de instalación y operación

Instalación reproducible de MedicalHub: SQL Server + API (.NET 10) + web (Flutter Web). Hay dos caminos: **contenedores** (referencia) o **servidor Windows con IIS**. Los pasos de base de datos, secretos, copias de seguridad y actualización son los mismos.

## 1. Requisitos
| Componente | Versión |
|---|---|
| SQL Server | 2019 o superior (Express sirve para pilotos; sin compresión de copias) |
| .NET SDK / Runtime | 10.0 |
| Flutter | estable 3.44 o superior (solo para compilar la web) |
| HTTPS | certificado válido en el punto público (proxy inverso o IIS) |

## 2. Secretos y configuración
**Nada secreto va en el repositorio ni en `appsettings*.json`.** Se inyectan por variables de entorno (o *User Secrets* en desarrollo). Plantilla: `deploy/.env.example`.

| Variable | Descripción |
|---|---|
| `ConnectionStrings__Conn` | Cadena de conexión con el **usuario de mínimo privilegio** (no `sa`). |
| `Seguridad__ClaveJwt` | Clave HMAC ≥ 32 bytes, aleatoria. Cambiarla cierra todas las sesiones. |
| `Cors__OrigenesPermitidos__0` | Origen público de la web. Con la web y la API bajo el mismo origen (`/api`), no hay CORS. |
| `DataProtection__RutaClaves` | Carpeta persistente y protegida donde se guarda el anillo de claves que cifra los secretos MFA. **Hay que incluirla en las copias**: si se pierde, los usuarios deben volver a configurar el MFA. |
| `Seguridad__CookieRefrescoSegura` | `true` por defecto: la cookie de refresco de la web solo viaja por HTTPS. Desactívelo solo en desarrollo local con HTTP. |
| `Base__MigrarAlIniciar` | `false` en producción (las migraciones se aplican con el script, paso 4). |
| `Bootstrap__AdministradorPlataforma__*` | Solo la primera vez, para crear el administrador de plataforma. Vacíelas después. |
| `Retencion__*` | Plazos de depuración (ver `docs/rgpd.md`). |

Desarrollo local: `dotnet user-secrets set "ConnectionStrings:Conn" "<cadena>"` desde `backend/src/MedicalHub.Api` (las pruebas automáticas ignoran los *user secrets* a propósito, para que nunca apunten a un servidor real).

## 3. Base de datos y usuarios
1. Cree la base `MedicalHub` vacía.
2. Ejecute `deploy/sql/crear-usuario-aplicacion.sql` (cambiando las contraseñas): crea `medicalhub_app` (solo lectura y escritura de datos) y `medicalhub_migrador` (cambios de esquema).
3. La API se conecta siempre con `medicalhub_app`.

## 4. Migraciones
Se aplican con el **script idempotente**, que puede ejecutarse varias veces sin efectos:

```bash
sqlcmd -S <servidor> -d MedicalHub -U medicalhub_migrador -C -i backend/db/medicalhub-migraciones.sql
```

Para regenerarlo tras añadir migraciones (herramienta `dotnet-ef` 10.0.13 o superior):

```bash
dotnet ef migrations script --idempotent -p backend/src/MedicalHub.Infrastructure -s backend/src/MedicalHub.Api -o backend/db/medicalhub-migraciones.sql
```

## 5A. Contenedores
```bash
cd app && flutter build web --release --dart-define=API_BASE_URL=/ && cd ..
cd deploy && cp .env.example .env     # rellene .env (fuera del repositorio)
docker compose --env-file .env up -d --build
```
La web escucha en `WEB_PORT` (8080) y reenvía `/api` a la API. Ponga delante un proxy con HTTPS (nginx, Traefik, Caddy, balanceador del proveedor).

## 5B. Windows con IIS
1. Instale el *Hosting Bundle* de .NET 10 y el módulo de reescritura de URL.
2. `dotnet publish backend/src/MedicalHub.Api -c Release -o C:\inetpub\medicalhub-api` y cree un sitio/aplicación que lo sirva.
3. Defina las variables de entorno en el grupo de aplicaciones (no en `web.config` versionado).
4. Copie `app/build/web` a un segundo sitio y configure la reescritura: `/api/*` → API; cualquier otra ruta → `index.html`.
5. Habilite HTTPS y redirección; la API ya envía HSTS en producción.

## 6. Primer arranque
1. Arranque con las variables `Bootstrap__AdministradorPlataforma__*` definidas; el log avisa «Se ha creado el administrador de plataforma inicial».
2. **Retire esas variables** y reinicie.
3. Entre con ese usuario, active el MFA (obligatorio para roles de gestión) y cree la primera sociedad (organización) y su administrador.
4. Comprobaciones: `GET /health/live` (proceso) y `GET /health/ready` (acceso a la base de datos) responden 200.

## 7. Copia de seguridad y restauración (probadas)
Scripts en `deploy/scripts/` (PowerShell + `sqlcmd`):

```powershell
.\backup.ps1  -Servidor sql01 -BaseDatos MedicalHub -Carpeta D:\copias -Comprimir
.\restore.ps1 -Servidor sql01 -Archivo D:\copias\MedicalHub-<fecha>.bak -BaseDestino MedicalHub_Restaurada -BaseOrigen MedicalHub
```
- `backup.ps1`: copia completa con `CHECKSUM` y `RESTORE VERIFYONLY`.
- `restore.ps1`: restaura **siempre en una base nueva** (no sobrescribe), y con `-BaseOrigen` ejecuta `verificar-restauracion.ps1`: compara el número de filas de todas las tablas y lanza `DBCC CHECKDB`. Falla si algo no coincide.
- Contraseña SQL por la variable `MEDICALHUB_SQL_PASSWORD`, nunca por parámetro.
- **Verificación realizada:** copia de una base de desarrollo en LocalDB (30 tablas), restauración en una base nueva, coincidencia de todas las tablas y `DBCC CHECKDB` correcto. Repita este ensayo en su entorno al instalar y de forma periódica.
- Las copias contienen datos personales: cífrelas, restrinja el acceso y respete los plazos de retención. Incluya también el anillo de Data Protection.
- Recomendación: copia completa diaria, diferencial o de registro según el RPO que defina la sociedad, y un ensayo de restauración trimestral.

## 8. Actualizar a una versión nueva
1. Copia de seguridad (paso 7).
2. Aplique el script de migraciones (paso 4).
3. Despliegue la nueva API y la nueva web. Las sesiones abiertas siguen válidas mientras no cambie `Seguridad__ClaveJwt`.
4. Compruebe `/health/ready` y un inicio de sesión.

## 9. Operación
- **Registros:** Serilog a consola (JSON/texto según configuración); cada petición lleva un identificador de correlación (`X-Correlation-Id`). No se registran contraseñas ni tokens.
- **Auditoría y seguridad:** tablas `RegistrosAuditoria` y `EventosSeguridad` (inicios de sesión, bloqueos, reutilización de tokens, exportaciones).
- **Límites:** 600 peticiones/minuto por usuario, 10 intentos de inicio de sesión/minuto por IP, cuerpo máximo 1 MB (configurables en `Seguridad__*`).
- **Integración continua:** `.github/workflows/ci.yml` compila, ejecuta todas las pruebas (backend con SQL Server efímero; Flutter con análisis y compilación web) y falla si hay paquetes con vulnerabilidades conocidas.

## 10. Lo que NO está verificado
- Los `Dockerfile` y `docker-compose.yml` se han escrito pero **no se han construido** en este entorno (no hay Docker): pruébelos antes de usarlos.
- La guía de IIS no se ha ejecutado.
- El usuario de mínimo privilegio no se ha probado contra un servidor con autenticación SQL (LocalDB solo admite autenticación Windows).
- La web con `API_BASE_URL=/` (mismo origen) tras un proxy con HTTPS no se ha probado en un navegador.
- No se han compilado ni probado las versiones móvil y de escritorio.
