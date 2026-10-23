import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';
import '../../planificacion/data/planificacion_repositorio.dart' show EstadoCuadrante, ResultadoAsignacionDto, fechaParaApi;

part 'ausencias_repositorio.g.dart';

enum TipoAusencia {
  @JsonValue('Vacaciones')
  vacaciones('Vacaciones'),
  @JsonValue('AsuntosPropios')
  asuntosPropios('AsuntosPropios'),
  @JsonValue('Permiso')
  permiso('Permiso'),
  @JsonValue('Formacion')
  formacion('Formacion'),
  @JsonValue('BajaTemporal')
  bajaTemporal('BajaTemporal'),
  @JsonValue('Otro')
  otro('Otro');

  const TipoAusencia(this.valorApi);
  final String valorApi;
}

enum EstadoAusencia {
  @JsonValue('Solicitada')
  solicitada,
  @JsonValue('Aprobada')
  aprobada,
  @JsonValue('Rechazada')
  rechazada,
  @JsonValue('Cancelada')
  cancelada,
}

enum TipoDisponibilidad {
  @JsonValue('Disponible')
  disponible('Disponible'),
  @JsonValue('Bloqueo')
  bloqueo('Bloqueo'),
  @JsonValue('Prefiere')
  prefiere('Prefiere'),
  @JsonValue('Evita')
  evita('Evita');

  const TipoDisponibilidad(this.valorApi);
  final String valorApi;
}

enum EstadoDisponibilidad {
  @JsonValue('Declarada')
  declarada,
  @JsonValue('Aprobada')
  aprobada,
  @JsonValue('Rechazada')
  rechazada,
}

enum EstadoVacante {
  @JsonValue('Abierta')
  abierta,
  @JsonValue('Cubierta')
  cubierta,
  @JsonValue('Cancelada')
  cancelada,
}

/// Máscara de días de la semana: lunes = 1 … domingo = 64.
const diasTodos = 127;

@JsonSerializable(createToJson: false)
class DecisionDto {
  const DecisionDto({required this.decision, required this.usuario, required this.fecha, this.motivo});

  factory DecisionDto.fromJson(Map<String, dynamic> json) => _$DecisionDtoFromJson(json);

  final EstadoAusencia decision;
  final String usuario;
  final DateTime fecha;
  final String? motivo;
}

@JsonSerializable(createToJson: false)
class AusenciaDto {
  const AusenciaDto({
    required this.id,
    required this.profesionalId,
    required this.profesional,
    required this.tipo,
    required this.desde,
    required this.hasta,
    required this.dias,
    this.comentario,
    required this.estado,
    required this.fechaSolicitud,
    required this.solicitadaPor,
    required this.decisiones,
  });

  factory AusenciaDto.fromJson(Map<String, dynamic> json) => _$AusenciaDtoFromJson(json);

  final String id;
  final String profesionalId;
  final String profesional;
  final TipoAusencia tipo;
  final String desde;
  final String hasta;
  final int dias;
  final String? comentario;
  final EstadoAusencia estado;
  final DateTime fechaSolicitud;
  final String solicitadaPor;
  final List<DecisionDto> decisiones;

  bool get vigente => estado == EstadoAusencia.solicitada || estado == EstadoAusencia.aprobada;
}

@JsonSerializable(createToJson: false)
class TurnoComprometidoDto {
  const TurnoComprometidoDto({
    required this.asignacionId,
    required this.turnoId,
    required this.necesidadId,
    required this.fecha,
    required this.turno,
    required this.servicio,
    required this.centro,
    required this.cuadranteEstado,
  });

  factory TurnoComprometidoDto.fromJson(Map<String, dynamic> json) => _$TurnoComprometidoDtoFromJson(json);

  final String asignacionId;
  final String turnoId;
  final String necesidadId;
  final String fecha;
  final String turno;
  final String servicio;
  final String centro;
  final EstadoCuadrante cuadranteEstado;
}

@JsonSerializable(createToJson: false)
class ResultadoAusenciaDto {
  const ResultadoAusenciaDto({required this.ausencia, required this.vacantes});

  factory ResultadoAusenciaDto.fromJson(Map<String, dynamic> json) => _$ResultadoAusenciaDtoFromJson(json);

  final AusenciaDto ausencia;
  final List<TurnoComprometidoDto> vacantes;
}

@JsonSerializable(createToJson: false)
class DisponibilidadDto {
  const DisponibilidadDto({
    required this.id,
    required this.profesionalId,
    required this.profesional,
    required this.tipo,
    required this.desde,
    required this.hasta,
    required this.dias,
    this.tipoTurnoId,
    this.tipoTurno,
    this.comentario,
    required this.estado,
    required this.fechaDeclaracion,
    this.motivoDecision,
  });

