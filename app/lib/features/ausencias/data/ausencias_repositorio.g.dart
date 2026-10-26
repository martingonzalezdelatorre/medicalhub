// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'ausencias_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DecisionDto _$DecisionDtoFromJson(Map<String, dynamic> json) => DecisionDto(
  decision: $enumDecode(_$EstadoAusenciaEnumMap, json['decision']),
  usuario: json['usuario'] as String,
  fecha: DateTime.parse(json['fecha'] as String),
  motivo: json['motivo'] as String?,
);

const _$EstadoAusenciaEnumMap = {
  EstadoAusencia.solicitada: 'Solicitada',
  EstadoAusencia.aprobada: 'Aprobada',
  EstadoAusencia.rechazada: 'Rechazada',
  EstadoAusencia.cancelada: 'Cancelada',
};

AusenciaDto _$AusenciaDtoFromJson(Map<String, dynamic> json) => AusenciaDto(
  id: json['id'] as String,
  profesionalId: json['profesionalId'] as String,
  profesional: json['profesional'] as String,
  tipo: $enumDecode(_$TipoAusenciaEnumMap, json['tipo']),
  desde: json['desde'] as String,
  hasta: json['hasta'] as String,
  dias: (json['dias'] as num).toInt(),
  comentario: json['comentario'] as String?,
  estado: $enumDecode(_$EstadoAusenciaEnumMap, json['estado']),
  fechaSolicitud: DateTime.parse(json['fechaSolicitud'] as String),
  solicitadaPor: json['solicitadaPor'] as String,
  decisiones: (json['decisiones'] as List<dynamic>)
      .map((e) => DecisionDto.fromJson(e as Map<String, dynamic>))
      .toList(),
);

const _$TipoAusenciaEnumMap = {
  TipoAusencia.vacaciones: 'Vacaciones',
  TipoAusencia.asuntosPropios: 'AsuntosPropios',
  TipoAusencia.permiso: 'Permiso',
  TipoAusencia.formacion: 'Formacion',
  TipoAusencia.bajaTemporal: 'BajaTemporal',
  TipoAusencia.otro: 'Otro',
};

TurnoComprometidoDto _$TurnoComprometidoDtoFromJson(
  Map<String, dynamic> json,
) => TurnoComprometidoDto(
  asignacionId: json['asignacionId'] as String,
  turnoId: json['turnoId'] as String,
  necesidadId: json['necesidadId'] as String,
  fecha: json['fecha'] as String,
  turno: json['turno'] as String,
  servicio: json['servicio'] as String,
  centro: json['centro'] as String,
  cuadranteEstado: $enumDecode(
    _$EstadoCuadranteEnumMap,
    json['cuadranteEstado'],
  ),
);

const _$EstadoCuadranteEnumMap = {
  EstadoCuadrante.borrador: 'Borrador',
  EstadoCuadrante.publicado: 'Publicado',
  EstadoCuadrante.archivado: 'Archivado',
};

ResultadoAusenciaDto _$ResultadoAusenciaDtoFromJson(
  Map<String, dynamic> json,
) => ResultadoAusenciaDto(
  ausencia: AusenciaDto.fromJson(json['ausencia'] as Map<String, dynamic>),
  vacantes: (json['vacantes'] as List<dynamic>)
      .map((e) => TurnoComprometidoDto.fromJson(e as Map<String, dynamic>))
      .toList(),
);

DisponibilidadDto _$DisponibilidadDtoFromJson(Map<String, dynamic> json) =>
    DisponibilidadDto(
      id: json['id'] as String,
      profesionalId: json['profesionalId'] as String,
      profesional: json['profesional'] as String,
      tipo: $enumDecode(_$TipoDisponibilidadEnumMap, json['tipo']),
      desde: json['desde'] as String,
      hasta: json['hasta'] as String,
      dias: (json['dias'] as num).toInt(),
      tipoTurnoId: json['tipoTurnoId'] as String?,
      tipoTurno: json['tipoTurno'] as String?,
      comentario: json['comentario'] as String?,
      estado: $enumDecode(_$EstadoDisponibilidadEnumMap, json['estado']),
      fechaDeclaracion: DateTime.parse(json['fechaDeclaracion'] as String),
      motivoDecision: json['motivoDecision'] as String?,
      centroId: json['centroId'] as String?,
      centro: json['centro'] as String?,
    );

