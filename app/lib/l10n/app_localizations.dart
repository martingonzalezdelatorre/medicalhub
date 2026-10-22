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
  /// **'Categorías'**
  String get catCategorias;

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
  /// **'Categoría profesional'**
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
