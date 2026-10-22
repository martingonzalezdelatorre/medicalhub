/// Configuración inyectada en compilación: `--dart-define=API_BASE_URL=https://...`
class Entorno {
  static const urlBaseApi = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5292',
  );
}
