// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'organizaciones_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

OrganizacionDto _$OrganizacionDtoFromJson(Map<String, dynamic> json) =>
    OrganizacionDto(
      id: json['id'] as String,
      nombre: json['nombre'] as String,
      zonaHoraria: json['zonaHoraria'] as String,
      activa: json['activa'] as bool,
      mfaObligatorio: json['mfaObligatorio'] as bool,
    );
