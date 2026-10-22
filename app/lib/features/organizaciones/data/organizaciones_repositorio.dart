import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';

part 'organizaciones_repositorio.g.dart';

@JsonSerializable(createToJson: false)
class OrganizacionDto {
  const OrganizacionDto({
    required this.id,
    required this.nombre,
    required this.zonaHoraria,
    required this.activa,
    required this.mfaObligatorio,
  });

  factory OrganizacionDto.fromJson(Map<String, dynamic> json) => _$OrganizacionDtoFromJson(json);

  final String id;
  final String nombre;
  final String zonaHoraria;
  final bool activa;
  final bool mfaObligatorio;
}

class OrganizacionesRepositorio {
  OrganizacionesRepositorio(this._dio);
  final Dio _dio;

  Future<List<OrganizacionDto>> listar() async {
    try {
      final r = await _dio.get<List<dynamic>>('/api/organizaciones');
      return [for (final j in r.data!) OrganizacionDto.fromJson(j as Map<String, dynamic>)];
    } catch (e) {
      throw FalloApi.desde(e);
    }
  }

  Future<void> crear(Map<String, dynamic> datos) async {
    try {
      await _dio.post<void>('/api/organizaciones', data: datos);
    } catch (e) {
      throw FalloApi.desde(e);
    }
  }
}

final organizacionesRepositorioProvider = Provider((ref) => OrganizacionesRepositorio(ref.watch(clienteApiProvider)));

final organizacionesProvider = FutureProvider.autoDispose<List<OrganizacionDto>>(
  (ref) => ref.watch(organizacionesRepositorioProvider).listar(),
);
