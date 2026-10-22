import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';

part 'estructura_repositorio.g.dart';

enum TipoCentro {
  @JsonValue('Hospital')
  hospital('Hospital'),
  @JsonValue('Clinica')
  clinica('Clinica'),
  @JsonValue('CentroAmbulatorio')
  centroAmbulatorio('CentroAmbulatorio'),
  @JsonValue('Otro')
  otro('Otro');

  const TipoCentro(this.valorApi);
  final String valorApi;
}

@JsonSerializable(createToJson: false)
class CentroDto {
  const CentroDto({
    required this.id,
    required this.nombre,
    required this.tipo,
    this.direccion,
    required this.zonaHoraria,
    required this.activo,
    required this.responsablesIds,
  });

  factory CentroDto.fromJson(Map<String, dynamic> json) => _$CentroDtoFromJson(json);

  final String id;
  final String nombre;
  final TipoCentro tipo;
  final String? direccion;
  final String zonaHoraria;
  final bool activo;
  final List<String> responsablesIds;
}

@JsonSerializable(createToJson: false)
class TipoTurnoDto {
  const TipoTurnoDto({
    required this.id,
    required this.nombre,
    required this.horaInicio,
    required this.horaFin,
    required this.esGuardia,
    required this.activo,
    required this.cruzaMedianoche,
    required this.duracionHoras,
  });

  factory TipoTurnoDto.fromJson(Map<String, dynamic> json) => _$TipoTurnoDtoFromJson(json);

  final String id;
  final String nombre;

  /// "HH:mm:ss" (hora local del centro).
  final String horaInicio;
  final String horaFin;
  final bool esGuardia;
  final bool activo;
  final bool cruzaMedianoche;
  final double duracionHoras;

  String get franja => '${horaInicio.substring(0, 5)}–${horaFin.substring(0, 5)}';
}

@JsonSerializable(createToJson: false)
class RequisitoDto {
  const RequisitoDto({
    required this.id,
    required this.tipoTurnoId,
    required this.numeroProfesionales,
    this.categoriaProfesionalId,
  });

  factory RequisitoDto.fromJson(Map<String, dynamic> json) => _$RequisitoDtoFromJson(json);

  final String id;
  final String tipoTurnoId;
  final int numeroProfesionales;
  final String? categoriaProfesionalId;
}

@JsonSerializable(createToJson: false)
class ServicioDto {
  const ServicioDto({
    required this.id,
    required this.centroId,
    required this.centroNombre,
    required this.nombre,
    this.descripcion,
    this.coordinadorUsuarioId,
    this.configuracionReglas,
    required this.activo,
    required this.categoriasAdmitidasIds,
    required this.competenciasRequeridasIds,
    required this.tiposTurno,
    required this.requisitos,
  });

  factory ServicioDto.fromJson(Map<String, dynamic> json) => _$ServicioDtoFromJson(json);

  final String id;
  final String centroId;
  final String centroNombre;
  final String nombre;
  final String? descripcion;
  final String? coordinadorUsuarioId;
  final String? configuracionReglas;
  final bool activo;
  final List<String> categoriasAdmitidasIds;
  final List<String> competenciasRequeridasIds;
  final List<TipoTurnoDto> tiposTurno;
  final List<RequisitoDto> requisitos;
}

class EstructuraRepositorio {
  EstructuraRepositorio(this._dio);
  final Dio _dio;

  // ── Centros ──
  Future<List<CentroDto>> centros({bool incluirInactivos = false}) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/centros', queryParameters: {'incluirInactivos': incluirInactivos});
        return [for (final j in r.data!) CentroDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<CentroDto> centro(String id) => protegerApi(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/centros/$id');
        return CentroDto.fromJson(r.data!);
      });

  Future<void> guardarCentro(String? id, Map<String, dynamic> datos) =>
      protegerApi(() => id == null ? _dio.post<void>('/api/centros', data: datos) : _dio.put<void>('/api/centros/$id', data: datos));

  Future<void> establecerResponsables(String id, List<String> usuarios) =>
      protegerApi(() => _dio.put<void>('/api/centros/$id/responsables', data: {'usuariosIds': usuarios}));

  Future<void> estadoCentro(String id, {required bool activo}) =>
      protegerApi(() => _dio.post<void>('/api/centros/$id/${activo ? 'activar' : 'desactivar'}'));

  // ── Servicios ──
  Future<List<ServicioDto>> servicios({String? centroId, bool incluirInactivos = false}) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/servicios', queryParameters: {
          'centroId': ?centroId,
          'incluirInactivos': incluirInactivos,
        });
        return [for (final j in r.data!) ServicioDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<ServicioDto> servicio(String id) => protegerApi(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/servicios/$id');
        return ServicioDto.fromJson(r.data!);
      });

  Future<void> guardarServicio(String? id, Map<String, dynamic> datos) => protegerApi(
      () => id == null ? _dio.post<void>('/api/servicios', data: datos) : _dio.put<void>('/api/servicios/$id', data: datos));

  Future<void> estadoServicio(String id, {required bool activo}) =>
      protegerApi(() => _dio.post<void>('/api/servicios/$id/${activo ? 'activar' : 'desactivar'}'));

  Future<void> guardarTipoTurno(String servicioId, String? tipoId, Map<String, dynamic> datos) => protegerApi(() => tipoId == null
      ? _dio.post<void>('/api/servicios/$servicioId/tipos-turno', data: datos)
      : _dio.put<void>('/api/servicios/$servicioId/tipos-turno/$tipoId', data: datos));

  Future<void> estadoTipoTurno(String servicioId, String tipoId, {required bool activo}) => protegerApi(
      () => _dio.post<void>('/api/servicios/$servicioId/tipos-turno/$tipoId/${activo ? 'activar' : 'desactivar'}'));

  Future<void> guardarRequisitos(String servicioId, List<Map<String, dynamic>> requisitos) =>
      protegerApi(() => _dio.put<void>('/api/servicios/$servicioId/requisitos', data: {'requisitos': requisitos}));
}

final estructuraRepositorioProvider = Provider((ref) => EstructuraRepositorio(ref.watch(clienteApiProvider)));

final centrosProvider = FutureProvider.autoDispose<List<CentroDto>>(
  (ref) => ref.watch(estructuraRepositorioProvider).centros(incluirInactivos: true),
);

final centroProvider = FutureProvider.autoDispose.family<CentroDto, String>(
  (ref, id) => ref.watch(estructuraRepositorioProvider).centro(id),
);

final serviciosDeCentroProvider = FutureProvider.autoDispose.family<List<ServicioDto>, String>(
  (ref, centroId) => ref.watch(estructuraRepositorioProvider).servicios(centroId: centroId, incluirInactivos: true),
);

final servicioProvider = FutureProvider.autoDispose.family<ServicioDto, String>(
  (ref, id) => ref.watch(estructuraRepositorioProvider).servicio(id),
);

/// Todos los servicios activos (selector de servicios autorizados de un profesional).
final serviciosActivosProvider = FutureProvider.autoDispose<List<ServicioDto>>(
  (ref) => ref.watch(estructuraRepositorioProvider).servicios(),
);
