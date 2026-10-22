import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_es.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[Locale('es')];

  /// No description provided for @tituloApp.
  ///
  /// In es, this message translates to:
  /// **'MedicalHub'**
  String get tituloApp;

  /// No description provided for @reintentar.
  ///
  /// In es, this message translates to:
  /// **'Reintentar'**
  String get reintentar;

  /// No description provided for @errorConexion.
  ///
  /// In es, this message translates to:
  /// **'No se ha podido conectar con el servidor. Compruebe su conexión e inténtelo de nuevo.'**
  String get errorConexion;

  /// No description provided for @errorInesperado.
  ///
  /// In es, this message translates to:
  /// **'Se ha producido un error inesperado.'**
  String get errorInesperado;

  /// No description provided for @sinDatos.
  ///
  /// In es, this message translates to:
  /// **'No hay datos para mostrar.'**
  String get sinDatos;

  /// No description provided for @listaVacia.
  ///
  /// In es, this message translates to:
  /// **'Todavía no hay elementos.'**
  String get listaVacia;

  /// No description provided for @versionEntorno.
  ///
  /// In es, this message translates to:
  /// **'Versión {version} · {entorno}'**
  String versionEntorno(String version, String entorno);

  /// No description provided for @loginTitulo.
  ///
  /// In es, this message translates to:
  /// **'Iniciar sesión'**
  String get loginTitulo;

  /// No description provided for @campoCorreo.
  ///
  /// In es, this message translates to:
  /// **'Correo electrónico'**
  String get campoCorreo;

  /// No description provided for @campoContrasena.
  ///
  /// In es, this message translates to:
  /// **'Contraseña'**
  String get campoContrasena;

  /// No description provided for @campoCodigoMfa.
  ///
  /// In es, this message translates to:
  /// **'Código de verificación (6 dígitos)'**
  String get campoCodigoMfa;

  /// No description provided for @botonEntrar.
  ///
  /// In es, this message translates to:
  /// **'Entrar'**
  String get botonEntrar;

  /// No description provided for @mfaRequeridoAviso.
  ///
  /// In es, this message translates to:
  /// **'Introduzca el código de su aplicación autenticadora.'**
  String get mfaRequeridoAviso;

  /// No description provided for @validaObligatorio.
  ///
  /// In es, this message translates to:
  /// **'Campo obligatorio'**
  String get validaObligatorio;

  /// No description provided for @validaCorreo.
  ///
  /// In es, this message translates to:
  /// **'Introduzca un correo válido'**
  String get validaCorreo;

  /// No description provided for @validaLongitudMinima.
  ///
  /// In es, this message translates to:
  /// **'Mínimo {n} caracteres'**
  String validaLongitudMinima(int n);

  /// No description provided for @validaCodigo.
  ///
  /// In es, this message translates to:
  /// **'El código tiene 6 dígitos'**
  String get validaCodigo;

  /// No description provided for @mfaTitulo.
  ///
  /// In es, this message translates to:
  /// **'Verificación en dos pasos'**
  String get mfaTitulo;

  /// No description provided for @mfaInstrucciones.
  ///
  /// In es, this message translates to:
  /// **'Escanee el código QR con su aplicación autenticadora e introduzca el código de 6 dígitos para activar la protección.'**
  String get mfaInstrucciones;

  /// No description provided for @mfaClaveManual.
  ///
  /// In es, this message translates to:
  /// **'O introduzca esta clave manualmente'**
  String get mfaClaveManual;

  /// No description provided for @botonActivar.
  ///
  /// In es, this message translates to:
  /// **'Activar'**
  String get botonActivar;

  /// No description provided for @botonCopiar.
  ///
  /// In es, this message translates to:
  /// **'Copiar'**
  String get botonCopiar;

  /// No description provided for @copiado.
  ///
  /// In es, this message translates to:
  /// **'Copiado al portapapeles'**
  String get copiado;

  /// No description provided for @cerrarSesion.
  ///
  /// In es, this message translates to:
  /// **'Cerrar sesión'**
  String get cerrarSesion;

  /// No description provided for @menuInicio.
  ///
  /// In es, this message translates to:
  /// **'Inicio'**
  String get menuInicio;

  /// No description provided for @menuUsuarios.
  ///
  /// In es, this message translates to:
  /// **'Usuarios'**
  String get menuUsuarios;

  /// No description provided for @menuOrganizaciones.
  ///
  /// In es, this message translates to:
  /// **'Sociedades'**
  String get menuOrganizaciones;

  /// No description provided for @bienvenida.
  ///
  /// In es, this message translates to:
  /// **'Hola, {nombre}'**
  String bienvenida(String nombre);

  /// No description provided for @perfilSociedad.
  ///
  /// In es, this message translates to:
  /// **'Sociedad'**
  String get perfilSociedad;

  /// No description provided for @perfilRoles.
  ///
  /// In es, this message translates to:
  /// **'Roles'**
  String get perfilRoles;

  /// No description provided for @perfilMfa.
  ///
  /// In es, this message translates to:
  /// **'Verificación en dos pasos'**
  String get perfilMfa;

  /// No description provided for @mfaActiva.
  ///
  /// In es, this message translates to:
  /// **'Activada'**
  String get mfaActiva;

  /// No description provided for @mfaInactiva.
  ///
  /// In es, this message translates to:
  /// **'No activada'**
  String get mfaInactiva;

  /// No description provided for @usuariosTitulo.
  ///
  /// In es, this message translates to:
  /// **'Usuarios'**
  String get usuariosTitulo;

  /// No description provided for @nuevoUsuario.
  ///
  /// In es, this message translates to:
  /// **'Nuevo usuario'**
  String get nuevoUsuario;

  /// No description provided for @campoNombre.
  ///
  /// In es, this message translates to:
  /// **'Nombre completo'**
  String get campoNombre;

  /// No description provided for @campoRoles.
  ///
  /// In es, this message translates to:
  /// **'Roles'**
  String get campoRoles;

  /// No description provided for @validaRol.
  ///
  /// In es, this message translates to:
  /// **'Seleccione al menos un rol'**
  String get validaRol;

  /// No description provided for @botonGuardar.
  ///
  /// In es, this message translates to:
  /// **'Guardar'**
  String get botonGuardar;

  /// No description provided for @botonCancelar.
  ///
  /// In es, this message translates to:
  /// **'Cancelar'**
  String get botonCancelar;

  /// No description provided for @estadoActivo.
  ///
  /// In es, this message translates to:
  /// **'Activo'**
  String get estadoActivo;

  /// No description provided for @estadoInactivo.
  ///
  /// In es, this message translates to:
  /// **'Inactivo'**
  String get estadoInactivo;

  /// No description provided for @accionDesactivar.
  ///
  /// In es, this message translates to:
  /// **'Desactivar'**
  String get accionDesactivar;

  /// No description provided for @accionActivar.
  ///
  /// In es, this message translates to:
  /// **'Activar'**
  String get accionActivar;

  /// No description provided for @accionReiniciarMfa.
  ///
  /// In es, this message translates to:
  /// **'Reiniciar verificación en dos pasos'**
  String get accionReiniciarMfa;

  /// No description provided for @operacionRealizada.
  ///
  /// In es, this message translates to:
  /// **'Operación realizada'**
  String get operacionRealizada;

  /// No description provided for @organizacionesTitulo.
  ///
  /// In es, this message translates to:
  /// **'Sociedades'**
  String get organizacionesTitulo;

  /// No description provided for @nuevaOrganizacion.
  ///
  /// In es, this message translates to:
  /// **'Nueva sociedad'**
  String get nuevaOrganizacion;

  /// No description provided for @campoNombreSociedad.
  ///
  /// In es, this message translates to:
  /// **'Nombre de la sociedad'**
  String get campoNombreSociedad;

  /// No description provided for @campoZonaHoraria.
  ///
  /// In es, this message translates to:
  /// **'Zona horaria'**
  String get campoZonaHoraria;

  /// No description provided for @campoMfaObligatorio.
  ///
  /// In es, this message translates to:
  /// **'Exigir verificación en dos pasos a administradores y coordinadores'**
  String get campoMfaObligatorio;

  /// No description provided for @datosAdministrador.
  ///
  /// In es, this message translates to:
  /// **'Administrador inicial'**
  String get datosAdministrador;

  /// No description provided for @rolAdministradorPlataforma.
  ///
  /// In es, this message translates to:
  /// **'Administrador de plataforma'**
  String get rolAdministradorPlataforma;

  /// No description provided for @rolAdministradorSociedad.
  ///
  /// In es, this message translates to:
  /// **'Administrador de sociedad'**
  String get rolAdministradorSociedad;

  /// No description provided for @rolCoordinadorMedico.
  ///
  /// In es, this message translates to:
  /// **'Coordinador médico'**
  String get rolCoordinadorMedico;

  /// No description provided for @rolProfesionalSanitario.
  ///
  /// In es, this message translates to:
  /// **'Profesional sanitario'**
  String get rolProfesionalSanitario;

  /// No description provided for @rolResponsableCentro.
  ///
  /// In es, this message translates to:
  /// **'Responsable de centro'**
  String get rolResponsableCentro;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['es'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'es':
      return AppLocalizationsEs();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
