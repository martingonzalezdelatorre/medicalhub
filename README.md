# MedicalHub

SaaS B2B para la gestión de turnos, guardias, disponibilidad y cobertura de profesionales sanitarios.
Backend ASP.NET Core (.NET 10) + SQL Server; frontend Flutter (web, móvil y escritorio).

Estado: **E0 – Cimientos** completada. Ver [docs/fase-1-analisis.md](docs/fase-1-analisis.md) para el análisis y el plan de entregas.

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
- Cadena de conexión: `ConnectionStrings__MedicalHub` (variable de entorno). En Desarrollo se usa LocalDB (`appsettings.Development.json`, sin secretos).
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
