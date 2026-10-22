// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'estructura_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

CentroDto _$CentroDtoFromJson(Map<String, dynamic> json) => CentroDto(
  id: json['id'] as String,
  nombre: json['nombre'] as String,
  tipo: $enumDecode(_$TipoCentroEnumMap, json['tipo']),
  direccion: json['direccion'] as String?,
  zonaHoraria: json['zonaHoraria'] as String,
  activo: json['activo'] as bool,
  responsablesIds: (json['responsablesIds'] as List<dynamic>)
      .map((e) => e as String)
      .toList(),
);

const _$TipoCentroEnumMap = {
  TipoCentro.hospital: 'Hospital',
  TipoCentro.clinica: 'Clinica',
  TipoCentro.centroAmbulatorio: 'CentroAmbulatorio',
  TipoCentro.otro: 'Otro',
};

TipoTurnoDto _$TipoTurnoDtoFromJson(Map<String, dynamic> json) => TipoTurnoDto(
  id: json['id'] as String,
  nombre: json['nombre'] as String,
  horaInicio: json['horaInicio'] as String,
  horaFin: json['horaFin'] as String,
  esGuardia: json['esGuardia'] as bool,
  activo: json['activo'] as bool,
  cruzaMedianoche: json['cruzaMedianoche'] as bool,
  duracionHoras: (json['duracionHoras'] as num).toDouble(),
);

RequisitoDto _$RequisitoDtoFromJson(Map<String, dynamic> json) => RequisitoDto(
  id: json['id'] as String,
  tipoTurnoId: json['tipoTurnoId'] as String,
  numeroProfesionales: (json['numeroProfesionales'] as num).toInt(),
  categoriaProfesionalId: json['categoriaProfesionalId'] as String?,
);

ServicioDto _$ServicioDtoFromJson(Map<String, dynamic> json) => ServicioDto(
  id: json['id'] as String,
  centroId: json['centroId'] as String,
  centroNombre: json['centroNombre'] as String,
  nombre: json['nombre'] as String,
  descripcion: json['descripcion'] as String?,
  coordinadorUsuarioId: json['coordinadorUsuarioId'] as String?,
  configuracionReglas: json['configuracionReglas'] as String?,
  activo: json['activo'] as bool,
  categoriasAdmitidasIds: (json['categoriasAdmitidasIds'] as List<dynamic>)
      .map((e) => e as String)
      .toList(),
  competenciasRequeridasIds:
      (json['competenciasRequeridasIds'] as List<dynamic>)
          .map((e) => e as String)
          .toList(),
  tiposTurno: (json['tiposTurno'] as List<dynamic>)
      .map((e) => TipoTurnoDto.fromJson(e as Map<String, dynamic>))
      .toList(),
  requisitos: (json['requisitos'] as List<dynamic>)
      .map((e) => RequisitoDto.fromJson(e as Map<String, dynamic>))
      .toList(),
);
