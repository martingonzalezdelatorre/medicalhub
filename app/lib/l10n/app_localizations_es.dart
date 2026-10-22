// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Spanish Castilian (`es`).
class AppLocalizationsEs extends AppLocalizations {
  AppLocalizationsEs([String locale = 'es']) : super(locale);

  @override
  String get tituloApp => 'MedicalHub';

  @override
  String get reintentar => 'Reintentar';

  @override
  String get errorConexion =>
      'No se ha podido conectar con el servidor. Compruebe su conexión e inténtelo de nuevo.';

  @override
  String get errorInesperado => 'Se ha producido un error inesperado.';

  @override
  String get sinDatos => 'No hay datos para mostrar.';

  @override
  String versionEntorno(String version, String entorno) {
    return 'Versión $version · $entorno';
  }
}
