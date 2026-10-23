import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';
import '../../planificacion/data/planificacion_repositorio.dart' show EstadoCuadrante, fechaParaApi;

part 'informes_repositorio.g.dart';

// ── Panel ──
@JsonSerializable(createToJson: false)
class TurnoSinCubrirDto {
  const TurnoSinCubrirDto({required this.turnoId, required this.fecha, required this.turno, required this.servicio, required this.centro, required this.plazasLibres});
  factory TurnoSinCubrirDto.fromJson(Map<String, dynamic> json) => _$TurnoSinCubrirDtoFromJson(json);

  final String turnoId;
  final String fecha;
  final String turno;
  final String servicio;
  final String centro;
  final int plazasLibres;
}

@JsonSerializable(createToJson: false)
class PendientesGestionDto {
  const PendientesGestionDto({
    required this.vacantesAbiertas,
    required this.ausenciasSolicitadas,
    required this.ofertasPorAprobar,
    required this.disponibilidadesPendientes,
    required this.excepcionesUltimos30Dias,
  });
  factory PendientesGestionDto.fromJson(Map<String, dynamic> json) => _$PendientesGestionDtoFromJson(json);

  final int vacantesAbiertas;
  final int ausenciasSolicitadas;
  final int ofertasPorAprobar;
  final int disponibilidadesPendientes;
  final int excepcionesUltimos30Dias;
}

@JsonSerializable(createToJson: false)
class DashboardGestionDto {
  const DashboardGestionDto({
    required this.desde,
    required this.hasta,
    required this.plazasTotales,
    required this.plazasCubiertas,
    required this.cuadrantesBorrador,
    required this.proximosSinCubrir,
    this.pendientes,
  });
  factory DashboardGestionDto.fromJson(Map<String, dynamic> json) => _$DashboardGestionDtoFromJson(json);

  final String desde;
  final String hasta;
  final int plazasTotales;
  final int plazasCubiertas;
  final int cuadrantesBorrador;
  final List<TurnoSinCubrirDto> proximosSinCubrir;
  final PendientesGestionDto? pendientes;

  double get porcentaje => plazasTotales == 0 ? 100 : 100 * plazasCubiertas / plazasTotales;
}

@JsonSerializable(createToJson: false)
class ProximoTurnoDto {
  const ProximoTurnoDto({
    required this.asignacionId,
    required this.fecha,
    required this.turno,
    required this.servicio,
    required this.centro,
    required this.cuadranteEstado,
  });
  factory ProximoTurnoDto.fromJson(Map<String, dynamic> json) => _$ProximoTurnoDtoFromJson(json);

  final String asignacionId;
  final String fecha;
  final String turno;
  final String servicio;
  final String centro;
  final EstadoCuadrante cuadranteEstado;
}

@JsonSerializable(createToJson: false)
class DashboardProfesionalDto {
  const DashboardProfesionalDto({required this.proximosTurnos, required this.horasPlanificadasMesMin, required this.ofertasPendientes, required this.ausenciasSolicitadas});
  factory DashboardProfesionalDto.fromJson(Map<String, dynamic> json) => _$DashboardProfesionalDtoFromJson(json);

  final List<ProximoTurnoDto> proximosTurnos;
  final int horasPlanificadasMesMin;
  final int ofertasPendientes;
  final int ausenciasSolicitadas;
}

@JsonSerializable(createToJson: false)
class DashboardDto {
  const DashboardDto({this.gestion, this.profesional});
  factory DashboardDto.fromJson(Map<String, dynamic> json) => _$DashboardDtoFromJson(json);

  final DashboardGestionDto? gestion;
  final DashboardProfesionalDto? profesional;
}

// ── Horas ──
@JsonSerializable(createToJson: false)
class AjusteHorasDto {
  const AjusteHorasDto({required this.minutos, required this.motivo, required this.usuario, required this.fecha});
  factory AjusteHorasDto.fromJson(Map<String, dynamic> json) => _$AjusteHorasDtoFromJson(json);

  final int minutos;
  final String motivo;
  final String usuario;
  final DateTime fecha;
}

