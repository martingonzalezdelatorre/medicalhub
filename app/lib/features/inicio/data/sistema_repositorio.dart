import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';
import 'informacion_sistema.dart';

class SistemaRepositorio {
  SistemaRepositorio(this._dio);
  final Dio _dio;

  Future<InformacionSistema> obtenerInfo() async {
    try {
      final r = await _dio.get<Map<String, dynamic>>('/api/sistema/info');
      return InformacionSistema.fromJson(r.data!);
    } catch (e) {
      throw FalloApi.desde(e);
    }
  }
}

final sistemaRepositorioProvider = Provider((ref) => SistemaRepositorio(ref.watch(clienteApiProvider)));