  factory DisponibilidadDto.fromJson(Map<String, dynamic> json) => _$DisponibilidadDtoFromJson(json);

  final String id;
  final String profesionalId;
  final String profesional;
  final TipoDisponibilidad tipo;
  final String desde;
  final String hasta;
  final int dias;
  final String? tipoTurnoId;
  final String? tipoTurno;
  final String? comentario;
  final EstadoDisponibilidad estado;
  final DateTime fechaDeclaracion;
  final String? motivoDecision;
}

@JsonSerializable(createToJson: false)
class TipoTurnoPropioDto {
  const TipoTurnoPropioDto({required this.id, required this.nombre, required this.franja, required this.servicio, required this.centro});

  factory TipoTurnoPropioDto.fromJson(Map<String, dynamic> json) => _$TipoTurnoPropioDtoFromJson(json);

  final String id;
  final String nombre;
  final String franja;
  final String servicio;
  final String centro;
}

@JsonSerializable(createToJson: false)
class VacanteDto {
  const VacanteDto({
    required this.id,
    required this.estado,
    required this.motivo,
    required this.fechaCreacion,
    this.fechaCierre,
    required this.necesidadId,
    required this.turnoId,
    required this.fecha,
    required this.turno,
    required this.servicio,
    required this.centro,
    this.categoria,
    required this.profesionalAusente,
    this.ausenciaId,
    required this.cuadranteEstado,
  });

  factory VacanteDto.fromJson(Map<String, dynamic> json) => _$VacanteDtoFromJson(json);

  final String id;
  final EstadoVacante estado;
  final String motivo;
  final DateTime fechaCreacion;
  final DateTime? fechaCierre;
  final String necesidadId;
  final String turnoId;
  final String fecha;
  final String turno;
  final String servicio;
  final String centro;
  final String? categoria;
  final String profesionalAusente;
  final String? ausenciaId;
  final EstadoCuadrante cuadranteEstado;
}

class AusenciasRepositorio {
  AusenciasRepositorio(this._dio);
  final Dio _dio;

  // ── Propias ──
  Future<List<AusenciaDto>> mias() => protegerApi(() async => _lista(await _dio.get<List<dynamic>>('/api/mis-ausencias'), AusenciaDto.fromJson));

  Future<void> solicitar({required TipoAusencia tipo, required DateTime desde, required DateTime hasta, String? comentario}) => protegerApi(() => _dio.post<void>(
      '/api/mis-ausencias', data: {'tipo': tipo.valorApi, 'desde': fechaParaApi(desde), 'hasta': fechaParaApi(hasta), 'comentario': comentario}));

  Future<void> cancelarPropia(String id) => protegerApi(() => _dio.post<void>('/api/mis-ausencias/$id/cancelar'));

  Future<List<DisponibilidadDto>> miDisponibilidad() =>
      protegerApi(() async => _lista(await _dio.get<List<dynamic>>('/api/mi-disponibilidad'), DisponibilidadDto.fromJson));

  Future<List<TipoTurnoPropioDto>> misTiposTurno() =>
      protegerApi(() async => _lista(await _dio.get<List<dynamic>>('/api/mis-tipos-turno'), TipoTurnoPropioDto.fromJson));

  Future<void> declarar({
    required TipoDisponibilidad tipo,
    required DateTime desde,
    required DateTime hasta,
    required int dias,
    String? tipoTurnoId,
    String? comentario,
  }) =>
      protegerApi(() => _dio.post<void>('/api/mi-disponibilidad', data: {
            'tipo': tipo.valorApi,
            'desde': fechaParaApi(desde),
            'hasta': fechaParaApi(hasta),
            'dias': dias,
            'tipoTurnoId': tipoTurnoId,
            'comentario': comentario,
          }));

  Future<void> retirarDeclaracion(String id) => protegerApi(() => _dio.delete<void>('/api/mi-disponibilidad/$id'));

  // ── Gestión ──
  Future<List<AusenciaDto>> ausencias({EstadoAusencia? estado}) => protegerApi(() async => _lista(
      await _dio.get<List<dynamic>>('/api/ausencias', queryParameters: {'estado': ?_nombreEstado(estado)}), AusenciaDto.fromJson));

  Future<List<TurnoComprometidoDto>> comprometidos(String id) =>
      protegerApi(() async => _lista(await _dio.get<List<dynamic>>('/api/ausencias/$id/comprometidos'), TurnoComprometidoDto.fromJson));