@JsonSerializable(createToJson: false)
class LineaHorasDto {
  const LineaHorasDto({
    required this.asignacionId,
    required this.profesionalId,
    required this.profesional,
    required this.fecha,
    required this.turno,
    required this.servicio,
    required this.centro,
    required this.esGuardia,
    required this.inicioUtc,
    required this.finUtc,
    required this.planificadasMin,
    required this.nocturnasPlanMin,
    this.registroId,
    this.inicioRealUtc,
    this.finRealUtc,
    this.realesMin,
    this.nocturnasRealMin,
    required this.ajustesMin,
    this.efectivasMin,
    this.ajustes = const [],
  });
  factory LineaHorasDto.fromJson(Map<String, dynamic> json) => _$LineaHorasDtoFromJson(json);

  final String asignacionId;
  final String profesionalId;
  final String profesional;
  final String fecha;
  final String turno;
  final String servicio;
  final String centro;
  final bool esGuardia;
  final DateTime inicioUtc;
  final DateTime finUtc;
  final int planificadasMin;
  final int nocturnasPlanMin;
  final String? registroId;
  final DateTime? inicioRealUtc;
  final DateTime? finRealUtc;
  final int? realesMin;
  final int? nocturnasRealMin;
  final int ajustesMin;
  final int? efectivasMin;
  final List<AjusteHorasDto> ajustes;

  bool get registrada => registroId != null;
}

@JsonSerializable(createToJson: false)
class FilaInformeHorasDto {
  const FilaInformeHorasDto({
    required this.profesionalId,
    required this.profesional,
    required this.turnos,
    required this.guardias,
    required this.planificadasMin,
    required this.nocturnasPlanMin,
    required this.turnosRegistrados,
    required this.realesMin,
    required this.nocturnasRealMin,
    required this.ajustesMin,
    required this.efectivasMin,
  });
  factory FilaInformeHorasDto.fromJson(Map<String, dynamic> json) => _$FilaInformeHorasDtoFromJson(json);

  final String profesionalId;
  final String profesional;
  final int turnos;
  final int guardias;
  final int planificadasMin;
  final int nocturnasPlanMin;
  final int turnosRegistrados;
  final int realesMin;
  final int nocturnasRealMin;
  final int ajustesMin;
  final int efectivasMin;
}

@JsonSerializable(createToJson: false)
class InformeHorasDto {
  const InformeHorasDto({required this.filas, required this.total});
  factory InformeHorasDto.fromJson(Map<String, dynamic> json) => _$InformeHorasDtoFromJson(json);

  final List<FilaInformeHorasDto> filas;
  final FilaInformeHorasDto total;
}

// ── Cobertura ──
@JsonSerializable(createToJson: false)
class FilaInformeCoberturaDto {
  const FilaInformeCoberturaDto({
    required this.servicio,
    required this.centro,
    required this.plazas,
    required this.cubiertas,
    required this.libres,
    required this.porcentajeCobertura,
    required this.vacantesAbiertas,
    required this.excepciones,
  });
  factory FilaInformeCoberturaDto.fromJson(Map<String, dynamic> json) => _$FilaInformeCoberturaDtoFromJson(json);

  final String servicio;
  final String centro;
  final int plazas;
  final int cubiertas;
  final int libres;
  final double porcentajeCobertura;
  final int vacantesAbiertas;
  final int excepciones;
}

@JsonSerializable(createToJson: false)
class InformeCoberturaDto {
  const InformeCoberturaDto({required this.filas, required this.total});
  factory InformeCoberturaDto.fromJson(Map<String, dynamic> json) => _$InformeCoberturaDtoFromJson(json);

  final List<FilaInformeCoberturaDto> filas;
  final FilaInformeCoberturaDto total;
}

/// Periodo y filtros de los informes.
class FiltroInforme {
  const FiltroInforme({required this.desde, required this.hasta, this.servicioId, this.centroId});
  final DateTime desde;
  final DateTime hasta;
  final String? servicioId;
  final String? centroId;

  Map<String, dynamic> get consulta => {
        'desde': fechaParaApi(desde),
        'hasta': fechaParaApi(hasta),
        'servicioId': ?servicioId,
        'centroId': ?centroId,
      };

