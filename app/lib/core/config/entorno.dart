import 'package:flutter/foundation.dart' show kIsWeb;

/// Configuración inyectada en compilación: `--dart-define=API_BASE_URL=https://...`
class Entorno {
  static const urlBaseApi = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5292',
  );

  /// En la web el token de refresco viaja en una cookie HttpOnly que gestiona el navegador (el JavaScript no puede leerla).
  /// En móvil y escritorio se guarda en el almacén seguro del dispositivo. Se puede forzar con `--dart-define=SESION_COOKIE=true|false`.
  static const sesionCookie = bool.fromEnvironment('SESION_COOKIE', defaultValue: kIsWeb);
}
