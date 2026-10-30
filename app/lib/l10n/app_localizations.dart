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

  /// No description provided for @rolRecursosHumanos.
  ///
  /// In es, this message translates to:
  /// **'RRHH y Administración'**
  String get rolRecursosHumanos;

  /// No description provided for @menuProfesionales.
  ///
  /// In es, this message translates to:
  /// **'Profesionales'**
  String get menuProfesionales;

  /// No description provided for @menuCentros.
  ///
  /// In es, this message translates to:
  /// **'Centros'**
  String get menuCentros;

  /// No description provided for @menuCatalogos.
  ///
  /// In es, this message translates to:
  /// **'Catálogos'**
  String get menuCatalogos;

  /// No description provided for @editar.
  ///
  /// In es, this message translates to:
  /// **'Editar'**
  String get editar;

  /// No description provided for @anadir.
  ///
  /// In es, this message translates to:
  /// **'Añadir'**
  String get anadir;

  /// No description provided for @quitar.
  ///
  /// In es, this message translates to:
  /// **'Quitar'**
  String get quitar;

  /// No description provided for @buscar.
  ///
  /// In es, this message translates to:
  /// **'Buscar'**
  String get buscar;

  /// No description provided for @soloActivos.
  ///
  /// In es, this message translates to:
  /// **'Solo activos'**
  String get soloActivos;

  /// No description provided for @campoDescripcion.
  ///
  /// In es, this message translates to:
  /// **'Descripción'**
  String get campoDescripcion;

  /// No description provided for @catEspecialidades.
  ///
  /// In es, this message translates to:
  /// **'Especialidades'**
  String get catEspecialidades;

  /// No description provided for @catCategorias.
  ///
  /// In es, this message translates to:
  /// **'Perfiles'**
  String get catCategorias;

  /// No description provided for @catTarifas.
  ///
  /// In es, this message translates to:
  /// **'Tarifas por turno'**
  String get catTarifas;

  /// No description provided for @tarifasAyuda.
  ///
  /// In es, this message translates to:
  /// **'Coste de cada turno según el perfil que lo trabaja. Los turnos de un servicio se definen en su ficha.'**
  String get tarifasAyuda;

  /// No description provided for @tarifasServicio.
  ///
  /// In es, this message translates to:
  /// **'Servicio'**
  String get tarifasServicio;

  /// No description provided for @tarifasSinTurnos.
  ///
  /// In es, this message translates to:
  /// **'Este servicio todavía no tiene turnos. Créelos en la ficha del servicio.'**
  String get tarifasSinTurnos;

  /// No description provided for @tarifasSinPerfiles.
  ///
  /// In es, this message translates to:
  /// **'Primero cree algún perfil en la pestaña «Perfiles».'**
  String get tarifasSinPerfiles;

  /// No description provided for @tarifasGestionarTurnos.
  ///
  /// In es, this message translates to:
  /// **'Gestionar turnos del servicio'**
  String get tarifasGestionarTurnos;

  /// No description provided for @tarifasImporte.
  ///
  /// In es, this message translates to:
  /// **'€ por turno'**
  String get tarifasImporte;

  /// No description provided for @tarifasGuardado.
  ///
  /// In es, this message translates to:
  /// **'Tarifas guardadas'**
  String get tarifasGuardado;

  /// No description provided for @validaImporte.
  ///
  /// In es, this message translates to:
  /// **'Importe no válido'**
  String get validaImporte;

  /// No description provided for @catCompetencias.
  ///
  /// In es, this message translates to:
  /// **'Competencias'**
  String get catCompetencias;

  /// No description provided for @catNuevo.
  ///
  /// In es, this message translates to:
  /// **'Nuevo elemento'**
  String get catNuevo;

  /// No description provided for @catEditar.
  ///
  /// In es, this message translates to:
  /// **'Editar elemento'**
  String get catEditar;

  /// No description provided for @centrosTitulo.
  ///
  /// In es, this message translates to:
  /// **'Centros'**
  String get centrosTitulo;

  /// No description provided for @nuevoCentro.
  ///
  /// In es, this message translates to:
  /// **'Nuevo centro'**
  String get nuevoCentro;

  /// No description provided for @editarCentro.
  ///
  /// In es, this message translates to:
  /// **'Editar centro'**
  String get editarCentro;

  /// No description provided for @campoTipoCentro.
  ///
  /// In es, this message translates to:
  /// **'Tipo de centro'**
  String get campoTipoCentro;

  /// No description provided for @campoDireccion.
  ///
  /// In es, this message translates to:
  /// **'Dirección'**
  String get campoDireccion;

  /// No description provided for @campoResponsables.
  ///
  /// In es, this message translates to:
  /// **'Responsables del centro'**
  String get campoResponsables;

  /// No description provided for @tipoHospital.
  ///
  /// In es, this message translates to:
  /// **'Hospital'**
  String get tipoHospital;

  /// No description provided for @tipoClinica.
  ///
  /// In es, this message translates to:
  /// **'Clínica'**
  String get tipoClinica;

  /// No description provided for @tipoCentroAmbulatorio.
  ///
  /// In es, this message translates to:
  /// **'Centro ambulatorio'**
  String get tipoCentroAmbulatorio;

  /// No description provided for @tipoOtro.
  ///
  /// In es, this message translates to:
  /// **'Otro'**
  String get tipoOtro;

  /// No description provided for @serviciosTitulo.
  ///
  /// In es, this message translates to:
  /// **'Servicios'**
  String get serviciosTitulo;

  /// No description provided for @nuevoServicio.
  ///
  /// In es, this message translates to:
  /// **'Nuevo servicio'**
  String get nuevoServicio;

  /// No description provided for @editarServicio.
  ///
  /// In es, this message translates to:
  /// **'Editar servicio'**
  String get editarServicio;

  /// No description provided for @campoCoordinador.
  ///
  /// In es, this message translates to:
  /// **'Coordinador del servicio'**
  String get campoCoordinador;

  /// No description provided for @sinCoordinador.
  ///
  /// In es, this message translates to:
  /// **'Sin coordinador'**
  String get sinCoordinador;

  /// No description provided for @campoCategoriasAdmitidas.
  ///
  /// In es, this message translates to:
  /// **'Categorías admitidas (vacío = cualquiera)'**
  String get campoCategoriasAdmitidas;

  /// No description provided for @campoCompetenciasRequeridas.
  ///
  /// In es, this message translates to:
  /// **'Competencias requeridas'**
  String get campoCompetenciasRequeridas;

  /// No description provided for @campoReglas.
  ///
  /// In es, this message translates to:
  /// **'Reglas específicas (JSON, opcional)'**
  String get campoReglas;

  /// No description provided for @tiposTurnoTitulo.
  ///
  /// In es, this message translates to:
  /// **'Tipos de turno'**
  String get tiposTurnoTitulo;

  /// No description provided for @nuevoTipoTurno.
  ///
  /// In es, this message translates to:
  /// **'Nuevo tipo de turno'**
  String get nuevoTipoTurno;

  /// No description provided for @editarTipoTurno.
  ///
  /// In es, this message translates to:
  /// **'Editar tipo de turno'**
  String get editarTipoTurno;

  /// No description provided for @campoHoraInicio.
  ///
  /// In es, this message translates to:
  /// **'Inicio'**
  String get campoHoraInicio;

  /// No description provided for @campoHoraFin.
  ///
  /// In es, this message translates to:
  /// **'Fin'**
  String get campoHoraFin;

  /// No description provided for @campoGuardia.
  ///
  /// In es, this message translates to:
  /// **'Es guardia'**
  String get campoGuardia;

  /// No description provided for @cruzaMedianoche.
  ///
  /// In es, this message translates to:
  /// **'termina al día siguiente'**
  String get cruzaMedianoche;

  /// No description provided for @duracionHoras.
  ///
  /// In es, this message translates to:
  /// **'{horas} h'**
  String duracionHoras(String horas);

  /// No description provided for @requisitosTitulo.
  ///
  /// In es, this message translates to:
  /// **'Requisitos de cobertura'**
  String get requisitosTitulo;

  /// No description provided for @editarRequisitos.
  ///
  /// In es, this message translates to:
  /// **'Editar requisitos'**
  String get editarRequisitos;

  /// No description provided for @campoNumeroProfesionales.
  ///
  /// In es, this message translates to:
  /// **'Nº de profesionales'**
  String get campoNumeroProfesionales;

  /// No description provided for @cualquierCategoria.
  ///
  /// In es, this message translates to:
  /// **'Cualquier categoría'**
  String get cualquierCategoria;

  /// No description provided for @campoTipoTurno.
  ///
  /// In es, this message translates to:
  /// **'Tipo de turno'**
  String get campoTipoTurno;

  /// No description provided for @sinElementos.
  ///
  /// In es, this message translates to:
  /// **'Sin elementos'**
  String get sinElementos;

  /// No description provided for @validaNumero.
  ///
  /// In es, this message translates to:
  /// **'Número entre 1 y 50'**
  String get validaNumero;

  /// No description provided for @validaJson.
  ///
  /// In es, this message translates to:
  /// **'No es un JSON válido'**
  String get validaJson;

  /// No description provided for @profesionalesTitulo.
  ///
  /// In es, this message translates to:
  /// **'Profesionales'**
  String get profesionalesTitulo;

  /// No description provided for @nuevoProfesional.
  ///
  /// In es, this message translates to:
  /// **'Nuevo profesional'**
  String get nuevoProfesional;

  /// No description provided for @editarProfesional.
  ///
  /// In es, this message translates to:
  /// **'Editar profesional'**
  String get editarProfesional;

  /// No description provided for @campoApellidos.
  ///
  /// In es, this message translates to:
  /// **'Apellidos'**
  String get campoApellidos;

  /// No description provided for @campoTelefono.
  ///
  /// In es, this message translates to:
  /// **'Teléfono'**
  String get campoTelefono;

  /// No description provided for @campoColegiado.
  ///
  /// In es, this message translates to:
  /// **'Nº de colegiado'**
  String get campoColegiado;

  /// No description provided for @campoCategoria.
  ///
  /// In es, this message translates to:
  /// **'Perfil profesional'**
  String get campoCategoria;

  /// No description provided for @campoEspecialidades.
  ///
  /// In es, this message translates to:
  /// **'Especialidades'**
  String get campoEspecialidades;

  /// No description provided for @campoCompetencias.
  ///
  /// In es, this message translates to:
  /// **'Competencias y habilitaciones'**
  String get campoCompetencias;

  /// No description provided for @campoServiciosAutorizados.
  ///
  /// In es, this message translates to:
  /// **'Servicios autorizados'**
  String get campoServiciosAutorizados;

  /// No description provided for @campoUsuarioVinculado.
  ///
  /// In es, this message translates to:
  /// **'Usuario vinculado'**
  String get campoUsuarioVinculado;

  /// No description provided for @sinUsuario.
  ///
  /// In es, this message translates to:
  /// **'Sin usuario'**
  String get sinUsuario;

  /// No description provided for @vigenteHasta.
  ///
  /// In es, this message translates to:
  /// **'Vigente hasta {fecha}'**
  String vigenteHasta(String fecha);

  /// No description provided for @sinCaducidad.
  ///
  /// In es, this message translates to:
  /// **'Sin caducidad'**
  String get sinCaducidad;

  /// No description provided for @fijarCaducidad.
  ///
  /// In es, this message translates to:
  /// **'Fijar caducidad'**
  String get fijarCaducidad;

  /// No description provided for @historial.
  ///
  /// In es, this message translates to:
  /// **'Historial'**
  String get historial;

  /// No description provided for @historialVacio.
  ///
  /// In es, this message translates to:
  /// **'Sin cambios registrados.'**
  String get historialVacio;

  /// No description provided for @cerrar.
  ///
  /// In es, this message translates to:
  /// **'Cerrar'**
  String get cerrar;

  /// No description provided for @datosContacto.
  ///
  /// In es, this message translates to:
  /// **'Datos de contacto'**
  String get datosContacto;

  /// No description provided for @sinContacto.
  ///
  /// In es, this message translates to:
  /// **'Sin datos de contacto'**
  String get sinContacto;

  /// No description provided for @menuPlanificacion.
  ///
  /// In es, this message translates to:
  /// **'Planificación'**
  String get menuPlanificacion;

  /// No description provided for @menuCobertura.
  ///
  /// In es, this message translates to:
  /// **'Cobertura'**
  String get menuCobertura;

  /// No description provided for @cobRango.
  ///
  /// In es, this message translates to:
  /// **'Rango'**
  String get cobRango;

  /// No description provided for @cobElegirRango.
  ///
  /// In es, this message translates to:
  /// **'Elegir intervalo de días'**
  String get cobElegirRango;

  /// No description provided for @cobRangoMaximo.
  ///
  /// In es, this message translates to:
  /// **'El intervalo máximo es de {n} días; se ha ajustado.'**
  String cobRangoMaximo(int n);

  /// No description provided for @cobHospital.
  ///
  /// In es, this message translates to:
  /// **'Hospital'**
  String get cobHospital;

  /// No description provided for @cobProfesional.
  ///
  /// In es, this message translates to:
  /// **'Profesional'**
  String get cobProfesional;

  /// No description provided for @cobTodosHospitales.
  ///
  /// In es, this message translates to:
  /// **'Todos los hospitales'**
  String get cobTodosHospitales;

  /// No description provided for @cobTodosProfesionales.
  ///
  /// In es, this message translates to:
  /// **'Todos los profesionales'**
  String get cobTodosProfesionales;

  /// No description provided for @cobLimpiar.
  ///
  /// In es, this message translates to:
  /// **'Quitar filtros'**
  String get cobLimpiar;

  /// No description provided for @cobBuscarProfesional.
  ///
  /// In es, this message translates to:
  /// **'Escriba al menos 3 letras para buscar'**
  String get cobBuscarProfesional;

  /// No description provided for @cobTurnos.
  ///
  /// In es, this message translates to:
  /// **'Turnos'**
  String get cobTurnos;

  /// No description provided for @cobCobertura.
  ///
  /// In es, this message translates to:
  /// **'Cobertura'**
  String get cobCobertura;

  /// No description provided for @cobPlazasSinCubrir.
  ///
  /// In es, this message translates to:
  /// **'Plazas sin cubrir'**
  String get cobPlazasSinCubrir;

  /// No description provided for @cobProfesionales.
  ///
  /// In es, this message translates to:
  /// **'Profesionales'**
  String get cobProfesionales;

  /// No description provided for @cobPorDia.
  ///
  /// In es, this message translates to:
  /// **'Por día'**
  String get cobPorDia;

  /// No description provided for @cobPorHospital.
  ///
  /// In es, this message translates to:
  /// **'Por hospital'**
  String get cobPorHospital;

  /// No description provided for @cobPorProfesional.
  ///
  /// In es, this message translates to:
  /// **'Por profesional'**
  String get cobPorProfesional;

  /// No description provided for @cobSinResultados.
  ///
  /// In es, this message translates to:
  /// **'No hay turnos con estos filtros'**
  String get cobSinResultados;

  /// No description provided for @cobSinAsignar.
  ///
  /// In es, this message translates to:
  /// **'Sin asignar'**
  String get cobSinAsignar;

  /// No description provided for @cobBorrador.
  ///
  /// In es, this message translates to:
  /// **'Cuadrante en borrador'**
  String get cobBorrador;

  /// No description provided for @cobNTurnos.
  ///
  /// In es, this message translates to:
  /// **'{n, plural, =1{1 turno} other{{n} turnos}}'**
  String cobNTurnos(int n);

  /// No description provided for @cobHoras.
  ///
  /// In es, this message translates to:
  /// **'{h} h'**
  String cobHoras(String h);

  /// No description provided for @menuCuadrantes.
  ///
  /// In es, this message translates to:
  /// **'Cuadrantes'**
  String get menuCuadrantes;

  /// No description provided for @menuMiPlanificacion.
  ///
  /// In es, this message translates to:
  /// **'Mi planificación'**
  String get menuMiPlanificacion;

  /// No description provided for @menuMas.
  ///
  /// In es, this message translates to:
  /// **'Más'**
  String get menuMas;

  /// No description provided for @eliminar.
  ///
  /// In es, this message translates to:
  /// **'Eliminar'**
  String get eliminar;

  /// No description provided for @crear.
  ///
  /// In es, this message translates to:
  /// **'Crear'**
  String get crear;

  /// No description provided for @cerrarMenu.
  ///
  /// In es, this message translates to:
  /// **'Cerrar'**
  String get cerrarMenu;

  /// No description provided for @vistaDia.
  ///
  /// In es, this message translates to:
  /// **'Día'**
  String get vistaDia;

  /// No description provided for @vistaSemana.
  ///
  /// In es, this message translates to:
  /// **'Semana'**
  String get vistaSemana;

  /// No description provided for @vistaMes.
  ///
  /// In es, this message translates to:
  /// **'Mes'**
  String get vistaMes;

  /// No description provided for @vistaAgenda.
  ///
  /// In es, this message translates to:
  /// **'Agenda'**
  String get vistaAgenda;

  /// No description provided for @anterior.
  ///
  /// In es, this message translates to:
  /// **'Anterior'**
  String get anterior;

  /// No description provided for @siguiente.
  ///
  /// In es, this message translates to:
  /// **'Siguiente'**
  String get siguiente;

  /// No description provided for @hoy.
  ///
  /// In es, this message translates to:
  /// **'Hoy'**
  String get hoy;

  /// No description provided for @filtroCentro.
  ///
  /// In es, this message translates to:
  /// **'Centro'**
  String get filtroCentro;

  /// No description provided for @filtroServicio.
  ///
  /// In es, this message translates to:
  /// **'Servicio'**
  String get filtroServicio;

  /// No description provided for @todos.
  ///
  /// In es, this message translates to:
  /// **'Todos'**
  String get todos;

  /// No description provided for @soloSinCubrir.
  ///
  /// In es, this message translates to:
  /// **'Solo sin cubrir'**
  String get soloSinCubrir;

  /// No description provided for @sinTurnos.
  ///
  /// In es, this message translates to:
  /// **'Sin turnos'**
  String get sinTurnos;

  /// No description provided for @resumenDia.
  ///
  /// In es, this message translates to:
  /// **'{turnos} turnos, {sinCubrir} sin cubrir'**
  String resumenDia(int turnos, int sinCubrir);

  /// No description provided for @plazasCubiertasSemantica.
  ///
  /// In es, this message translates to:
  /// **'{cubiertas} de {total} plazas cubiertas'**
  String plazasCubiertasSemantica(int cubiertas, int total);

  /// No description provided for @faltanProfesionales.
  ///
  /// In es, this message translates to:
  /// **'{n, plural, =1{Falta 1 profesional} other{Faltan {n} profesionales}}'**
  String faltanProfesionales(int n);

  /// No description provided for @sobranProfesionales.
  ///
  /// In es, this message translates to:
  /// **'{n, plural, =1{Sobra 1 profesional} other{Sobran {n} profesionales}}'**
  String sobranProfesionales(int n);

  /// No description provided for @dispAprobadaAsignada.
  ///
  /// In es, this message translates to:
  /// **'{n, plural, =1{Disponibilidad aprobada: asignado automáticamente a 1 turno} other{Disponibilidad aprobada: asignado automáticamente a {n} turnos}}'**
  String dispAprobadaAsignada(int n);

  /// No description provided for @cobCualquierCategoria.
  ///
  /// In es, this message translates to:
  /// **'Cualquier categoría'**
  String get cobCualquierCategoria;

  /// No description provided for @dispAprobadaSinTurnos.
  ///
  /// In es, this message translates to:
  /// **'Disponibilidad aprobada. Se asignará sola cuando existan los turnos de esos días.'**
  String get dispAprobadaSinTurnos;

  /// No description provided for @cuadranteSinTurnos.
  ///
  /// In es, this message translates to:
  /// **'Cuadrante creado sin turnos: defina los requisitos de cobertura del servicio (médicos, enfermería…) para poder generarlos.'**
  String get cuadranteSinTurnos;

  /// No description provided for @cuadranteCreadoResumen.
  ///
  /// In es, this message translates to:
  /// **'Cuadrante creado: {turnos} turnos, {cubiertas} plazas ya cubiertas con la disponibilidad aprobada'**
  String cuadranteCreadoResumen(int turnos, int cubiertas);

  /// No description provided for @dashSemaforoTitulo.
  ///
  /// In es, this message translates to:
  /// **'Cobertura por turno'**
  String get dashSemaforoTitulo;

  /// No description provided for @dashSemaforoVerde.
  ///
  /// In es, this message translates to:
  /// **'En su tope'**
  String get dashSemaforoVerde;

  /// No description provided for @dashSemaforoAmarillo.
  ///
  /// In es, this message translates to:
  /// **'Faltan profesionales'**
  String get dashSemaforoAmarillo;

  /// No description provided for @dashSemaforoRojo.
  ///
  /// In es, this message translates to:
  /// **'Pasados del tope'**
  String get dashSemaforoRojo;

  /// No description provided for @dashSemaforoAtencion.
  ///
  /// In es, this message translates to:
  /// **'Turnos que requieren atención'**
  String get dashSemaforoAtencion;

  /// No description provided for @cuadranteNoEditable.
  ///
  /// In es, this message translates to:
  /// **'El cuadrante no está en borrador: reábralo para modificarlo.'**
  String get cuadranteNoEditable;

  /// No description provided for @asignarProfesional.
  ///
  /// In es, this message translates to:
  /// **'{n, plural, =1{Asignar profesional (1 plaza libre)} other{Asignar profesional ({n} plazas libres)}}'**
  String asignarProfesional(int n);

  /// No description provided for @plazasLibres.
  ///
  /// In es, this message translates to:
  /// **'{n, plural, =1{1 plaza libre} other{{n} plazas libres}}'**
  String plazasLibres(int n);

  /// No description provided for @sinNecesidades.
  ///
  /// In es, this message translates to:
  /// **'Este turno no tiene necesidades de cobertura definidas.'**
  String get sinNecesidades;

  /// No description provided for @eliminarTurno.
  ///
  /// In es, this message translates to:
  /// **'Eliminar turno'**
  String get eliminarTurno;

  /// No description provided for @confirmaEliminarTurno.
  ///
  /// In es, this message translates to:
  /// **'Se eliminará el turno con sus necesidades y asignaciones.'**
  String get confirmaEliminarTurno;

  /// No description provided for @candidatosTitulo.
  ///
  /// In es, this message translates to:
  /// **'Candidatos'**
  String get candidatosTitulo;

  /// No description provided for @asignar.
  ///
  /// In es, this message translates to:
  /// **'Asignar'**
  String get asignar;

  /// No description provided for @estadoBorrador.
  ///
  /// In es, this message translates to:
  /// **'Borrador'**
  String get estadoBorrador;

  /// No description provided for @estadoPublicado.
  ///
  /// In es, this message translates to:
  /// **'Publicado'**
  String get estadoPublicado;

  /// No description provided for @estadoArchivado.
  ///
  /// In es, this message translates to:
  /// **'Archivado'**
  String get estadoArchivado;

  /// No description provided for @nuevoCuadrante.
  ///
  /// In es, this message translates to:
  /// **'Nuevo cuadrante'**
  String get nuevoCuadrante;

  /// No description provided for @desdePlantilla.
  ///
  /// In es, this message translates to:
  /// **'Desde plantilla'**
  String get desdePlantilla;

  /// No description provided for @plazasCubiertasDe.
  ///
  /// In es, this message translates to:
  /// **'{cubiertas}/{total} plazas cubiertas'**
  String plazasCubiertasDe(int cubiertas, int total);

  /// No description provided for @nombreOpcional.
  ///
  /// In es, this message translates to:
  /// **'Nombre (opcional)'**
  String get nombreOpcional;

  /// No description provided for @campoDesde.
  ///
  /// In es, this message translates to:
  /// **'Desde'**
  String get campoDesde;

  /// No description provided for @campoHasta.
  ///
  /// In es, this message translates to:
  /// **'Hasta'**
  String get campoHasta;

  /// No description provided for @generarTurnos.
  ///
  /// In es, this message translates to:
  /// **'Generar turnos con los requisitos del servicio'**
  String get generarTurnos;

  /// No description provided for @plantilla.
  ///
  /// In es, this message translates to:
  /// **'Plantilla'**
  String get plantilla;

  /// No description provided for @sinPlantillas.
  ///
  /// In es, this message translates to:
  /// **'Todavía no hay plantillas. Guarde un cuadrante como plantilla para reutilizarlo.'**
  String get sinPlantillas;

  /// No description provided for @diasPlantilla.
  ///
  /// In es, this message translates to:
  /// **'{n} días'**
  String diasPlantilla(int n);

  /// No description provided for @turnosCuenta.
  ///
  /// In es, this message translates to:
  /// **'{n, plural, =1{1 turno} other{{n} turnos}}'**
  String turnosCuenta(int n);

  /// No description provided for @versionPublicada.
  ///
  /// In es, this message translates to:
  /// **'Versión publicada: {v}'**
  String versionPublicada(int v);

  /// No description provided for @verCalendario.
  ///
  /// In es, this message translates to:
  /// **'Ver calendario'**
  String get verCalendario;

  /// No description provided for @validarYPublicar.
  ///
  /// In es, this message translates to:
  /// **'Validar y publicar'**
  String get validarYPublicar;

  /// No description provided for @publicar.
  ///
  /// In es, this message translates to:
  /// **'Publicar'**
  String get publicar;

  /// No description provided for @cuadrantePublicado.
  ///
  /// In es, this message translates to:
  /// **'Cuadrante publicado'**
  String get cuadrantePublicado;

  /// No description provided for @anadirTurno.
  ///
  /// In es, this message translates to:
  /// **'Añadir turno'**
  String get anadirTurno;

  /// No description provided for @eliminarCuadrante.
  ///
  /// In es, this message translates to:
  /// **'Eliminar cuadrante'**
  String get eliminarCuadrante;

  /// No description provided for @confirmaEliminarCuadrante.
  ///
  /// In es, this message translates to:
  /// **'Se eliminará el borrador con todos sus turnos y asignaciones.'**
  String get confirmaEliminarCuadrante;

  /// No description provided for @reabrir.
  ///
  /// In es, this message translates to:
  /// **'Reabrir'**
  String get reabrir;

  /// No description provided for @archivar.
  ///
  /// In es, this message translates to:
  /// **'Archivar'**
  String get archivar;

  /// No description provided for @guardarPlantilla.
  ///
  /// In es, this message translates to:
  /// **'Guardar como plantilla'**
  String get guardarPlantilla;

  /// No description provided for @plantillaGuardada.
  ///
  /// In es, this message translates to:
  /// **'Plantilla guardada'**
  String get plantillaGuardada;

  /// No description provided for @exportarCsv.
  ///
  /// In es, this message translates to:
  /// **'Exportar CSV'**
  String get exportarCsv;

  /// No description provided for @csvCopiado.
  ///
  /// In es, this message translates to:
  /// **'Este dispositivo no permite descargar archivos: el CSV se ha copiado al portapapeles.'**
  String get csvCopiado;

  /// No description provided for @versiones.
  ///
  /// In es, this message translates to:
  /// **'Versiones publicadas'**
  String get versiones;

  /// No description provided for @sinVersiones.
  ///
  /// In es, this message translates to:
  /// **'Todavía no se ha publicado.'**
  String get sinVersiones;

  /// No description provided for @versionTitulo.
  ///
  /// In es, this message translates to:
  /// **'Versión {n}'**
  String versionTitulo(int n);

  /// No description provided for @sinIncidencias.
  ///
  /// In es, this message translates to:
  /// **'No hay incidencias: el cuadrante está listo para publicarse.'**
  String get sinIncidencias;

  /// No description provided for @errores.
  ///
  /// In es, this message translates to:
  /// **'Errores (impiden publicar)'**
  String get errores;

  /// No description provided for @advertencias.
  ///
  /// In es, this message translates to:
  /// **'Advertencias (revisables)'**
  String get advertencias;

  /// No description provided for @yMasAvisos.
  ///
  /// In es, this message translates to:
  /// **'… y {n} avisos más'**
  String yMasAvisos(int n);

  /// No description provided for @menuReglas.
  ///
  /// In es, this message translates to:
  /// **'Reglas'**
  String get menuReglas;

  /// No description provided for @reglasTabReglas.
  ///
  /// In es, this message translates to:
  /// **'Reglas'**
  String get reglasTabReglas;

  /// No description provided for @reglasTabExcepciones.
  ///
  /// In es, this message translates to:
  /// **'Excepciones'**
  String get reglasTabExcepciones;

  /// No description provided for @nuevaRegla.
  ///
  /// In es, this message translates to:
  /// **'Nueva regla'**
  String get nuevaRegla;

  /// No description provided for @editarRegla.
  ///
  /// In es, this message translates to:
  /// **'Editar regla'**
  String get editarRegla;

  /// No description provided for @campoTipoRegla.
  ///
  /// In es, this message translates to:
  /// **'Tipo de regla'**
  String get campoTipoRegla;

  /// No description provided for @campoAmbito.
  ///
  /// In es, this message translates to:
  /// **'Ámbito'**
  String get campoAmbito;

  /// No description provided for @ambitoSociedad.
  ///
  /// In es, this message translates to:
  /// **'Toda la sociedad'**
  String get ambitoSociedad;

  /// No description provided for @severidadBloqueante.
  ///
  /// In es, this message translates to:
  /// **'Bloqueante'**
  String get severidadBloqueante;

  /// No description provided for @severidadAdvertencia.
  ///
  /// In es, this message translates to:
  /// **'Advertencia'**
  String get severidadAdvertencia;

  /// No description provided for @campoPermiteExcepcion.
  ///
  /// In es, this message translates to:
  /// **'Permitir excepción justificada'**
  String get campoPermiteExcepcion;

  /// No description provided for @campoExcepcionAdmin.
  ///
  /// In es, this message translates to:
  /// **'Solo el administrador puede autorizar la excepción'**
  String get campoExcepcionAdmin;

  /// No description provided for @campoFundamento.
  ///
  /// In es, this message translates to:
  /// **'Fundamento (convenio, acuerdo o norma)'**
  String get campoFundamento;

  /// No description provided for @fundamentoAyuda.
  ///
  /// In es, this message translates to:
  /// **'Lo completa la sociedad: el sistema no incluye límites legales por defecto'**
  String get fundamentoAyuda;

  /// No description provided for @pendienteRevision.
  ///
  /// In es, this message translates to:
  /// **'Pendiente de revisión'**
  String get pendienteRevision;

  /// No description provided for @revisadaPor.
  ///
  /// In es, this message translates to:
  /// **'Revisada por {nombre} el {fecha}'**
  String revisadaPor(String nombre, String fecha);

  /// No description provided for @marcarRevisada.
  ///
  /// In es, this message translates to:
  /// **'Marcar como revisada'**
  String get marcarRevisada;

  /// No description provided for @reglasAviso.
  ///
  /// In es, this message translates to:
  /// **'Las reglas las define y revisa su sociedad. Los límites (descansos, horas, guardias…) no existen hasta que los configure según su convenio o acuerdo; consulte la normativa aplicable a cada colectivo.'**
  String get reglasAviso;

  /// No description provided for @reglasVacio.
  ///
  /// In es, this message translates to:
  /// **'Todavía no ha configurado ninguna regla.'**
  String get reglasVacio;

  /// No description provided for @excepcionSoloAdmin.
  ///
  /// In es, this message translates to:
  /// **'excepción solo del administrador'**
  String get excepcionSoloAdmin;

  /// No description provided for @excepcionPosible.
  ///
  /// In es, this message translates to:
  /// **'admite excepción'**
  String get excepcionPosible;

  /// No description provided for @comportamientoPorDefecto.
  ///
  /// In es, this message translates to:
  /// **'Comportamiento cuando no hay regla configurada'**
  String get comportamientoPorDefecto;

  /// No description provided for @sinExcepciones.
  ///
  /// In es, this message translates to:
  /// **'No hay excepciones registradas.'**
  String get sinExcepciones;

  /// No description provided for @excepcionAnulada.
  ///
  /// In es, this message translates to:
  /// **'Asignación anulada'**
  String get excepcionAnulada;

  /// No description provided for @validaRango.
  ///
  /// In es, this message translates to:
  /// **'Número entre {min} y {max}'**
  String validaRango(int min, int max);

  /// No description provided for @conExcepcion.
  ///
  /// In es, this message translates to:
  /// **'Con excepción'**
  String get conExcepcion;

  /// No description provided for @conExcepcionTitulo.
  ///
  /// In es, this message translates to:
  /// **'Asignar a {nombre} con excepción'**
  String conExcepcionTitulo(String nombre);

  /// No description provided for @soloAdministrador.
  ///
  /// In es, this message translates to:
  /// **'Requiere al administrador'**
  String get soloAdministrador;

  /// No description provided for @asignadoConAdvertencias.
  ///
  /// In es, this message translates to:
  /// **'{n, plural, =1{Asignado con 1 advertencia} other{Asignado con {n} advertencias}}'**
  String asignadoConAdvertencias(int n);

  /// No description provided for @asignadoConExcepcion.
  ///
  /// In es, this message translates to:
  /// **'Asignado con excepción registrada'**
  String get asignadoConExcepcion;

  /// No description provided for @excepcionAviso.
  ///
  /// In es, this message translates to:
  /// **'Esta asignación incumple las siguientes reglas. Puede autorizarla como excepción: quedará registrada con su justificación y su nombre.'**
  String get excepcionAviso;

  /// No description provided for @justificacion.
  ///
  /// In es, this message translates to:
  /// **'Justificación'**
  String get justificacion;

  /// No description provided for @justificacionMinimo.
  ///
  /// In es, this message translates to:
  /// **'Explique el motivo (mínimo {n} caracteres)'**
  String justificacionMinimo(int n);

  /// No description provided for @autorizarExcepcion.
  ///
  /// In es, this message translates to:
  /// **'Autorizar excepción'**
  String get autorizarExcepcion;

  /// No description provided for @excepcionesAutorizadas.
  ///
  /// In es, this message translates to:
  /// **'Excepciones autorizadas'**
  String get excepcionesAutorizadas;

  /// No description provided for @autorizadaPor.
  ///
  /// In es, this message translates to:
  /// **'Autorizada por {nombre}'**
  String autorizadaPor(String nombre);

  /// No description provided for @ausenciaVacaciones.
  ///
  /// In es, this message translates to:
  /// **'Vacaciones'**
  String get ausenciaVacaciones;

  /// No description provided for @ausenciaAsuntosPropios.
  ///
  /// In es, this message translates to:
  /// **'Asuntos propios'**
  String get ausenciaAsuntosPropios;

  /// No description provided for @ausenciaPermiso.
  ///
  /// In es, this message translates to:
  /// **'Permiso'**
  String get ausenciaPermiso;

  /// No description provided for @ausenciaFormacion.
  ///
  /// In es, this message translates to:
  /// **'Formación'**
  String get ausenciaFormacion;

  /// No description provided for @ausenciaBajaTemporal.
  ///
  /// In es, this message translates to:
  /// **'Baja temporal'**
  String get ausenciaBajaTemporal;

  /// No description provided for @ausenciaOtro.
  ///
  /// In es, this message translates to:
  /// **'Otro'**
  String get ausenciaOtro;

  /// No description provided for @ausenciaSolicitada.
  ///
  /// In es, this message translates to:
  /// **'Solicitada'**
  String get ausenciaSolicitada;

  /// No description provided for @ausenciaAprobada.
  ///
  /// In es, this message translates to:
  /// **'Aprobada'**
  String get ausenciaAprobada;

  /// No description provided for @ausenciaRechazada.
  ///
  /// In es, this message translates to:
  /// **'Rechazada'**
  String get ausenciaRechazada;

  /// No description provided for @ausenciaCancelada.
  ///
  /// In es, this message translates to:
  /// **'Cancelada'**
  String get ausenciaCancelada;

  /// No description provided for @dispDisponible.
  ///
  /// In es, this message translates to:
  /// **'Disponible'**
  String get dispDisponible;

  /// No description provided for @dispBloqueo.
  ///
  /// In es, this message translates to:
  /// **'No disponible (bloqueo)'**
  String get dispBloqueo;

  /// No description provided for @dispPrefiere.
  ///
  /// In es, this message translates to:
  /// **'Prefiere'**
  String get dispPrefiere;

  /// No description provided for @dispEvita.
  ///
  /// In es, this message translates to:
  /// **'Prefiere evitar'**
  String get dispEvita;

  /// No description provided for @dispPendiente.
  ///
  /// In es, this message translates to:
  /// **'Pendiente de aprobación'**
  String get dispPendiente;

  /// No description provided for @todosLosDias.
  ///
  /// In es, this message translates to:
  /// **'Todos los días'**
  String get todosLosDias;

  /// No description provided for @registrarAusencia.
  ///
  /// In es, this message translates to:
  /// **'Registrar ausencia'**
  String get registrarAusencia;

  /// No description provided for @solicitarAusencia.
  ///
  /// In es, this message translates to:
  /// **'Solicitar ausencia'**
  String get solicitarAusencia;

  /// No description provided for @campoTipoAusencia.
  ///
  /// In es, this message translates to:
  /// **'Tipo de ausencia'**
  String get campoTipoAusencia;

  /// No description provided for @campoProfesional.
  ///
  /// In es, this message translates to:
  /// **'Profesional'**
  String get campoProfesional;

  /// No description provided for @campoComentarioOpcional.
  ///
  /// In es, this message translates to:
  /// **'Comentario (opcional)'**
  String get campoComentarioOpcional;

  /// No description provided for @avisoSinDatosSalud.
  ///
  /// In es, this message translates to:
  /// **'No incluya datos de salud ni diagnósticos.'**
  String get avisoSinDatosSalud;

  /// No description provided for @aprobarAlRegistrar.
  ///
  /// In es, this message translates to:
  /// **'Aprobar al registrar'**
  String get aprobarAlRegistrar;

  /// No description provided for @registrar.
  ///
  /// In es, this message translates to:
  /// **'Registrar'**
  String get registrar;

  /// No description provided for @solicitar.
  ///
  /// In es, this message translates to:
  /// **'Solicitar'**
  String get solicitar;

  /// No description provided for @declararDisponibilidad.
  ///
  /// In es, this message translates to:
  /// **'Declarar disponibilidad'**
  String get declararDisponibilidad;

  /// No description provided for @campoTipoDisponibilidad.
  ///
  /// In es, this message translates to:
  /// **'Tipo'**
  String get campoTipoDisponibilidad;

  /// No description provided for @campoDiasSemana.
  ///
  /// In es, this message translates to:
  /// **'Días de la semana'**
  String get campoDiasSemana;

  /// No description provided for @campoTipoTurnoOpcional.
  ///
  /// In es, this message translates to:
  /// **'Tipo de turno (opcional)'**
  String get campoTipoTurnoOpcional;

  /// No description provided for @cualquierTurno.
  ///
  /// In es, this message translates to:
  /// **'Cualquier turno'**
  String get cualquierTurno;

  /// No description provided for @campoHospital.
  ///
  /// In es, this message translates to:
  /// **'Hospital'**
  String get campoHospital;

  /// No description provided for @cualquierHospital.
  ///
  /// In es, this message translates to:
  /// **'Cualquier hospital'**
  String get cualquierHospital;

  /// No description provided for @campoTurnoDia.
  ///
  /// In es, this message translates to:
  /// **'Turno en esos días'**
  String get campoTurnoDia;

  /// No description provided for @seccionPeriodo.
  ///
  /// In es, this message translates to:
  /// **'Periodo'**
  String get seccionPeriodo;

  /// No description provided for @seccionDiasTurno.
  ///
  /// In es, this message translates to:
  /// **'Días y turno'**
  String get seccionDiasTurno;

  /// No description provided for @diasLaborables.
  ///
  /// In es, this message translates to:
  /// **'Lun–Vie'**
  String get diasLaborables;

  /// No description provided for @diasFinSemana.
  ///
  /// In es, this message translates to:
  /// **'Fin de semana'**
  String get diasFinSemana;

  /// No description provided for @diasTodosAtajo.
  ///
  /// In es, this message translates to:
  /// **'Todos'**
  String get diasTodosAtajo;

  /// No description provided for @seccionHospitales.
  ///
  /// In es, this message translates to:
  /// **'Hospitales, días y turnos'**
  String get seccionHospitales;

  /// No description provided for @anadirHospital.
  ///
  /// In es, this message translates to:
  /// **'Añadir otro hospital'**
  String get anadirHospital;

  /// No description provided for @hospitalYaElegido.
  ///
  /// In es, this message translates to:
  /// **'No se puede crear esa disponibilidad porque ese hospital ya se ha elegido en esta selección.'**
  String get hospitalYaElegido;

  /// No description provided for @anadirFranja.
  ///
  /// In es, this message translates to:
  /// **'Añadir otros días y turnos'**
  String get anadirFranja;

  /// No description provided for @quitarBloque.
  ///
  /// In es, this message translates to:
  /// **'Quitar'**
  String get quitarBloque;

  /// No description provided for @todosLosTurnos.
  ///
  /// In es, this message translates to:
  /// **'Todos los turnos'**
  String get todosLosTurnos;

  /// No description provided for @turnosEnLosDias.
  ///
  /// In es, this message translates to:
  /// **'Turnos en esos días'**
  String get turnosEnLosDias;

  /// No description provided for @sinTurnosPropios.
  ///
  /// In es, this message translates to:
  /// **'No tiene servicios autorizados con turnos definidos: la declaración se aplicará a cualquier hospital.'**
  String get sinTurnosPropios;

  /// No description provided for @avisoAusenciaSolicitada.
  ///
  /// In es, this message translates to:
  /// **'Ausencia solicitada'**
  String get avisoAusenciaSolicitada;

  /// No description provided for @avisoAusenciaRegistrada.
  ///
  /// In es, this message translates to:
  /// **'Ausencia registrada'**
  String get avisoAusenciaRegistrada;

  /// No description provided for @resumenTitulo.
  ///
  /// In es, this message translates to:
  /// **'Resumen'**
  String get resumenTitulo;

  /// No description provided for @disponibilidadDeclarada.
  ///
  /// In es, this message translates to:
  /// **'Disponibilidad declarada'**
  String get disponibilidadDeclarada;

  /// No description provided for @avisoDisponibilidad.
  ///
  /// In es, this message translates to:
  /// **'Las declaraciones solo cuentan para la planificación cuando las aprueba la coordinación.'**
  String get avisoDisponibilidad;

  /// No description provided for @declarar.
  ///
  /// In es, this message translates to:
  /// **'Declarar'**
  String get declarar;

  /// No description provided for @menuMiDisponibilidad.
  ///
  /// In es, this message translates to:
  /// **'Mi disponibilidad'**
  String get menuMiDisponibilidad;

  /// No description provided for @menuAusencias.
  ///
  /// In es, this message translates to:
  /// **'Ausencias'**
  String get menuAusencias;

  /// No description provided for @misAusencias.
  ///
  /// In es, this message translates to:
  /// **'Mis ausencias'**
  String get misAusencias;

  /// No description provided for @misDeclaraciones.
  ///
  /// In es, this message translates to:
  /// **'Mis declaraciones'**
  String get misDeclaraciones;

  /// No description provided for @sinAusencias.
  ///
  /// In es, this message translates to:
  /// **'No hay ausencias.'**
  String get sinAusencias;

  /// No description provided for @sinDeclaraciones.
  ///
  /// In es, this message translates to:
  /// **'No hay declaraciones.'**
  String get sinDeclaraciones;

  /// No description provided for @sinVacantes.
  ///
  /// In es, this message translates to:
  /// **'No hay vacantes.'**
  String get sinVacantes;

  /// No description provided for @ausTabSolicitudes.
  ///
  /// In es, this message translates to:
  /// **'Ausencias'**
  String get ausTabSolicitudes;

  /// No description provided for @ausTabVacantes.
  ///
  /// In es, this message translates to:
  /// **'Vacantes'**
  String get ausTabVacantes;

  /// No description provided for @ausTabDisponibilidad.
  ///
  /// In es, this message translates to:
  /// **'Disponibilidad'**
  String get ausTabDisponibilidad;

  /// No description provided for @accionAprobar.
  ///
  /// In es, this message translates to:
  /// **'Aprobar'**
  String get accionAprobar;

  /// No description provided for @accionRechazar.
  ///
  /// In es, this message translates to:
  /// **'Rechazar'**
  String get accionRechazar;

  /// No description provided for @accionCancelarAusencia.
  ///
  /// In es, this message translates to:
  /// **'Cancelar'**
  String get accionCancelarAusencia;

  /// No description provided for @accionRetirar.
  ///
  /// In es, this message translates to:
  /// **'Retirar'**
  String get accionRetirar;

  /// No description provided for @accionCubrir.
  ///
  /// In es, this message translates to:
  /// **'Buscar sustituto'**
  String get accionCubrir;

  /// No description provided for @motivoObligatorio.
  ///
  /// In es, this message translates to:
  /// **'Motivo (obligatorio)'**
  String get motivoObligatorio;

  /// No description provided for @motivoOpcional.
  ///
  /// In es, this message translates to:
  /// **'Motivo (opcional)'**
  String get motivoOpcional;

  /// No description provided for @turnosAfectados.
  ///
  /// In es, this message translates to:
  /// **'Turnos afectados: {n}'**
  String turnosAfectados(int n);

  /// No description provided for @vacantesCreadas.
  ///
  /// In es, this message translates to:
  /// **'Ausencia aprobada. Vacantes creadas: {n}'**
  String vacantesCreadas(int n);

  /// No description provided for @vacanteCubierta.
  ///
  /// In es, this message translates to:
  /// **'Vacante cubierta'**
  String get vacanteCubierta;

  /// No description provided for @vacanteAusente.
  ///
  /// In es, this message translates to:
  /// **'Ausente: {nombre}'**
  String vacanteAusente(String nombre);

  /// No description provided for @estadoVacanteAbierta.
  ///
  /// In es, this message translates to:
  /// **'Abierta'**
  String get estadoVacanteAbierta;

  /// No description provided for @estadoVacanteCubierta.
  ///
  /// In es, this message translates to:
  /// **'Cubierta'**
  String get estadoVacanteCubierta;

  /// No description provided for @estadoVacanteCancelada.
  ///
  /// In es, this message translates to:
  /// **'Cancelada'**
  String get estadoVacanteCancelada;

  /// No description provided for @diasNumero.
  ///
  /// In es, this message translates to:
  /// **'{n} días'**
  String diasNumero(int n);

  /// No description provided for @filtroTodas.
  ///
  /// In es, this message translates to:
  /// **'Todas'**
  String get filtroTodas;

  /// No description provided for @ofertasTitulo.
  ///
  /// In es, this message translates to:
  /// **'Ofertas de sustitución'**
  String get ofertasTitulo;

  /// No description provided for @sinOfertas.
  ///
  /// In es, this message translates to:
  /// **'No hay ofertas de sustitución.'**
  String get sinOfertas;

  /// No description provided for @ofertaAceptar.
  ///
  /// In es, this message translates to:
  /// **'Aceptar'**
  String get ofertaAceptar;

  /// No description provided for @ofertaRechazar.
  ///
  /// In es, this message translates to:
  /// **'Rechazar'**
  String get ofertaRechazar;

  /// No description provided for @ofertaEstadoPendiente.
  ///
  /// In es, this message translates to:
  /// **'Pendiente de respuesta'**
  String get ofertaEstadoPendiente;

  /// No description provided for @ofertaEstadoAceptada.
  ///
  /// In es, this message translates to:
  /// **'Aceptada, pendiente de aprobación'**
  String get ofertaEstadoAceptada;

  /// No description provided for @ofertaEstadoRechazada.
  ///
  /// In es, this message translates to:
  /// **'Rechazada'**
  String get ofertaEstadoRechazada;

  /// No description provided for @ofertaEstadoAprobada.
  ///
  /// In es, this message translates to:
  /// **'Aprobada'**
  String get ofertaEstadoAprobada;

  /// No description provided for @ofertaEstadoDenegada.
  ///
  /// In es, this message translates to:
  /// **'Denegada'**
  String get ofertaEstadoDenegada;

  /// No description provided for @ofertaEstadoRetirada.
  ///
  /// In es, this message translates to:
  /// **'Retirada'**
  String get ofertaEstadoRetirada;

  /// No description provided for @ofertaEstadoCaducada.
  ///
  /// In es, this message translates to:
  /// **'Caducada'**
  String get ofertaEstadoCaducada;

  /// No description provided for @ofertaEstadoCerrada.
  ///
  /// In es, this message translates to:
  /// **'Cerrada'**
  String get ofertaEstadoCerrada;

  /// No description provided for @ofrecerTurno.
  ///
  /// In es, this message translates to:
  /// **'Ofrecer turno'**
  String get ofrecerTurno;

  /// No description provided for @ofrecerTitulo.
  ///
  /// In es, this message translates to:
  /// **'Ofrecer la vacante'**
  String get ofrecerTitulo;

  /// No description provided for @ofrecerPlazo.
  ///
  /// In es, this message translates to:
  /// **'Plazo para responder, en horas (opcional)'**
  String get ofrecerPlazo;

  /// No description provided for @ofrecerEnviar.
  ///
  /// In es, this message translates to:
  /// **'Enviar oferta'**
  String get ofrecerEnviar;

  /// No description provided for @ofrecerSeleccion.
  ///
  /// In es, this message translates to:
  /// **'Elija a quién ofrecérsela (máximo 10).'**
  String get ofrecerSeleccion;

  /// No description provided for @accionDenegar.
  ///
  /// In es, this message translates to:
  /// **'Denegar'**
  String get accionDenegar;

  /// No description provided for @accionRetirarOferta.
  ///
  /// In es, this message translates to:
  /// **'Retirar oferta'**
  String get accionRetirarOferta;

  /// No description provided for @accionAprobarSustitucion.
  ///
  /// In es, this message translates to:
  /// **'Aprobar sustitución'**
  String get accionAprobarSustitucion;

  /// No description provided for @ofertasEnviadas.
  ///
  /// In es, this message translates to:
  /// **'Ofertas enviadas'**
  String get ofertasEnviadas;

  /// No description provided for @ofertaCaduca.
  ///
  /// In es, this message translates to:
  /// **'Caduca el {fecha}'**
  String ofertaCaduca(String fecha);

  /// No description provided for @ofertaOfrecidaPor.
  ///
  /// In es, this message translates to:
  /// **'Ofrecida por {nombre}'**
  String ofertaOfrecidaPor(String nombre);

  /// No description provided for @sustitucionAprobada.
  ///
  /// In es, this message translates to:
  /// **'Sustitución aprobada'**
  String get sustitucionAprobada;

  /// No description provided for @notificaciones.
  ///
  /// In es, this message translates to:
  /// **'Notificaciones'**
  String get notificaciones;

  /// No description provided for @sinNotificaciones.
  ///
  /// In es, this message translates to:
  /// **'No tiene notificaciones.'**
  String get sinNotificaciones;

  /// No description provided for @marcarTodasLeidas.
  ///
  /// In es, this message translates to:
  /// **'Marcar todas como leídas'**
  String get marcarTodasLeidas;

  /// No description provided for @menuInformes.
  ///
  /// In es, this message translates to:
  /// **'Informes'**
  String get menuInformes;

  /// No description provided for @informesTabHoras.
  ///
  /// In es, this message translates to:
  /// **'Horas'**
  String get informesTabHoras;

  /// No description provided for @informesTabRegistro.
  ///
  /// In es, this message translates to:
  /// **'Registro de horas'**
  String get informesTabRegistro;

  /// No description provided for @informesTabCobertura.
  ///
  /// In es, this message translates to:
  /// **'Cobertura'**
  String get informesTabCobertura;

  /// No description provided for @colPlanificadas.
  ///
  /// In es, this message translates to:
  /// **'Planificadas'**
  String get colPlanificadas;

  /// No description provided for @colNocturnas.
  ///
  /// In es, this message translates to:
  /// **'Nocturnas'**
  String get colNocturnas;

  /// No description provided for @colRegistradas.
  ///
  /// In es, this message translates to:
  /// **'Registradas'**
  String get colRegistradas;

  /// No description provided for @colAjustes.
  ///
  /// In es, this message translates to:
  /// **'Ajustes'**
  String get colAjustes;

  /// No description provided for @colEfectivas.
  ///
  /// In es, this message translates to:
  /// **'Efectivas'**
  String get colEfectivas;

  /// No description provided for @colTurnos.
  ///
  /// In es, this message translates to:
  /// **'Turnos'**
  String get colTurnos;

  /// No description provided for @colGuardias.
  ///
  /// In es, this message translates to:
  /// **'Guardias'**
  String get colGuardias;

  /// No description provided for @colTotal.
  ///
  /// In es, this message translates to:
  /// **'Total'**
  String get colTotal;

  /// No description provided for @colPlazas.
  ///
  /// In es, this message translates to:
  /// **'Plazas'**
  String get colPlazas;

  /// No description provided for @colCubiertas.
  ///
  /// In es, this message translates to:
  /// **'Cubiertas'**
  String get colCubiertas;

  /// No description provided for @colLibres.
  ///
  /// In es, this message translates to:
  /// **'Libres'**
  String get colLibres;

  /// No description provided for @colPorcentaje.
  ///
  /// In es, this message translates to:
  /// **'% cobertura'**
  String get colPorcentaje;

  /// No description provided for @colVacantes.
  ///
  /// In es, this message translates to:
  /// **'Vacantes'**
  String get colVacantes;

  /// No description provided for @colExcepciones.
  ///
  /// In es, this message translates to:
  /// **'Excepciones'**
  String get colExcepciones;

  /// No description provided for @registrarHoras.
  ///
  /// In es, this message translates to:
  /// **'Registrar horas'**
  String get registrarHoras;

  /// No description provided for @ajustarHoras.
  ///
  /// In es, this message translates to:
  /// **'Ajustar'**
  String get ajustarHoras;

  /// No description provided for @horaInicioReal.
  ///
  /// In es, this message translates to:
  /// **'Inicio real'**
  String get horaInicioReal;

  /// No description provided for @horaFinReal.
  ///
  /// In es, this message translates to:
  /// **'Fin real'**
  String get horaFinReal;

  /// No description provided for @ajusteMinutos.
  ///
  /// In es, this message translates to:
  /// **'Minutos (+/−)'**
  String get ajusteMinutos;

  /// No description provided for @ajusteMotivo.
  ///
  /// In es, this message translates to:
  /// **'Motivo (mínimo 10 caracteres)'**
  String get ajusteMotivo;

  /// No description provided for @sinRegistroHoras.
  ///
  /// In es, this message translates to:
  /// **'Sin registrar'**
  String get sinRegistroHoras;

  /// No description provided for @horasGuardadas.
  ///
  /// In es, this message translates to:
  /// **'Horas guardadas'**
  String get horasGuardadas;

  /// No description provided for @ajusteGuardado.
  ///
  /// In es, this message translates to:
  /// **'Ajuste guardado'**
  String get ajusteGuardado;

  /// No description provided for @ajusteInvalido.
  ///
  /// In es, this message translates to:
  /// **'Indique minutos distintos de cero y un motivo de al menos 10 caracteres.'**
  String get ajusteInvalido;

  /// No description provided for @filtroTodos.
  ///
  /// In es, this message translates to:
  /// **'Todos'**
  String get filtroTodos;

  /// No description provided for @sinDatosPeriodo.
  ///
  /// In es, this message translates to:
  /// **'No hay datos en este periodo.'**
  String get sinDatosPeriodo;

  /// No description provided for @dashCoberturaTitulo.
  ///
  /// In es, this message translates to:
  /// **'Cobertura de los próximos 14 días'**
  String get dashCoberturaTitulo;

  /// No description provided for @dashPlazasCubiertas.
  ///
  /// In es, this message translates to:
  /// **'{c} de {t} plazas cubiertas'**
  String dashPlazasCubiertas(int c, int t);

  /// No description provided for @dashPendientes.
  ///
  /// In es, this message translates to:
  /// **'Pendientes'**
  String get dashPendientes;

  /// No description provided for @dashVacantes.
  ///
  /// In es, this message translates to:
  /// **'Vacantes abiertas'**
  String get dashVacantes;

  /// No description provided for @dashAusencias.
  ///
  /// In es, this message translates to:
  /// **'Ausencias por decidir'**
  String get dashAusencias;

  /// No description provided for @dashOfertas.
  ///
  /// In es, this message translates to:
  /// **'Sustituciones por aprobar'**
  String get dashOfertas;

  /// No description provided for @dashDisponibilidades.
  ///
  /// In es, this message translates to:
  /// **'Disponibilidades por aprobar'**
  String get dashDisponibilidades;

  /// No description provided for @dashExcepciones.
  ///
  /// In es, this message translates to:
  /// **'Excepciones (30 días)'**
  String get dashExcepciones;

  /// No description provided for @dashBorradores.
  ///
  /// In es, this message translates to:
  /// **'Cuadrantes en borrador'**
  String get dashBorradores;

  /// No description provided for @dashSinCubrir.
  ///
  /// In es, this message translates to:
  /// **'Próximos turnos sin cubrir'**
  String get dashSinCubrir;

  /// No description provided for @dashLibres.
  ///
  /// In es, this message translates to:
  /// **'{n} libre(s)'**
  String dashLibres(int n);

  /// No description provided for @dashProximosTurnos.
  ///
  /// In es, this message translates to:
  /// **'Mis próximos turnos'**
  String get dashProximosTurnos;

  /// No description provided for @dashHorasMes.
  ///
  /// In es, this message translates to:
  /// **'Horas planificadas este mes'**
  String get dashHorasMes;

  /// No description provided for @dashSinProximos.
  ///
  /// In es, this message translates to:
  /// **'No tiene turnos próximos.'**
  String get dashSinProximos;

  /// No description provided for @dashOfertasPendientes.
  ///
  /// In es, this message translates to:
  /// **'Ofertas de sustitución pendientes'**
  String get dashOfertasPendientes;

  /// No description provided for @misHoras.
  ///
  /// In es, this message translates to:
  /// **'Mis horas (últimos 30 días)'**
  String get misHoras;

  /// No description provided for @menuFichaje.
  ///
  /// In es, this message translates to:
  /// **'Fichaje'**
  String get menuFichaje;

  /// No description provided for @fichajeSinTurno.
  ///
  /// In es, this message translates to:
  /// **'No tiene ningún turno asignado en este momento sobre el que fichar.'**
  String get fichajeSinTurno;

  /// No description provided for @fichajeTurnoActual.
  ///
  /// In es, this message translates to:
  /// **'Turno: {turno}'**
  String fichajeTurnoActual(String turno);

  /// No description provided for @fichajeCentro.
  ///
  /// In es, this message translates to:
  /// **'Centro: {centro}'**
  String fichajeCentro(String centro);

  /// No description provided for @fichajeEstadoEnCurso.
  ///
  /// In es, this message translates to:
  /// **'Jornada en curso'**
  String get fichajeEstadoEnCurso;

  /// No description provided for @fichajeEstadoEnPausa.
  ///
  /// In es, this message translates to:
  /// **'En pausa'**
  String get fichajeEstadoEnPausa;

  /// No description provided for @fichajeEstadoCerrado.
  ///
  /// In es, this message translates to:
  /// **'Jornada cerrada'**
  String get fichajeEstadoCerrado;

  /// No description provided for @fichajeDesde.
  ///
  /// In es, this message translates to:
  /// **'Desde las {hora}'**
  String fichajeDesde(String hora);

  /// No description provided for @fichajeTrabajado.
  ///
  /// In es, this message translates to:
  /// **'Trabajado: {tiempo}'**
  String fichajeTrabajado(String tiempo);

  /// No description provided for @fichajePausas.
  ///
  /// In es, this message translates to:
  /// **'Pausas: {tiempo}'**
  String fichajePausas(String tiempo);

  /// No description provided for @fichajeEntrada.
  ///
  /// In es, this message translates to:
  /// **'Fichar entrada'**
  String get fichajeEntrada;

  /// No description provided for @fichajePausa.
  ///
  /// In es, this message translates to:
  /// **'Pausa'**
  String get fichajePausa;

  /// No description provided for @fichajeReanudar.
  ///
  /// In es, this message translates to:
  /// **'Reanudar'**
  String get fichajeReanudar;

  /// No description provided for @fichajeSalida.
  ///
  /// In es, this message translates to:
  /// **'Fichar salida'**
  String get fichajeSalida;

  /// No description provided for @fichajeRegistrado.
  ///
  /// In es, this message translates to:
  /// **'Fichaje registrado'**
  String get fichajeRegistrado;

  /// No description provided for @fichajeFueraDeZona.
  ///
  /// In es, this message translates to:
  /// **'Se ha registrado fuera de la zona del centro. La coordinación podrá revisarlo.'**
  String get fichajeFueraDeZona;

  /// No description provided for @fichajeSinUbicacion.
  ///
  /// In es, this message translates to:
  /// **'Registrado sin ubicación.'**
  String get fichajeSinUbicacion;

  /// No description provided for @fichajeUbicacionNoDisponible.
  ///
  /// In es, this message translates to:
  /// **'No se ha podido obtener su ubicación (permiso denegado, GPS desactivado o sin señal). ¿Desea fichar sin ubicación?'**
  String get fichajeUbicacionNoDisponible;

  /// No description provided for @fichajeFicharSinUbicacion.
  ///
  /// In es, this message translates to:
  /// **'Fichar sin ubicación'**
  String get fichajeFicharSinUbicacion;

  /// No description provided for @fichajeUbicacionAviso.
  ///
  /// In es, this message translates to:
  /// **'Al fichar se registra su ubicación solo en la entrada y la salida, para comprobar que está en el centro.'**
  String get fichajeUbicacionAviso;

  /// No description provided for @fichajeHistorial.
  ///
  /// In es, this message translates to:
  /// **'Mis fichajes recientes'**
  String get fichajeHistorial;

  /// No description provided for @fichajeSinHistorial.
  ///
  /// In es, this message translates to:
  /// **'Aún no hay fichajes en los últimos 7 días.'**
  String get fichajeSinHistorial;

  /// No description provided for @fichajeFueraDeZonaEtiqueta.
  ///
  /// In es, this message translates to:
  /// **'Fuera de zona'**
  String get fichajeFueraDeZonaEtiqueta;
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
