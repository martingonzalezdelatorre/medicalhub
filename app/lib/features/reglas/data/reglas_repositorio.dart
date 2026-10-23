import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';

part 'reglas_repositorio.g.dart';

@JsonSerializable(createToJson: false)
class ParametroReglaDto {
  const ParametroReglaDto({required this.clave, required this.etiqueta, required this.tipo, this.minimo, this.maximo});

  factory ParametroReglaDto.fromJson(Map<String, dynamic> json) => _$ParametroReglaDtoFromJson(json);

  final String clave;
  final String etiqueta;

  /// «entero» o «servicio».
  final String tipo;
  final int? minimo;
  final int? maximo;
}

@JsonSerializable(createToJson: false)
class TipoReglaDto {
  const TipoReglaDto({
    required this.tipo,
    required this.nombre,
    required this.descripcion,
    required this.nivel,
    required this.comportamientoPorDefecto,
    required this.parametros,
  });

  factory TipoReglaDto.fromJson(Map<String, dynamic> json) => _$TipoReglaDtoFromJson(json);

  final String tipo;
  final String nombre;
  final String descripcion;
  final String nivel;
  final String comportamientoPorDefecto;
  final List<ParametroReglaDto> parametros;
}

@JsonSerializable(createToJson: false)
class ReglaDto {
  const ReglaDto({
    required this.id,
    required this.tipo,
    required this.nombre,
    this.descripcion,
    this.servicioId,
    this.servicioNombre,
    required this.severidad,
    required this.activa,
    required this.permiteExcepcion,
    required this.excepcionRequiereAdministrador,
    this.parametros,
    this.fundamento,
    this.revisadaEl,
    this.revisadaPor,
    required this.pendienteDeRevision,
  });

  factory ReglaDto.fromJson(Map<String, dynamic> json) => _$ReglaDtoFromJson(json);

  final String id;
  final String tipo;
  final String nombre;
  final String? descripcion;
  final String? servicioId;
  final String? servicioNombre;
  final String severidad;
  final bool activa;
  final bool permiteExcepcion;
  final bool excepcionRequiereAdministrador;
  final Map<String, dynamic>? parametros;
  final String? fundamento;
  final DateTime? revisadaEl;
  final String? revisadaPor;
  final bool pendienteDeRevision;

  bool get bloqueante => severidad == 'Bloqueante';
}

@JsonSerializable(createToJson: false)
class ExcepcionDto {
  const ExcepcionDto({
    required this.id,
    required this.fecha,
    required this.profesionalId,
    required this.profesional,
    required this.turno,
    required this.codigo,
    required this.regla,
    required this.mensaje,
    required this.justificacion,
    required this.autorizadaPor,
    required this.asignacionActiva,
  });

  factory ExcepcionDto.fromJson(Map<String, dynamic> json) => _$ExcepcionDtoFromJson(json);

  final String id;
  final DateTime fecha;
  final String profesionalId;
  final String profesional;
  final String turno;
  final String codigo;
  final String regla;
  final String mensaje;
  final String justificacion;
  final String autorizadaPor;
  final bool asignacionActiva;
}

class ReglasRepositorio {
  ReglasRepositorio(this._dio);
  final Dio _dio;

  Future<List<TipoReglaDto>> tipos() => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/reglas-planificacion/tipos');
        return [for (final j in r.data!) TipoReglaDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<List<ReglaDto>> reglas() => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/reglas-planificacion');
        return [for (final j in r.data!) ReglaDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<void> guardar(String? id, Map<String, dynamic> datos) => protegerApi(() =>
      id == null ? _dio.post<void>('/api/reglas-planificacion', data: datos) : _dio.put<void>('/api/reglas-planificacion/$id', data: datos));

  Future<void> cambiarEstado(String id, {required bool activa}) =>
      protegerApi(() => _dio.post<void>('/api/reglas-planificacion/$id/${activa ? 'activar' : 'desactivar'}'));

  Future<void> revisar(String id) => protegerApi(() => _dio.post<void>('/api/reglas-planificacion/$id/revisar'));

  Future<List<ExcepcionDto>> excepciones() => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/excepciones-reglas');
        return [for (final j in r.data!) ExcepcionDto.fromJson(j as Map<String, dynamic>)];
      });
}

final reglasRepositorioProvider = Provider((ref) => ReglasRepositorio(ref.watch(clienteApiProvider)));

final tiposReglaProvider = FutureProvider.autoDispose<List<TipoReglaDto>>((ref) => ref.watch(reglasRepositorioProvider).tipos());
final reglasProvider = FutureProvider.autoDispose<List<ReglaDto>>((ref) => ref.watch(reglasRepositorioProvider).reglas());
final excepcionesProvider = FutureProvider.autoDispose<List<ExcepcionDto>>((ref) => ref.watch(reglasRepositorioProvider).excepciones());
