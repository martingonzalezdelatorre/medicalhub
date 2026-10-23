import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';

part 'planificacion_repositorio.g.dart';

enum EstadoCuadrante {
  @JsonValue('Borrador')
  borrador,
  @JsonValue('Publicado')
  publicado,
  @JsonValue('Archivado')
  archivado,
}

/// Fecha «yyyy-MM-dd» de la API → fecha de calendario (sin hora ni zona).
DateTime fechaDeApi(String iso) => DateTime.parse(iso);

String fechaParaApi(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

@JsonSerializable(createToJson: false)
class CuadranteDto {
  const CuadranteDto({
    required this.id,
    required this.servicioId,
    required this.servicioNombre,
    required this.centroId,
    required this.centroNombre,
    required this.nombre,
    required this.desde,
    required this.hasta,
    required this.estado,
    required this.version,
    this.fechaPublicacion,
    required this.totalTurnos,
    required this.plazasTotales,
    required this.plazasCubiertas,
  });

  factory CuadranteDto.fromJson(Map<String, dynamic> json) => _$CuadranteDtoFromJson(json);

  final String id;
  final String servicioId;
  final String servicioNombre;
  final String centroId;
  final String centroNombre;
  final String nombre;
  final String desde;
  final String hasta;
  final EstadoCuadrante estado;
  final int version;
  final DateTime? fechaPublicacion;
  final int totalTurnos;
  final int plazasTotales;
  final int plazasCubiertas;

  int get plazasSinCubrir => plazasTotales - plazasCubiertas;
}

@JsonSerializable(createToJson: false)
class AsignacionDto {
  const AsignacionDto({required this.id, required this.profesionalId, required this.profesional, required this.estado});

  factory AsignacionDto.fromJson(Map<String, dynamic> json) => _$AsignacionDtoFromJson(json);

  final String id;
  final String profesionalId;
  final String profesional;
  final String estado;
}

@JsonSerializable(createToJson: false)
class NecesidadDto {
  const NecesidadDto({
    required this.id,
    required this.numeroPlazas,
    this.categoriaProfesionalId,
    this.categoria,
    required this.asignaciones,
  });

  factory NecesidadDto.fromJson(Map<String, dynamic> json) => _$NecesidadDtoFromJson(json);

  final String id;
  final int numeroPlazas;
  final String? categoriaProfesionalId;
  final String? categoria;
  final List<AsignacionDto> asignaciones;

  int get plazasLibres => numeroPlazas - asignaciones.length;
}

@JsonSerializable(createToJson: false)
class TurnoDto {
  const TurnoDto({
    required this.id,
    required this.cuadranteId,
    required this.cuadranteEstado,
    required this.fecha,
    required this.inicioLocal,
    required this.finLocal,
    required this.cruzaMedianoche,
    required this.duracionHoras,
    required this.centroId,
    required this.centro,
    required this.servicioId,
    required this.servicio,
    required this.tipoTurnoId,
    required this.tipoTurno,
    required this.esGuardia,
    required this.necesidades,
    required this.plazasTotales,
    required this.plazasCubiertas,
  });

  factory TurnoDto.fromJson(Map<String, dynamic> json) => _$TurnoDtoFromJson(json);

  final String id;
  final String cuadranteId;
  final EstadoCuadrante cuadranteEstado;
  final String fecha;
  final String inicioLocal;
  final String finLocal;
  final bool cruzaMedianoche;
  final double duracionHoras;
  final String centroId;
  final String centro;
  final String servicioId;
  final String servicio;
  final String tipoTurnoId;
  final String tipoTurno;
  final bool esGuardia;
  final List<NecesidadDto> necesidades;
  final int plazasTotales;
  final int plazasCubiertas;

  DateTime get dia => fechaDeApi(fecha);
  bool get sinCubrir => plazasCubiertas < plazasTotales;
  bool get editable => cuadranteEstado == EstadoCuadrante.borrador;
}

@JsonSerializable(createToJson: false)
class ViolacionDto {
  const ViolacionDto({
    required this.codigo,
    this.reglaId,
    required this.regla,
    required this.severidad,
    required this.mensaje,
    required this.permiteExcepcion,
    required this.requiereAdministrador,
  });

  factory ViolacionDto.fromJson(Map<String, dynamic> json) => _$ViolacionDtoFromJson(json);

  final String codigo;
  final String? reglaId;
  final String regla;
  final String severidad;
  final String mensaje;
  final bool permiteExcepcion;
  final bool requiereAdministrador;

  bool get bloquea => severidad == 'Bloqueante';
}

/// Resultado de asignar: las advertencias no bloquean, pero conviene revisarlas.
@JsonSerializable(createToJson: false)
class ResultadoAsignacionDto {
  const ResultadoAsignacionDto({required this.id, this.advertencias = const [], this.excepciones = const []});

  factory ResultadoAsignacionDto.fromJson(Map<String, dynamic> json) => _$ResultadoAsignacionDtoFromJson(json);

  final String id;
  final List<ViolacionDto> advertencias;
  final List<ViolacionDto> excepciones;
}

@JsonSerializable(createToJson: false)
class CandidatoDto {
  const CandidatoDto({
    required this.profesionalId,
    required this.nombreCompleto,
    required this.elegible,
    required this.libre,
    required this.motivos,
    this.violaciones = const [],
    this.requiereExcepcion = false,
    this.puedeAutorizar = false,
    this.notas = const [],
  });

  factory CandidatoDto.fromJson(Map<String, dynamic> json) => _$CandidatoDtoFromJson(json);

  final String profesionalId;
  final String nombreCompleto;
  final bool elegible;
  final bool libre;
  final List<String> motivos;
  final List<ViolacionDto> violaciones;

  /// Tiene infracciones bloqueantes, pero todas admiten una excepción justificada.
  final bool requiereExcepcion;

  /// El usuario actual tiene autoridad para autorizar esa excepción.
  final bool puedeAutorizar;

  /// Información positiva: «Prefiere este turno», «Ha declarado disponibilidad…».
  final List<String> notas;

  List<ViolacionDto> get advertencias => violaciones.where((v) => v.severidad != 'Bloqueante').toList();
}

@JsonSerializable(createToJson: false)
class IncidenciaDto {
  const IncidenciaDto({
    required this.severidad,
    required this.codigo,
    required this.mensaje,
    this.excepcionada = false,
    this.justificacion,
    this.autorizadaPor,
    this.asignacionId,
    this.permiteExcepcion = false,
  });

  factory IncidenciaDto.fromJson(Map<String, dynamic> json) => _$IncidenciaDtoFromJson(json);

  final String severidad;
  final String codigo;
  final String mensaje;

  final bool excepcionada;
  final String? justificacion;
  final String? autorizadaPor;
  final String? asignacionId;
  final bool permiteExcepcion;

  bool get bloqueante => severidad == 'Bloqueante';
}

@JsonSerializable(createToJson: false)
class ValidacionDto {
  const ValidacionDto({required this.puedePublicarse, required this.incidencias});

  factory ValidacionDto.fromJson(Map<String, dynamic> json) => _$ValidacionDtoFromJson(json);

  final bool puedePublicarse;
  final List<IncidenciaDto> incidencias;
}

@JsonSerializable(createToJson: false)
class VersionResumenDto {
  const VersionResumenDto({
    required this.numero,
    required this.fecha,
    this.usuario,
    required this.turnos,
    required this.plazasTotales,
    required this.plazasCubiertas,
  });

  factory VersionResumenDto.fromJson(Map<String, dynamic> json) => _$VersionResumenDtoFromJson(json);

  final int numero;
  final DateTime fecha;
  final String? usuario;
  final int turnos;
  final int plazasTotales;
  final int plazasCubiertas;
}

@JsonSerializable(createToJson: false)
class PlantillaDto {
  const PlantillaDto({required this.id, required this.servicioId, required this.nombre, required this.diasPeriodo, required this.lineas});

  factory PlantillaDto.fromJson(Map<String, dynamic> json) => _$PlantillaDtoFromJson(json);

  final String id;
  final String servicioId;
  final String nombre;
  final int diasPeriodo;
  final int lineas;
}

/// Filtros de la consulta de calendario (comparables para usarlos como clave de proveedor).
class FiltroCalendario {
  const FiltroCalendario({
    required this.desde,
    required this.hasta,
    this.centroId,
    this.servicioId,
    this.cuadranteId,
    this.soloSinCubrir = false,
    this.propia = false,
  });

  final DateTime desde;
  final DateTime hasta;
  final String? centroId;
  final String? servicioId;
  final String? cuadranteId;
  final bool soloSinCubrir;

  /// true = «mi planificación» (profesional conectado).
  final bool propia;

  @override
  bool operator ==(Object other) =>
      other is FiltroCalendario &&
      other.desde == desde &&
      other.hasta == hasta &&
      other.centroId == centroId &&
      other.servicioId == servicioId &&
      other.cuadranteId == cuadranteId &&
      other.soloSinCubrir == soloSinCubrir &&
      other.propia == propia;

  @override
  int get hashCode => Object.hash(desde, hasta, centroId, servicioId, cuadranteId, soloSinCubrir, propia);
}

class PlanificacionRepositorio {
  PlanificacionRepositorio(this._dio);
  final Dio _dio;

  // ── Cuadrantes ──
  Future<List<CuadranteDto>> cuadrantes() => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/cuadrantes');
        return [for (final j in r.data!) CuadranteDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<CuadranteDto> cuadrante(String id) => protegerApi(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/cuadrantes/$id');
        return CuadranteDto.fromJson(r.data!);
      });

  Future<CuadranteDto> crear({required String servicioId, String? nombre, required DateTime desde, required DateTime hasta, required bool generarTurnos}) =>
      protegerApi(() async {
        final r = await _dio.post<Map<String, dynamic>>('/api/cuadrantes', data: {
          'servicioId': servicioId,
          'nombre': nombre,
          'desde': fechaParaApi(desde),
          'hasta': fechaParaApi(hasta),
          'generarTurnos': generarTurnos,
        });
        return CuadranteDto.fromJson(r.data!);
      });

  Future<CuadranteDto> desdePlantilla({required String plantillaId, required DateTime desde, String? nombre}) => protegerApi(() async {
        final r = await _dio.post<Map<String, dynamic>>('/api/cuadrantes/desde-plantilla',
            data: {'plantillaId': plantillaId, 'desde': fechaParaApi(desde), 'nombre': nombre});
        return CuadranteDto.fromJson(r.data!);
      });

  Future<ValidacionDto> validar(String id) => protegerApi(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/cuadrantes/$id/validacion');
        return ValidacionDto.fromJson(r.data!);
      });

  Future<void> publicar(String id) => protegerApi(() => _dio.post<void>('/api/cuadrantes/$id/publicar'));
  Future<void> reabrir(String id) => protegerApi(() => _dio.post<void>('/api/cuadrantes/$id/reabrir'));
  Future<void> archivar(String id) => protegerApi(() => _dio.post<void>('/api/cuadrantes/$id/archivar'));
  Future<void> eliminar(String id) => protegerApi(() => _dio.delete<void>('/api/cuadrantes/$id'));

  Future<List<VersionResumenDto>> versiones(String id) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/cuadrantes/$id/versiones');
        return [for (final j in r.data!) VersionResumenDto.fromJson(j as Map<String, dynamic>)];
      });

  // ── Plantillas ──
  Future<List<PlantillaDto>> plantillas() => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/plantillas-cuadrante');
        return [for (final j in r.data!) PlantillaDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<void> guardarPlantilla(String cuadranteId, String nombre) =>
      protegerApi(() => _dio.post<void>('/api/cuadrantes/$cuadranteId/plantilla', data: {'nombre': nombre}));

  Future<void> eliminarPlantilla(String id) => protegerApi(() => _dio.delete<void>('/api/plantillas-cuadrante/$id'));

  // ── Turnos y asignaciones ──
  Future<List<TurnoDto>> turnos(FiltroCalendario f) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>(f.propia ? '/api/mi-planificacion' : '/api/turnos', queryParameters: {
          'desde': fechaParaApi(f.desde),
          'hasta': fechaParaApi(f.hasta),
          if (!f.propia) ...{
            'centroId': ?f.centroId,
            'servicioId': ?f.servicioId,
            'cuadranteId': ?f.cuadranteId,
            'soloSinCubrir': f.soloSinCubrir,
          },
        });
        return [for (final j in r.data!) TurnoDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<void> crearTurno(String cuadranteId, DateTime fecha, String tipoTurnoId) => protegerApi(
      () => _dio.post<void>('/api/cuadrantes/$cuadranteId/turnos', data: {'fecha': fechaParaApi(fecha), 'tipoTurnoId': tipoTurnoId}));

  Future<void> eliminarTurno(String id) => protegerApi(() => _dio.delete<void>('/api/turnos/$id'));

  Future<List<CandidatoDto>> candidatos(String necesidadId) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/necesidades/$necesidadId/candidatos');
        return [for (final j in r.data!) CandidatoDto.fromJson(j as Map<String, dynamic>)];
      });

  /// Asigna un profesional. Si incumple reglas bloqueantes que admiten excepción hay que enviar la justificación.
  Future<ResultadoAsignacionDto> asignar(String necesidadId, String profesionalId, {String? justificacion}) => protegerApi(() async {
        final r = await _dio.post<Map<String, dynamic>>('/api/necesidades/$necesidadId/asignaciones', data: {
          'profesionalId': profesionalId,
          if (justificacion != null) 'excepcion': {'justificacion': justificacion},
        });
        return ResultadoAsignacionDto.fromJson(r.data!);
      });

  /// Autoriza a posteriori las infracciones bloqueantes de una asignación ya hecha.
  Future<void> autorizarExcepcion(String asignacionId, String justificacion) => protegerApi(
      () => _dio.post<void>('/api/asignaciones/$asignacionId/excepciones', data: {'justificacion': justificacion}));


  Future<void> quitarAsignacion(String id) => protegerApi(() => _dio.delete<void>('/api/asignaciones/$id'));

  Future<Uint8List> exportarCsv(String id) => protegerApi(() async {
        final r = await _dio.get<List<int>>('/api/cuadrantes/$id/exportar.csv', options: Options(responseType: ResponseType.bytes));
        return Uint8List.fromList(r.data!);
      });
}

final planificacionRepositorioProvider = Provider((ref) => PlanificacionRepositorio(ref.watch(clienteApiProvider)));

final cuadrantesProvider = FutureProvider.autoDispose<List<CuadranteDto>>((ref) => ref.watch(planificacionRepositorioProvider).cuadrantes());

final cuadranteProvider = FutureProvider.autoDispose.family<CuadranteDto, String>(
  (ref, id) => ref.watch(planificacionRepositorioProvider).cuadrante(id),
);

final versionesProvider = FutureProvider.autoDispose.family<List<VersionResumenDto>, String>(
  (ref, id) => ref.watch(planificacionRepositorioProvider).versiones(id),
);

final plantillasProvider = FutureProvider.autoDispose<List<PlantillaDto>>((ref) => ref.watch(planificacionRepositorioProvider).plantillas());

final turnosProvider = FutureProvider.autoDispose.family<List<TurnoDto>, FiltroCalendario>(
  (ref, f) => ref.watch(planificacionRepositorioProvider).turnos(f),
);
