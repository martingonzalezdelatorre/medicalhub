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

  @override
  String get menuProfesionales => 'Profesionales';

  @override
  String get menuCentros => 'Centros';

  @override
  String get menuCatalogos => 'Catálogos';

  @override
  String get editar => 'Editar';

  @override
  String get anadir => 'Añadir';

  @override
  String get quitar => 'Quitar';

  @override
  String get buscar => 'Buscar';

  @override
  String get soloActivos => 'Solo activos';

  @override
  String get campoDescripcion => 'Descripción';

  @override
  String get catEspecialidades => 'Especialidades';

  @override
  String get catCategorias => 'Categorías';

  @override
  String get catCompetencias => 'Competencias';

  @override
  String get catNuevo => 'Nuevo elemento';

  @override
  String get catEditar => 'Editar elemento';

  @override
  String get centrosTitulo => 'Centros';

  @override
  String get nuevoCentro => 'Nuevo centro';

  @override
  String get editarCentro => 'Editar centro';

  @override
  String get campoTipoCentro => 'Tipo de centro';

  @override
  String get campoDireccion => 'Dirección';

  @override
  String get campoResponsables => 'Responsables del centro';

  @override
  String get tipoHospital => 'Hospital';

  @override
  String get tipoClinica => 'Clínica';

  @override
  String get tipoCentroAmbulatorio => 'Centro ambulatorio';

  @override
  String get tipoOtro => 'Otro';

  @override
  String get serviciosTitulo => 'Servicios';

  @override
  String get nuevoServicio => 'Nuevo servicio';

  @override
  String get editarServicio => 'Editar servicio';

  @override
  String get campoCoordinador => 'Coordinador del servicio';

  @override
  String get sinCoordinador => 'Sin coordinador';

  @override
  String get campoCategoriasAdmitidas =>
      'Categorías admitidas (vacío = cualquiera)';

  @override
  String get campoCompetenciasRequeridas => 'Competencias requeridas';

  @override
  String get campoReglas => 'Reglas específicas (JSON, opcional)';

  @override
  String get tiposTurnoTitulo => 'Tipos de turno';

  @override
  String get nuevoTipoTurno => 'Nuevo tipo de turno';

  @override
  String get editarTipoTurno => 'Editar tipo de turno';

  @override
  String get campoHoraInicio => 'Inicio';

  @override
  String get campoHoraFin => 'Fin';

  @override
  String get campoGuardia => 'Es guardia';

  @override
  String get cruzaMedianoche => 'termina al día siguiente';

  @override
  String duracionHoras(String horas) {
    return '$horas h';
  }

  @override
  String get requisitosTitulo => 'Requisitos de cobertura';

  @override
  String get editarRequisitos => 'Editar requisitos';

  @override
  String get campoNumeroProfesionales => 'Nº de profesionales';

  @override
  String get cualquierCategoria => 'Cualquier categoría';

  @override
  String get campoTipoTurno => 'Tipo de turno';

  @override
  String get sinElementos => 'Sin elementos';

  @override
  String get validaNumero => 'Número entre 1 y 50';

  @override
  String get validaJson => 'No es un JSON válido';

  @override
  String get profesionalesTitulo => 'Profesionales';

  @override
  String get nuevoProfesional => 'Nuevo profesional';

  @override
  String get editarProfesional => 'Editar profesional';

  @override
  String get campoApellidos => 'Apellidos';

  @override
  String get campoTelefono => 'Teléfono';

  @override
  String get campoColegiado => 'Nº de colegiado';

  @override
  String get campoCategoria => 'Categoría profesional';

  @override
  String get campoEspecialidades => 'Especialidades';

  @override
  String get campoCompetencias => 'Competencias y habilitaciones';

  @override
  String get campoServiciosAutorizados => 'Servicios autorizados';

  @override
  String get campoUsuarioVinculado => 'Usuario vinculado';

  @override
  String get sinUsuario => 'Sin usuario';

  @override
  String vigenteHasta(String fecha) {
    return 'Vigente hasta $fecha';
  }

  @override
  String get sinCaducidad => 'Sin caducidad';

  @override
  String get fijarCaducidad => 'Fijar caducidad';

  @override
  String get historial => 'Historial';

  @override
  String get historialVacio => 'Sin cambios registrados.';

  @override
  String get cerrar => 'Cerrar';

  @override
  String get datosContacto => 'Datos de contacto';

  @override
  String get sinContacto => 'Sin datos de contacto';

  @override
  String get menuPlanificacion => 'Planificación';

  @override
  String get menuCuadrantes => 'Cuadrantes';

  @override
  String get menuMiPlanificacion => 'Mi planificación';

  @override
  String get menuMas => 'Más';

  @override
  String get eliminar => 'Eliminar';

  @override
  String get crear => 'Crear';

  @override
  String get cerrarMenu => 'Cerrar';

  @override
  String get vistaDia => 'Día';

  @override
  String get vistaSemana => 'Semana';

  @override
  String get vistaMes => 'Mes';

  @override
  String get vistaAgenda => 'Agenda';

  @override
  String get anterior => 'Anterior';

  @override
  String get siguiente => 'Siguiente';

  @override
  String get hoy => 'Hoy';

  @override
  String get filtroCentro => 'Centro';

  @override
  String get filtroServicio => 'Servicio';

  @override
  String get todos => 'Todos';

  @override
  String get soloSinCubrir => 'Solo sin cubrir';

  @override
  String get sinTurnos => 'Sin turnos';

  @override
  String resumenDia(int turnos, int sinCubrir) {
    return '$turnos turnos, $sinCubrir sin cubrir';
  }

  @override
  String plazasCubiertasSemantica(int cubiertas, int total) {
    return '$cubiertas de $total plazas cubiertas';
  }

  @override
  String faltanProfesionales(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: 'Faltan $n profesionales',
      one: 'Falta 1 profesional',
    );
    return '$_temp0';
  }

  @override
  String get cuadranteNoEditable =>
      'El cuadrante no está en borrador: reábralo para modificarlo.';

  @override
  String asignarProfesional(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: 'Asignar profesional ($n plazas libres)',
      one: 'Asignar profesional (1 plaza libre)',
    );
    return '$_temp0';
  }

  @override
  String plazasLibres(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: '$n plazas libres',
      one: '1 plaza libre',
    );
    return '$_temp0';
  }

  @override
  String get sinNecesidades =>
      'Este turno no tiene necesidades de cobertura definidas.';

  @override
  String get eliminarTurno => 'Eliminar turno';

  @override
  String get confirmaEliminarTurno =>
      'Se eliminará el turno con sus necesidades y asignaciones.';

  @override
  String get candidatosTitulo => 'Candidatos';

  @override
  String get asignar => 'Asignar';

  @override
  String get estadoBorrador => 'Borrador';

  @override
  String get estadoPublicado => 'Publicado';

  @override
  String get estadoArchivado => 'Archivado';

  @override
  String get nuevoCuadrante => 'Nuevo cuadrante';

  @override
  String get desdePlantilla => 'Desde plantilla';

  @override
  String plazasCubiertasDe(int cubiertas, int total) {
    return '$cubiertas/$total plazas cubiertas';
  }

  @override
  String get nombreOpcional => 'Nombre (opcional)';

  @override
  String get campoDesde => 'Desde';

  @override
  String get campoHasta => 'Hasta';

  @override
  String get generarTurnos => 'Generar turnos con los requisitos del servicio';

  @override
  String get plantilla => 'Plantilla';

  @override
  String get sinPlantillas =>
      'Todavía no hay plantillas. Guarde un cuadrante como plantilla para reutilizarlo.';

  @override
  String diasPlantilla(int n) {
    return '$n días';
  }

  @override
  String turnosCuenta(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: '$n turnos',
      one: '1 turno',
    );
    return '$_temp0';
  }

  @override
  String versionPublicada(int v) {
    return 'Versión publicada: $v';
  }

  @override
  String get verCalendario => 'Ver calendario';

  @override
  String get validarYPublicar => 'Validar y publicar';

  @override
  String get publicar => 'Publicar';

  @override
  String get cuadrantePublicado => 'Cuadrante publicado';

  @override
  String get anadirTurno => 'Añadir turno';

  @override
  String get eliminarCuadrante => 'Eliminar cuadrante';

  @override
  String get confirmaEliminarCuadrante =>
      'Se eliminará el borrador con todos sus turnos y asignaciones.';

  @override
  String get reabrir => 'Reabrir';

  @override
  String get archivar => 'Archivar';

  @override
  String get guardarPlantilla => 'Guardar como plantilla';

  @override
  String get plantillaGuardada => 'Plantilla guardada';

  @override
  String get exportarCsv => 'Exportar CSV';

  @override
  String get csvCopiado =>
      'Este dispositivo no permite descargar archivos: el CSV se ha copiado al portapapeles.';

  @override
  String get versiones => 'Versiones publicadas';

  @override
  String get sinVersiones => 'Todavía no se ha publicado.';

  @override
  String versionTitulo(int n) {
    return 'Versión $n';
  }

  @override
  String get sinIncidencias =>
      'No hay incidencias: el cuadrante está listo para publicarse.';

  @override
  String get errores => 'Errores (impiden publicar)';

  @override
  String get advertencias => 'Advertencias (revisables)';

  @override
  String yMasAvisos(int n) {
    return '… y $n avisos más';
  }

  @override
  String get menuReglas => 'Reglas';

  @override
  String get reglasTabReglas => 'Reglas';

  @override
  String get reglasTabExcepciones => 'Excepciones';

  @override
  String get nuevaRegla => 'Nueva regla';

  @override
  String get editarRegla => 'Editar regla';

  @override
  String get campoTipoRegla => 'Tipo de regla';

  @override
  String get campoAmbito => 'Ámbito';

  @override
  String get ambitoSociedad => 'Toda la sociedad';

  @override
  String get severidadBloqueante => 'Bloqueante';

  @override
  String get severidadAdvertencia => 'Advertencia';

  @override
  String get campoPermiteExcepcion => 'Permitir excepción justificada';

  @override
  String get campoExcepcionAdmin =>
      'Solo el administrador puede autorizar la excepción';

  @override
  String get campoFundamento => 'Fundamento (convenio, acuerdo o norma)';

  @override
  String get fundamentoAyuda =>
      'Lo completa la sociedad: el sistema no incluye límites legales por defecto';

  @override
  String get pendienteRevision => 'Pendiente de revisión';

  @override
  String revisadaPor(String nombre, String fecha) {
    return 'Revisada por $nombre el $fecha';
  }

  @override
  String get marcarRevisada => 'Marcar como revisada';

  @override
  String get reglasAviso =>
      'Las reglas las define y revisa su sociedad. Los límites (descansos, horas, guardias…) no existen hasta que los configure según su convenio o acuerdo; consulte la normativa aplicable a cada colectivo.';

  @override
  String get reglasVacio => 'Todavía no ha configurado ninguna regla.';

  @override
  String get excepcionSoloAdmin => 'excepción solo del administrador';

  @override
  String get excepcionPosible => 'admite excepción';

  @override
  String get comportamientoPorDefecto =>
      'Comportamiento cuando no hay regla configurada';

  @override
  String get sinExcepciones => 'No hay excepciones registradas.';

  @override
  String get excepcionAnulada => 'Asignación anulada';

  @override
  String validaRango(int min, int max) {
    return 'Número entre $min y $max';
  }

  @override
  String get conExcepcion => 'Con excepción';

  @override
  String conExcepcionTitulo(String nombre) {
    return 'Asignar a $nombre con excepción';
  }

  @override
  String get soloAdministrador => 'Requiere al administrador';

  @override
  String asignadoConAdvertencias(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: 'Asignado con $n advertencias',
      one: 'Asignado con 1 advertencia',
    );
    return '$_temp0';
  }

  @override
  String get asignadoConExcepcion => 'Asignado con excepción registrada';

  @override
  String get excepcionAviso =>
      'Esta asignación incumple las siguientes reglas. Puede autorizarla como excepción: quedará registrada con su justificación y su nombre.';

  @override
  String get justificacion => 'Justificación';

  @override
  String justificacionMinimo(int n) {
    return 'Explique el motivo (mínimo $n caracteres)';
  }

  @override
  String get autorizarExcepcion => 'Autorizar excepción';

  @override
  String get excepcionesAutorizadas => 'Excepciones autorizadas';

  @override
  String autorizadaPor(String nombre) {
    return 'Autorizada por $nombre';
  }

  @override
  String get ausenciaVacaciones => 'Vacaciones';

  @override
  String get ausenciaAsuntosPropios => 'Asuntos propios';

  @override
  String get ausenciaPermiso => 'Permiso';

  @override
  String get ausenciaFormacion => 'Formación';

  @override
  String get ausenciaBajaTemporal => 'Baja temporal';

  @override
  String get ausenciaOtro => 'Otro';

  @override
  String get ausenciaSolicitada => 'Solicitada';

  @override
  String get ausenciaAprobada => 'Aprobada';

  @override
  String get ausenciaRechazada => 'Rechazada';

  @override
  String get ausenciaCancelada => 'Cancelada';

  @override
  String get dispDisponible => 'Disponible';

  @override
  String get dispBloqueo => 'No disponible (bloqueo)';

  @override
  String get dispPrefiere => 'Prefiere';

  @override
  String get dispEvita => 'Prefiere evitar';

  @override
  String get dispPendiente => 'Pendiente de aprobación';

  @override
  String get todosLosDias => 'Todos los días';

  @override
  String get registrarAusencia => 'Registrar ausencia';

  @override
  String get solicitarAusencia => 'Solicitar ausencia';

  @override
  String get campoTipoAusencia => 'Tipo de ausencia';

  @override
  String get campoProfesional => 'Profesional';

  @override
  String get campoComentarioOpcional => 'Comentario (opcional)';

  @override
  String get avisoSinDatosSalud => 'No incluya datos de salud ni diagnósticos.';

  @override
  String get aprobarAlRegistrar => 'Aprobar al registrar';

  @override
  String get registrar => 'Registrar';

  @override
  String get solicitar => 'Solicitar';

  @override
  String get declararDisponibilidad => 'Declarar disponibilidad';

  @override
  String get campoTipoDisponibilidad => 'Tipo';

  @override
  String get campoDiasSemana => 'Días de la semana';

  @override
  String get campoTipoTurnoOpcional => 'Tipo de turno (opcional)';

  @override
  String get cualquierTurno => 'Cualquier turno';

  @override
  String get avisoDisponibilidad =>
      'Las declaraciones solo cuentan para la planificación cuando las aprueba la coordinación.';

  @override
  String get declarar => 'Declarar';

  @override
  String get menuMiDisponibilidad => 'Mi disponibilidad';

  @override
  String get menuAusencias => 'Ausencias';

  @override
  String get misAusencias => 'Mis ausencias';

  @override
  String get misDeclaraciones => 'Mis declaraciones';

  @override
  String get sinAusencias => 'No hay ausencias.';

  @override
  String get sinDeclaraciones => 'No hay declaraciones.';

  @override
  String get sinVacantes => 'No hay vacantes.';

  @override
  String get ausTabSolicitudes => 'Ausencias';

  @override
  String get ausTabVacantes => 'Vacantes';

  @override
  String get ausTabDisponibilidad => 'Disponibilidad';

  @override
  String get accionAprobar => 'Aprobar';

  @override
  String get accionRechazar => 'Rechazar';

  @override
  String get accionCancelarAusencia => 'Cancelar';

  @override
  String get accionRetirar => 'Retirar';

  @override
  String get accionCubrir => 'Buscar sustituto';

  @override
  String get motivoObligatorio => 'Motivo (obligatorio)';

  @override
  String get motivoOpcional => 'Motivo (opcional)';

  @override
  String turnosAfectados(int n) {
    return 'Turnos afectados: $n';
  }

  @override
  String vacantesCreadas(int n) {
    return 'Ausencia aprobada. Vacantes creadas: $n';
  }

  @override
  String get vacanteCubierta => 'Vacante cubierta';

  @override
  String vacanteAusente(String nombre) {
    return 'Ausente: $nombre';
  }

  @override
  String get estadoVacanteAbierta => 'Abierta';

  @override
  String get estadoVacanteCubierta => 'Cubierta';

  @override
  String get estadoVacanteCancelada => 'Cancelada';

  @override
  String diasNumero(int n) {
    return '$n días';
  }

  @override
  String get filtroTodas => 'Todas';
}
