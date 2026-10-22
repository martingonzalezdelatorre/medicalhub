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
  String get listaVacia => 'Todavía no hay elementos.';

  @override
  String versionEntorno(String version, String entorno) {
    return 'Versión $version · $entorno';
  }

  @override
  String get loginTitulo => 'Iniciar sesión';

  @override
  String get campoCorreo => 'Correo electrónico';

  @override
  String get campoContrasena => 'Contraseña';

  @override
  String get campoCodigoMfa => 'Código de verificación (6 dígitos)';

  @override
  String get botonEntrar => 'Entrar';

  @override
  String get mfaRequeridoAviso =>
      'Introduzca el código de su aplicación autenticadora.';

  @override
  String get validaObligatorio => 'Campo obligatorio';

  @override
  String get validaCorreo => 'Introduzca un correo válido';

  @override
  String validaLongitudMinima(int n) {
    return 'Mínimo $n caracteres';
  }

  @override
  String get validaCodigo => 'El código tiene 6 dígitos';

  @override
  String get mfaTitulo => 'Verificación en dos pasos';

  @override
  String get mfaInstrucciones =>
      'Escanee el código QR con su aplicación autenticadora e introduzca el código de 6 dígitos para activar la protección.';

  @override
  String get mfaClaveManual => 'O introduzca esta clave manualmente';

  @override
  String get botonActivar => 'Activar';

  @override
  String get botonCopiar => 'Copiar';

  @override
  String get copiado => 'Copiado al portapapeles';

  @override
  String get cerrarSesion => 'Cerrar sesión';

  @override
  String get menuInicio => 'Inicio';

  @override
  String get menuUsuarios => 'Usuarios';

  @override
  String get menuOrganizaciones => 'Sociedades';

  @override
  String bienvenida(String nombre) {
    return 'Hola, $nombre';
  }

  @override
  String get perfilSociedad => 'Sociedad';

  @override
  String get perfilRoles => 'Roles';

  @override
  String get perfilMfa => 'Verificación en dos pasos';

  @override
  String get mfaActiva => 'Activada';

  @override
  String get mfaInactiva => 'No activada';

  @override
  String get usuariosTitulo => 'Usuarios';

  @override
  String get nuevoUsuario => 'Nuevo usuario';

  @override
  String get campoNombre => 'Nombre completo';

  @override
  String get campoRoles => 'Roles';

  @override
  String get validaRol => 'Seleccione al menos un rol';

  @override
  String get botonGuardar => 'Guardar';

  @override
  String get botonCancelar => 'Cancelar';

  @override
  String get estadoActivo => 'Activo';

  @override
  String get estadoInactivo => 'Inactivo';

  @override
  String get accionDesactivar => 'Desactivar';

  @override
  String get accionActivar => 'Activar';

  @override
  String get accionReiniciarMfa => 'Reiniciar verificación en dos pasos';

  @override
  String get operacionRealizada => 'Operación realizada';

  @override
  String get organizacionesTitulo => 'Sociedades';

  @override
  String get nuevaOrganizacion => 'Nueva sociedad';

  @override
  String get campoNombreSociedad => 'Nombre de la sociedad';

  @override
  String get campoZonaHoraria => 'Zona horaria';

  @override
  String get campoMfaObligatorio =>
      'Exigir verificación en dos pasos a administradores y coordinadores';

  @override
  String get datosAdministrador => 'Administrador inicial';

  @override
  String get rolAdministradorPlataforma => 'Administrador de plataforma';

  @override
  String get rolAdministradorSociedad => 'Administrador de sociedad';

  @override
  String get rolCoordinadorMedico => 'Coordinador médico';

  @override
  String get rolProfesionalSanitario => 'Profesional sanitario';

  @override
  String get rolResponsableCentro => 'Responsable de centro';
}
