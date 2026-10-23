import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';
import '../../planificacion/data/planificacion_repositorio.dart' show fechaParaApi;
import 'servicio_ubicacion.dart';

part 'fichaje_repositorio.g.dart';

enum EstadoFichaje {
  @JsonValue('EnCurso')
  enCurso,
  @JsonValue('EnPausa')
  enPausa,
  @JsonValue('Cerrado')
  cerrado,
  @JsonValue('Descartado')
  descartado,
}

@JsonSerializable(createToJson: false)
class PausaFichajeDto {
  const PausaFichajeDto({required this.inicioUtc, this.finUtc});
  factory PausaFichajeDto.fromJson(Map<String, dynamic> json) => _$PausaFichajeDtoFromJson(json);

  final DateTime inicioUtc;
  final DateTime? finUtc;
}

@JsonSerializable(createToJson: false)
class FichajeDto {
  const FichajeDto({
    required this.id,
    required this.asignacionId,
    required this.estado,
    required this.turno,
    required this.centro,
    required this.centroConGeovalla,
    required this.inicioUtc,
    this.finUtc,
    required this.minutosPausa,
    required this.minutosTrabajados,
    this.entradaFueraDeZona,
    this.salidaFueraDeZona,
    this.entradaSinUbicacion = false,
    this.salidaSinUbicacion,
    this.pausas = const [],
  });
  factory FichajeDto.fromJson(Map<String, dynamic> json) => _$FichajeDtoFromJson(json);

  final String id;
  final String asignacionId;
  final EstadoFichaje estado;
  final String turno;
  final String centro;
  final bool centroConGeovalla;
  final DateTime inicioUtc;
  final DateTime? finUtc;
  final int minutosPausa;
  final int minutosTrabajados;
  final bool? entradaFueraDeZona;
  final bool? salidaFueraDeZona;
  final bool entradaSinUbicacion;
  final bool? salidaSinUbicacion;
  final List<PausaFichajeDto> pausas;

  bool get abierto => estado == EstadoFichaje.enCurso || estado == EstadoFichaje.enPausa;
  bool get fueraDeZona => entradaFueraDeZona == true || salidaFueraDeZona == true;

  /// Minutos trabajados a esta hora (la API los calcula en el momento de la consulta; aquí se mantienen al día mientras la pantalla está abierta).
  int trabajadosA(DateTime ahora) {
    if (!abierto) return minutosTrabajados;
    final transcurrido = ahora.toUtc().difference(inicioUtc).inMinutes;
    var pausa = 0;
    for (final p in pausas) {
      pausa += (p.finUtc ?? ahora.toUtc()).difference(p.inicioUtc).inMinutes;
    }
    final v = transcurrido - pausa;
    return v < 0 ? 0 : v;
  }

  int pausaA(DateTime ahora) {
    if (!abierto) return minutosPausa;
    var pausa = 0;
    for (final p in pausas) {
      pausa += (p.finUtc ?? ahora.toUtc()).difference(p.inicioUtc).inMinutes;
    }
    return pausa;
  }
}

@JsonSerializable(createToJson: false)
class TurnoFichableDto {
  const TurnoFichableDto({required this.asignacionId, required this.turno, required this.centro, required this.centroConGeovalla, required this.inicioUtc, required this.finUtc});
  factory TurnoFichableDto.fromJson(Map<String, dynamic> json) => _$TurnoFichableDtoFromJson(json);

  final String asignacionId;
  final String turno;
  final String centro;
  final bool centroConGeovalla;
  final DateTime inicioUtc;
  final DateTime finUtc;
}

@JsonSerializable(createToJson: false)
class EstadoFichajeDto {
  const EstadoFichajeDto({this.actual, this.fichable});
  factory EstadoFichajeDto.fromJson(Map<String, dynamic> json) => _$EstadoFichajeDtoFromJson(json);

  final FichajeDto? actual;
  final TurnoFichableDto? fichable;
}

class FichajeRepositorio {
  FichajeRepositorio(this._dio);
  final Dio _dio;

  Future<EstadoFichajeDto> estado() =>
      protegerApi(() async => EstadoFichajeDto.fromJson((await _dio.get<Map<String, dynamic>>('/api/fichajes/estado')).data!));

  Future<FichajeDto> entrada(UbicacionDispositivo? u) => _fichar('entrada', u);

  Future<FichajeDto> salida(UbicacionDispositivo? u) => _fichar('salida', u);

  Future<FichajeDto> pausa() => _accion('pausa');

  Future<FichajeDto> reanudar() => _accion('reanudar');

  Future<List<FichajeDto>> mios(DateTime desde, DateTime hasta) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/mis-fichajes', queryParameters: {'desde': fechaParaApi(desde), 'hasta': fechaParaApi(hasta)});
        return [for (final j in r.data!) FichajeDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<FichajeDto> _fichar(String accion, UbicacionDispositivo? u) => protegerApi(() async => FichajeDto.fromJson((await _dio.post<Map<String, dynamic>>(
        '/api/fichajes/$accion',
        data: {'latitud': u?.latitud, 'longitud': u?.longitud, 'precisionMetros': u?.precisionMetros},
      ))
          .data!));

  Future<FichajeDto> _accion(String accion) =>
      protegerApi(() async => FichajeDto.fromJson((await _dio.post<Map<String, dynamic>>('/api/fichajes/$accion')).data!));
}

final fichajeRepositorioProvider = Provider((ref) => FichajeRepositorio(ref.watch(clienteApiProvider)));

final estadoFichajeProvider = FutureProvider.autoDispose<EstadoFichajeDto>((ref) => ref.watch(fichajeRepositorioProvider).estado(), retry: (_, _) => null);

final misFichajesProvider = FutureProvider.autoDispose<List<FichajeDto>>((ref) {
  final hoy = DateTime.now();
  return ref.watch(fichajeRepositorioProvider).mios(hoy.subtract(const Duration(days: 7)), hoy.add(const Duration(days: 1)));
}, retry: (_, _) => null);