  Future<ResultadoAusenciaDto> aprobar(String id, {String? motivo}) => protegerApi(() async {
        final r = await _dio.post<Map<String, dynamic>>('/api/ausencias/$id/aprobar', data: {'motivo': motivo});
        return ResultadoAusenciaDto.fromJson(r.data!);
      });

  Future<void> rechazar(String id, String motivo) => protegerApi(() => _dio.post<void>('/api/ausencias/$id/rechazar', data: {'motivo': motivo}));

  Future<void> cancelar(String id, {String? motivo}) => protegerApi(() => _dio.post<void>('/api/ausencias/$id/cancelar', data: {'motivo': motivo}));

  Future<ResultadoAusenciaDto> registrar({
    required String profesionalId,
    required TipoAusencia tipo,
    required DateTime desde,
    required DateTime hasta,
    String? comentario,
    required bool aprobar,
  }) =>
      protegerApi(() async {
        final r = await _dio.post<Map<String, dynamic>>('/api/ausencias', data: {
          'profesionalId': profesionalId,
          'tipo': tipo.valorApi,
          'desde': fechaParaApi(desde),
          'hasta': fechaParaApi(hasta),
          'comentario': comentario,
          'aprobar': aprobar,
        });
        return ResultadoAusenciaDto.fromJson(r.data!);
      });

  Future<List<DisponibilidadDto>> disponibilidades({EstadoDisponibilidad? estado}) => protegerApi(() async => _lista(
      await _dio.get<List<dynamic>>('/api/disponibilidades', queryParameters: {'estado': ?(estado == null ? null : _cap(estado.name))}),
      DisponibilidadDto.fromJson));

  Future<void> decidirDisponibilidad(String id, {required bool aprobar, String? motivo}) =>
      protegerApi(() => _dio.post<void>('/api/disponibilidades/$id/${aprobar ? 'aprobar' : 'rechazar'}', data: {'motivo': motivo}));

  Future<void> eliminarDisponibilidad(String id) => protegerApi(() => _dio.delete<void>('/api/disponibilidades/$id'));

  Future<List<VacanteDto>> vacantes({EstadoVacante? estado}) => protegerApi(() async => _lista(
      await _dio.get<List<dynamic>>('/api/vacantes', queryParameters: {'estado': ?(estado == null ? null : _cap(estado.name))}), VacanteDto.fromJson));

  Future<ResultadoAsignacionDto> cubrirVacante(String id, String profesionalId, {String? justificacion}) => protegerApi(() async {
        final r = await _dio.post<Map<String, dynamic>>('/api/vacantes/$id/cubrir', data: {
          'profesionalId': profesionalId,
          if (justificacion != null) 'excepcion': {'justificacion': justificacion},
        });
        return ResultadoAsignacionDto.fromJson(r.data!);
      });

  static List<T> _lista<T>(Response<List<dynamic>> r, T Function(Map<String, dynamic>) desde) => [for (final j in r.data!) desde(j as Map<String, dynamic>)];
  static String _cap(String s) => s[0].toUpperCase() + s.substring(1);
  static String? _nombreEstado(EstadoAusencia? e) => e == null ? null : _cap(e.name);
}

final ausenciasRepositorioProvider = Provider((ref) => AusenciasRepositorio(ref.watch(clienteApiProvider)));

final misAusenciasProvider = FutureProvider.autoDispose<List<AusenciaDto>>((ref) => ref.watch(ausenciasRepositorioProvider).mias());
final miDisponibilidadProvider = FutureProvider.autoDispose<List<DisponibilidadDto>>((ref) => ref.watch(ausenciasRepositorioProvider).miDisponibilidad());
final misTiposTurnoProvider = FutureProvider.autoDispose<List<TipoTurnoPropioDto>>((ref) => ref.watch(ausenciasRepositorioProvider).misTiposTurno());

/// `null` = todas las ausencias.
final ausenciasProvider = FutureProvider.autoDispose.family<List<AusenciaDto>, EstadoAusencia?>(
  (ref, estado) => ref.watch(ausenciasRepositorioProvider).ausencias(estado: estado),
);

final comprometidosProvider = FutureProvider.autoDispose.family<List<TurnoComprometidoDto>, String>(
  (ref, id) => ref.watch(ausenciasRepositorioProvider).comprometidos(id),
);

final disponibilidadesPendientesProvider = FutureProvider.autoDispose<List<DisponibilidadDto>>(
  (ref) => ref.watch(ausenciasRepositorioProvider).disponibilidades(estado: EstadoDisponibilidad.declarada),
);

final vacantesProvider = FutureProvider.autoDispose.family<List<VacanteDto>, EstadoVacante?>(
  (ref, estado) => ref.watch(ausenciasRepositorioProvider).vacantes(estado: estado),
);