  @override
  bool operator ==(Object other) =>
      other is FiltroInforme && other.desde == desde && other.hasta == hasta && other.servicioId == servicioId && other.centroId == centroId;

  @override
  int get hashCode => Object.hash(desde, hasta, servicioId, centroId);
}

class InformesRepositorio {
  InformesRepositorio(this._dio);
  final Dio _dio;

  Future<DashboardDto> dashboard() => protegerApi(() async => DashboardDto.fromJson((await _dio.get<Map<String, dynamic>>('/api/dashboard')).data!));

  Future<InformeHorasDto> informeHoras(FiltroInforme f) =>
      protegerApi(() async => InformeHorasDto.fromJson((await _dio.get<Map<String, dynamic>>('/api/informes/horas', queryParameters: f.consulta)).data!));

  Future<InformeCoberturaDto> informeCobertura(FiltroInforme f) => protegerApi(
      () async => InformeCoberturaDto.fromJson((await _dio.get<Map<String, dynamic>>('/api/informes/cobertura', queryParameters: f.consulta)).data!));

  Future<List<LineaHorasDto>> lineas(FiltroInforme f) => protegerApi(() async => _lista(await _dio.get<List<dynamic>>('/api/horas', queryParameters: f.consulta)));

  Future<List<LineaHorasDto>> mias(DateTime desde, DateTime hasta) => protegerApi(() async =>
      _lista(await _dio.get<List<dynamic>>('/api/mis-horas', queryParameters: {'desde': fechaParaApi(desde), 'hasta': fechaParaApi(hasta)})));

  Future<void> registrarHoras(String asignacionId, DateTime inicioUtc, DateTime finUtc) => protegerApi(() => _dio.put<void>('/api/asignaciones/$asignacionId/horas',
      data: {'inicioUtc': inicioUtc.toUtc().toIso8601String(), 'finUtc': finUtc.toUtc().toIso8601String()}));

  Future<void> ajustarHoras(String asignacionId, int minutos, String motivo) =>
      protegerApi(() => _dio.post<void>('/api/asignaciones/$asignacionId/horas/ajustes', data: {'minutos': minutos, 'motivo': motivo}));

  Future<Uint8List> csv(String informe, FiltroInforme f) => protegerApi(() async {
        final r = await _dio.get<List<int>>('/api/informes/$informe.csv', queryParameters: f.consulta, options: Options(responseType: ResponseType.bytes));
        return Uint8List.fromList(r.data!);
      });

  static List<LineaHorasDto> _lista(Response<List<dynamic>> r) => [for (final j in r.data!) LineaHorasDto.fromJson(j as Map<String, dynamic>)];
}

final informesRepositorioProvider = Provider((ref) => InformesRepositorio(ref.watch(clienteApiProvider)));

final dashboardProvider = FutureProvider.autoDispose<DashboardDto>((ref) => ref.watch(informesRepositorioProvider).dashboard(), retry: (_, _) => null);
final informeHorasProvider = FutureProvider.autoDispose.family<InformeHorasDto, FiltroInforme>((ref, f) => ref.watch(informesRepositorioProvider).informeHoras(f));
final informeCoberturaProvider =
    FutureProvider.autoDispose.family<InformeCoberturaDto, FiltroInforme>((ref, f) => ref.watch(informesRepositorioProvider).informeCobertura(f));
final lineasHorasProvider = FutureProvider.autoDispose.family<List<LineaHorasDto>, FiltroInforme>((ref, f) => ref.watch(informesRepositorioProvider).lineas(f));
final misHorasProvider = FutureProvider.autoDispose<List<LineaHorasDto>>((ref) {
  final hoy = DateTime.now();
  return ref.watch(informesRepositorioProvider).mias(hoy.subtract(const Duration(days: 30)), hoy);
});

/// «7 h 30 min» (o «7 h» si no hay minutos); admite negativos.
String formatearMinutos(int minutos) {
  final signo = minutos < 0 ? '−' : '';
  final m = minutos.abs();
  final h = m ~/ 60;
  final r = m % 60;
  return r == 0 ? '$signo$h h' : '$signo$h h $r min';
}
