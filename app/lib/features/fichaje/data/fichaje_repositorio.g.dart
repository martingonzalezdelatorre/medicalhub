// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'fichaje_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

PausaFichajeDto _$PausaFichajeDtoFromJson(Map<String, dynamic> json) =>
    PausaFichajeDto(
      inicioUtc: DateTime.parse(json['inicioUtc'] as String),
      finUtc: json['finUtc'] == null
          ? null
          : DateTime.parse(json['finUtc'] as String),
    );

FichajeDto _$FichajeDtoFromJson(Map<String, dynamic> json) => FichajeDto(
  id: json['id'] as String,
  asignacionId: json['asignacionId'] as String,
  estado: $enumDecode(_$EstadoFichajeEnumMap, json['estado']),
  turno: json['turno'] as String,
  centro: json['centro'] as String,
  centroConGeovalla: json['centroConGeovalla'] as bool,
  inicioUtc: DateTime.parse(json['inicioUtc'] as String),
  finUtc: json['finUtc'] == null
      ? null
      : DateTime.parse(json['finUtc'] as String),
  minutosPausa: (json['minutosPausa'] as num).toInt(),
  minutosTrabajados: (json['minutosTrabajados'] as num).toInt(),
  entradaFueraDeZona: json['entradaFueraDeZona'] as bool?,
  salidaFueraDeZona: json['salidaFueraDeZona'] as bool?,
  entradaSinUbicacion: json['entradaSinUbicacion'] as bool? ?? false,
  salidaSinUbicacion: json['salidaSinUbicacion'] as bool?,
  pausas:
      (json['pausas'] as List<dynamic>?)
          ?.map((e) => PausaFichajeDto.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
);

const _$EstadoFichajeEnumMap = {
  EstadoFichaje.enCurso: 'EnCurso',
  EstadoFichaje.enPausa: 'EnPausa',
  EstadoFichaje.cerrado: 'Cerrado',
  EstadoFichaje.descartado: 'Descartado',
};

TurnoFichableDto _$TurnoFichableDtoFromJson(Map<String, dynamic> json) =>
    TurnoFichableDto(
      asignacionId: json['asignacionId'] as String,
      turno: json['turno'] as String,
      centro: json['centro'] as String,
      centroConGeovalla: json['centroConGeovalla'] as bool,
      inicioUtc: DateTime.parse(json['inicioUtc'] as String),
      finUtc: DateTime.parse(json['finUtc'] as String),
    );

EstadoFichajeDto _$EstadoFichajeDtoFromJson(Map<String, dynamic> json) =>
    EstadoFichajeDto(
      actual: json['actual'] == null
          ? null
          : FichajeDto.fromJson(json['actual'] as Map<String, dynamic>),
      fichable: json['fichable'] == null
          ? null
          : TurnoFichableDto.fromJson(json['fichable'] as Map<String, dynamic>),
    );
