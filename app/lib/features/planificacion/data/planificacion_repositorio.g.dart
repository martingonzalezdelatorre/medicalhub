// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'planificacion_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

CuadranteDto _$CuadranteDtoFromJson(Map<String, dynamic> json) => CuadranteDto(
  id: json['id'] as String,
  servicioId: json['servicioId'] as String,
  servicioNombre: json['servicioNombre'] as String,
  centroId: json['centroId'] as String,
  centroNombre: json['centroNombre'] as String,
  nombre: json['nombre'] as String,
  desde: json['desde'] as String,
  hasta: json['hasta'] as String,
  estado: $enumDecode(_$EstadoCuadranteEnumMap, json['estado']),
  version: (json['version'] as num).toInt(),
  fechaPublicacion: json['fechaPublicacion'] == null
      ? null
      : DateTime.parse(json['fechaPublicacion'] as String),
  totalTurnos: (json['totalTurnos'] as num).toInt(),
  plazasTotales: (json['plazasTotales'] as num).toInt(),
  plazasCubiertas: (json['plazasCubiertas'] as num).toInt(),
);

const _$EstadoCuadranteEnumMap = {
  EstadoCuadrante.borrador: 'Borrador',
  EstadoCuadrante.publicado: 'Publicado',
  EstadoCuadrante.archivado: 'Archivado',
};

AsignacionDto _$AsignacionDtoFromJson(Map<String, dynamic> json) =>
    AsignacionDto(
      id: json['id'] as String,
      profesionalId: json['profesionalId'] as String,
      profesional: json['profesional'] as String,
      estado: json['estado'] as String,
    );

NecesidadDto _$NecesidadDtoFromJson(Map<String, dynamic> json) => NecesidadDto(
  id: json['id'] as String,
  numeroPlazas: (json['numeroPlazas'] as num).toInt(),
  categoriaProfesionalId: json['categoriaProfesionalId'] as String?,
  categoria: json['categoria'] as String?,
  asignaciones: (json['asignaciones'] as List<dynamic>)
      .map((e) => AsignacionDto.fromJson(e as Map<String, dynamic>))
      .toList(),
);

TurnoDto _$TurnoDtoFromJson(Map<String, dynamic> json) => TurnoDto(
  id: json['id'] as String,
  cuadranteId: json['cuadranteId'] as String,
  cuadranteEstado: $enumDecode(
    _$EstadoCuadranteEnumMap,
    json['cuadranteEstado'],
  ),
  fecha: json['fecha'] as String,
  inicioLocal: json['inicioLocal'] as String,
  finLocal: json['finLocal'] as String,
  cruzaMedianoche: json['cruzaMedianoche'] as bool,
  duracionHoras: (json['duracionHoras'] as num).toDouble(),
  centroId: json['centroId'] as String,
  centro: json['centro'] as String,
  servicioId: json['servicioId'] as String,
  servicio: json['servicio'] as String,
  tipoTurnoId: json['tipoTurnoId'] as String,
  tipoTurno: json['tipoTurno'] as String,
  esGuardia: json['esGuardia'] as bool,
  necesidades: (json['necesidades'] as List<dynamic>)
      .map((e) => NecesidadDto.fromJson(e as Map<String, dynamic>))
      .toList(),
  plazasTotales: (json['plazasTotales'] as num).toInt(),
  plazasCubiertas: (json['plazasCubiertas'] as num).toInt(),
);

ViolacionDto _$ViolacionDtoFromJson(Map<String, dynamic> json) => ViolacionDto(
  codigo: json['codigo'] as String,
  reglaId: json['reglaId'] as String?,
  regla: json['regla'] as String,
  severidad: json['severidad'] as String,
  mensaje: json['mensaje'] as String,
  permiteExcepcion: json['permiteExcepcion'] as bool,
  requiereAdministrador: json['requiereAdministrador'] as bool,
);

ResultadoAsignacionDto _$ResultadoAsignacionDtoFromJson(
  Map<String, dynamic> json,
) => ResultadoAsignacionDto(
  id: json['id'] as String,
  advertencias:
      (json['advertencias'] as List<dynamic>?)
          ?.map((e) => ViolacionDto.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  excepciones:
      (json['excepciones'] as List<dynamic>?)
          ?.map((e) => ViolacionDto.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
);

CandidatoDto _$CandidatoDtoFromJson(Map<String, dynamic> json) => CandidatoDto(
  profesionalId: json['profesionalId'] as String,
  nombreCompleto: json['nombreCompleto'] as String,
  elegible: json['elegible'] as bool,
  libre: json['libre'] as bool,
  motivos: (json['motivos'] as List<dynamic>).map((e) => e as String).toList(),
  violaciones:
      (json['violaciones'] as List<dynamic>?)
          ?.map((e) => ViolacionDto.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  requiereExcepcion: json['requiereExcepcion'] as bool? ?? false,
  puedeAutorizar: json['puedeAutorizar'] as bool? ?? false,
);

IncidenciaDto _$IncidenciaDtoFromJson(Map<String, dynamic> json) =>
    IncidenciaDto(
      severidad: json['severidad'] as String,
      codigo: json['codigo'] as String,
      mensaje: json['mensaje'] as String,
      excepcionada: json['excepcionada'] as bool? ?? false,
      justificacion: json['justificacion'] as String?,
      autorizadaPor: json['autorizadaPor'] as String?,
      asignacionId: json['asignacionId'] as String?,
      permiteExcepcion: json['permiteExcepcion'] as bool? ?? false,
    );

ValidacionDto _$ValidacionDtoFromJson(Map<String, dynamic> json) =>
    ValidacionDto(
      puedePublicarse: json['puedePublicarse'] as bool,
      incidencias: (json['incidencias'] as List<dynamic>)
          .map((e) => IncidenciaDto.fromJson(e as Map<String, dynamic>))
          .toList(),
    );

VersionResumenDto _$VersionResumenDtoFromJson(Map<String, dynamic> json) =>
    VersionResumenDto(
      numero: (json['numero'] as num).toInt(),
      fecha: DateTime.parse(json['fecha'] as String),
      usuario: json['usuario'] as String?,
      turnos: (json['turnos'] as num).toInt(),
      plazasTotales: (json['plazasTotales'] as num).toInt(),
      plazasCubiertas: (json['plazasCubiertas'] as num).toInt(),
    );

PlantillaDto _$PlantillaDtoFromJson(Map<String, dynamic> json) => PlantillaDto(
  id: json['id'] as String,
  servicioId: json['servicioId'] as String,
  nombre: json['nombre'] as String,
  diasPeriodo: (json['diasPeriodo'] as num).toInt(),
  lineas: (json['lineas'] as num).toInt(),
);
