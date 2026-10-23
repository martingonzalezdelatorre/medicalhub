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
