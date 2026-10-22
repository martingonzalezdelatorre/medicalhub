// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'profesionales_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

CompetenciaProfesionalDto _$CompetenciaProfesionalDtoFromJson(
  Map<String, dynamic> json,
) => CompetenciaProfesionalDto(
  competenciaId: json['competenciaId'] as String,
  vigenteHasta: json['vigenteHasta'] as String?,
);

ProfesionalDto _$ProfesionalDtoFromJson(Map<String, dynamic> json) =>
    ProfesionalDto(
      id: json['id'] as String,
      usuarioId: json['usuarioId'] as String?,
      nombre: json['nombre'] as String,
      apellidos: json['apellidos'] as String,
      email: json['email'] as String?,
      telefono: json['telefono'] as String?,
      numeroColegiado: json['numeroColegiado'] as String?,
      categoriaId: json['categoriaId'] as String,
      especialidadesIds: (json['especialidadesIds'] as List<dynamic>)
          .map((e) => e as String)
          .toList(),
      competencias: (json['competencias'] as List<dynamic>)
          .map(
            (e) =>
                CompetenciaProfesionalDto.fromJson(e as Map<String, dynamic>),
          )
          .toList(),
      serviciosIds: (json['serviciosIds'] as List<dynamic>)
          .map((e) => e as String)
          .toList(),
      activo: json['activo'] as bool,
    );

CambioHistorialDto _$CambioHistorialDtoFromJson(Map<String, dynamic> json) =>
    CambioHistorialDto(
      fecha: DateTime.parse(json['fecha'] as String),
      usuario: json['usuario'] as String?,
      entidad: json['entidad'] as String,
      accion: json['accion'] as String,
      cambios: json['cambios'] as String?,
    );
