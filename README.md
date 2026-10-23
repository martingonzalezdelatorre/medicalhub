# MedicalHub

SaaS B2B para la gestión de turnos, guardias, disponibilidad y cobertura de profesionales sanitarios.
Backend ASP.NET Core (.NET 10) + SQL Server; frontend Flutter (web, móvil y escritorio).

Estado: **E0** (cimientos), **E1** (identidad y multiempresa), **E2** (catálogos, centros, servicios y profesionales) y **E3** (cuadrantes, turnos y asignaciones) , **E4** (motor de reglas y excepciones) , **E5** (ausencias, disponibilidad y vacantes) , **E6** (sustituciones y notificaciones) y **E7** (panel, horas e informes) completadas. Ver [docs/fase-1-analisis.md](docs/fase-1-analisis.md) (análisis y plan de entregas), [E1](docs/e1-identidad-y-multiempresa.md), [E2](docs/e2-catalogos-estructura-profesionales.md) (incluye la herramienta de datos de demostración), [E3](docs/e3-planificacion.md) , [E4](docs/e4-reglas-y-excepciones.md) , [E5](docs/e5-ausencias-y-disponibilidad.md) , [E6](docs/e6-sustituciones-y-notificaciones.md) y [E7](docs/e7-panel-horas-e-informes.md).

## Arranque rápido (desarrollo)
```bash
# 1. API (crea/migra MedicalHub_Dev en LocalDB y el primer administrador de plataforma)
cd backend
export Bootstrap__AdministradorPlataforma__Email=<correo>
export Bootstrap__AdministradorPlataforma__Contrasena=<≥12 caracteres>
dotnet run --project src/MedicalHub.Api --launch-profile http

# 2. App web (otra terminal)
cd app && flutter run -d chrome --web-port 8080 --dart-define=API_BASE_URL=http://localhost:5292
```
El administrador de plataforma debe configurar su verificación en dos pasos en el primer acceso. Desde «Sociedades» se crean las sociedades y su administrador inicial.
Compilar para Windows requiere activar el *Modo de desarrollador* de Windows (symlinks de plugins).

## Requisitos
- .NET SDK 10, Flutter estable (3.44+), `dotnet-ef` (`dotnet tool install -g dotnet-ef`)
- SQL Server o LocalDB (`(localdb)\MSSQLLocalDB` en Windows)

## Backend
```bash
cd backend
dotnet ef database update -p src/MedicalHub.Infrastructure -s src/MedicalHub.Api   # aplica migraciones
dotnet run --project src/MedicalHub.Api --launch-profile http                       # http://localhost:5292
dotnet test MedicalHub.sln
```
- OpenAPI (solo Desarrollo): `/openapi/v1.json`
- Health checks: `/health/live`, `/health/ready` (comprueba la base de datos)
- Cadena de conexión: `ConnectionStrings__Conn` (variable de entorno). En Desarrollo se usa LocalDB (`appsettings.Development.json`, sin secretos).
- Pruebas de integración: usan `MedicalHub_Test` en LocalDB; para otro servidor, defina `MEDICALHUB_TEST_DB`.
- Nueva migración: `dotnet ef migrations add <Nombre> -p src/MedicalHub.Infrastructure -s src/MedicalHub.Api -o Persistencia/Migraciones`

## Frontend
```bash
cd app
flutter pub get
dart run build_runner build --delete-conflicting-outputs   # modelos JSON
flutter run -d chrome --web-port 8080 --dart-define=API_BASE_URL=http://localhost:5292
flutter test
flutter build web --release --dart-define=API_BASE_URL=https://api.ejemplo.es
```
`app/build/web` se publica en cualquier hosting estático (reescribir rutas desconocidas a `index.html`).
La URL del frontend debe figurar en `Cors__OrigenesPermitidos` de la API.

## Estructura
```
backend/src   Domain · Application · Infrastructure · Api
backend/tests Domain.Tests · Api.IntegrationTests
app/lib       core · shared · features/<funcionalidad>/{data,application,presentation}
deploy/       ejemplos de configuración (sin secretos)
docs/         análisis y documentación
```

## Seguridad
No se versionan secretos. Use variables de entorno o un gestor de secretos (`deploy/.env.example` es solo una plantilla).
