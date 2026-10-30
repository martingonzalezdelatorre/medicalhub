import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';

/// Coste de un turno para un perfil profesional (importe en euros).
class TarifaDto {
  const TarifaDto({required this.tipoTurnoId, required this.categoriaProfesionalId, required this.importe});

  factory TarifaDto.fromJson(Map<String, dynamic> json) => TarifaDto(
        tipoTurnoId: json['tipoTurnoId'] as String,
        categoriaProfesionalId: json['categoriaProfesionalId'] as String,
        importe: (json['importe'] as num).toDouble(),
      );

  final String tipoTurnoId;
  final String categoriaProfesionalId;
  final double importe;
}

class TarifasRepositorio {
  TarifasRepositorio(this._dio);
  final Dio _dio;

  Future<List<TarifaDto>> listar(String servicioId) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/servicios/$servicioId/tarifas');
        return [for (final j in r.data!) TarifaDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<void> guardar(String servicioId, List<TarifaDto> tarifas) => protegerApi(() => _dio.put<void>(
        '/api/servicios/$servicioId/tarifas',
        data: {
          'tarifas': [
            for (final t in tarifas)
              {'tipoTurnoId': t.tipoTurnoId, 'categoriaProfesionalId': t.categoriaProfesionalId, 'importe': t.importe},
          ],
        },
      ));
}

final tarifasRepositorioProvider = Provider((ref) => TarifasRepositorio(ref.watch(clienteApiProvider)));

final tarifasDeServicioProvider = FutureProvider.autoDispose.family<List<TarifaDto>, String>(
  (ref, servicioId) => ref.watch(tarifasRepositorioProvider).listar(servicioId),
);