const _$TipoDisponibilidadEnumMap = {
  TipoDisponibilidad.disponible: 'Disponible',
  TipoDisponibilidad.bloqueo: 'Bloqueo',
  TipoDisponibilidad.prefiere: 'Prefiere',
  TipoDisponibilidad.evita: 'Evita',
};

const _$EstadoDisponibilidadEnumMap = {
  EstadoDisponibilidad.declarada: 'Declarada',
  EstadoDisponibilidad.aprobada: 'Aprobada',
  EstadoDisponibilidad.rechazada: 'Rechazada',
};

TipoTurnoPropioDto _$TipoTurnoPropioDtoFromJson(Map<String, dynamic> json) =>
    TipoTurnoPropioDto(
      id: json['id'] as String,
      nombre: json['nombre'] as String,
      franja: json['franja'] as String,
      servicio: json['servicio'] as String,
      centro: json['centro'] as String,
      centroId: json['centroId'] as String?,
    );

VacanteDto _$VacanteDtoFromJson(Map<String, dynamic> json) => VacanteDto(
  id: json['id'] as String,
  estado: $enumDecode(_$EstadoVacanteEnumMap, json['estado']),
  motivo: json['motivo'] as String,
  fechaCreacion: DateTime.parse(json['fechaCreacion'] as String),
  fechaCierre: json['fechaCierre'] == null
      ? null
      : DateTime.parse(json['fechaCierre'] as String),
  necesidadId: json['necesidadId'] as String,
  turnoId: json['turnoId'] as String,
  fecha: json['fecha'] as String,
  turno: json['turno'] as String,
  servicio: json['servicio'] as String,
  centro: json['centro'] as String,
  categoria: json['categoria'] as String?,
  profesionalAusente: json['profesionalAusente'] as String,
  ausenciaId: json['ausenciaId'] as String?,
  cuadranteEstado: $enumDecode(
    _$EstadoCuadranteEnumMap,
    json['cuadranteEstado'],
  ),
);

const _$EstadoVacanteEnumMap = {
  EstadoVacante.abierta: 'Abierta',
  EstadoVacante.cubierta: 'Cubierta',
  EstadoVacante.cancelada: 'Cancelada',
};

OfertaDto _$OfertaDtoFromJson(Map<String, dynamic> json) => OfertaDto(
  id: json['id'] as String,
  vacanteId: json['vacanteId'] as String,
  profesionalId: json['profesionalId'] as String,
  profesional: json['profesional'] as String,
  estado: $enumDecode(_$EstadoOfertaEnumMap, json['estado']),
  fechaOferta: DateTime.parse(json['fechaOferta'] as String),
  caduca: json['caduca'] == null
      ? null
      : DateTime.parse(json['caduca'] as String),
  fechaRespuesta: json['fechaRespuesta'] == null
      ? null
      : DateTime.parse(json['fechaRespuesta'] as String),
  motivoRespuesta: json['motivoRespuesta'] as String?,
  fechaDecision: json['fechaDecision'] == null
      ? null
      : DateTime.parse(json['fechaDecision'] as String),
  motivoDecision: json['motivoDecision'] as String?,
  ofrecidaPor: json['ofrecidaPor'] as String,
  necesidadId: json['necesidadId'] as String,
  fecha: json['fecha'] as String,
  turno: json['turno'] as String,
  servicio: json['servicio'] as String,
  centro: json['centro'] as String,
  estadoVacante: $enumDecode(_$EstadoVacanteEnumMap, json['estadoVacante']),
  profesionalAusente: json['profesionalAusente'] as String,
);

const _$EstadoOfertaEnumMap = {
  EstadoOferta.pendiente: 'Pendiente',
  EstadoOferta.aceptada: 'Aceptada',
  EstadoOferta.rechazada: 'Rechazada',
  EstadoOferta.aprobada: 'Aprobada',
  EstadoOferta.denegada: 'Denegada',
  EstadoOferta.retirada: 'Retirada',
  EstadoOferta.caducada: 'Caducada',
  EstadoOferta.cerrada: 'Cerrada',
};

NotificacionDto _$NotificacionDtoFromJson(Map<String, dynamic> json) =>
    NotificacionDto(
      id: json['id'] as String,
      tipo: json['tipo'] as String,
      titulo: json['titulo'] as String,
      mensaje: json['mensaje'] as String,
      entidadId: json['entidadId'] as String?,
      fecha: DateTime.parse(json['fecha'] as String),
      leida: json['leida'] as bool,
    );
