// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'reglas_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ParametroReglaDto _$ParametroReglaDtoFromJson(Map<String, dynamic> json) =>
    ParametroReglaDto(
      clave: json['clave'] as String,
      etiqueta: json['etiqueta'] as String,
      tipo: json['tipo'] as String,
      minimo: (json['minimo'] as num?)?.toInt(),
      maximo: (json['maximo'] as num?)?.toInt(),
    );

TipoReglaDto _$TipoReglaDtoFromJson(Map<String, dynamic> json) => TipoReglaDto(
  tipo: json['tipo'] as String,
  nombre: json['nombre'] as String,
  descripcion: json['descripcion'] as String,
  nivel: json['nivel'] as String,
  comportamientoPorDefecto: json['comportamientoPorDefecto'] as String,
  parametros: (json['parametros'] as List<dynamic>)
      .map((e) => ParametroReglaDto.fromJson(e as Map<String, dynamic>))
      .toList(),
);

ReglaDto _$ReglaDtoFromJson(Map<String, dynamic> json) => ReglaDto(
  id: json['id'] as String,
  tipo: json['tipo'] as String,
  nombre: json['nombre'] as String,
  descripcion: json['descripcion'] as String?,
  servicioId: json['servicioId'] as String?,
  servicioNombre: json['servicioNombre'] as String?,
  severidad: json['severidad'] as String,
  activa: json['activa'] as bool,
  permiteExcepcion: json['permiteExcepcion'] as bool,
  excepcionRequiereAdministrador:
      json['excepcionRequiereAdministrador'] as bool,
  parametros: json['parametros'] as Map<String, dynamic>?,
  fundamento: json['fundamento'] as String?,
  revisadaEl: json['revisadaEl'] == null
      ? null
      : DateTime.parse(json['revisadaEl'] as String),
  revisadaPor: json['revisadaPor'] as String?,
  pendienteDeRevision: json['pendienteDeRevision'] as bool,
);

ExcepcionDto _$ExcepcionDtoFromJson(Map<String, dynamic> json) => ExcepcionDto(
  id: json['id'] as String,
  fecha: DateTime.parse(json['fecha'] as String),
  profesionalId: json['profesionalId'] as String,
  profesional: json['profesional'] as String,
  turno: json['turno'] as String,
  codigo: json['codigo'] as String,
  regla: json['regla'] as String,
  mensaje: json['mensaje'] as String,
  justificacion: json['justificacion'] as String,
  autorizadaPor: json['autorizadaPor'] as String,
  asignacionActiva: json['asignacionActiva'] as bool,
);
