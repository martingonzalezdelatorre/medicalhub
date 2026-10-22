// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'informacion_sistema.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

InformacionSistema _$InformacionSistemaFromJson(Map<String, dynamic> json) =>
    InformacionSistema(
      nombre: json['nombre'] as String,
      version: json['version'] as String,
      entorno: json['entorno'] as String,
    );

Map<String, dynamic> _$InformacionSistemaToJson(InformacionSistema instance) =>
    <String, dynamic>{
      'nombre': instance.nombre,
      'version': instance.version,
      'entorno': instance.entorno,
    };
