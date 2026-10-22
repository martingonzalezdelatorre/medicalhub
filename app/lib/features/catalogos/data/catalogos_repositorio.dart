import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';

part 'catalogos_repositorio.g.dart';

enum TipoCatalogo {
  especialidades('especialidades'),
  categorias('categorias-profesionales'),
  competencias('competencias');

  const TipoCatalogo(this.ruta);
  final String ruta;
}

@JsonSerializable(createToJson: false)
class CatalogoDto {
  const CatalogoDto({required this.id, required this.nombre, this.descripcion, required this.activa});

  factory CatalogoDto.fromJson(Map<String, dynamic> json) => _$CatalogoDtoFromJson(json);

  final String id;
  final String nombre;
  final String? descripcion;
  final bool activa;
}

class CatalogosRepositorio {
  CatalogosRepositorio(this._dio);
  final Dio _dio;

  Future<List<CatalogoDto>> listar(TipoCatalogo tipo, {bool incluirInactivas = false}) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/${tipo.ruta}', queryParameters: {'incluirInactivas': incluirInactivas});
        return [for (final j in r.data!) CatalogoDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<void> crear(TipoCatalogo tipo, String nombre, String? descripcion) =>
      protegerApi(() => _dio.post<void>('/api/${tipo.ruta}', data: {'nombre': nombre, 'descripcion': descripcion}));

  Future<void> actualizar(TipoCatalogo tipo, String id, String nombre, String? descripcion) =>
      protegerApi(() => _dio.put<void>('/api/${tipo.ruta}/$id', data: {'nombre': nombre, 'descripcion': descripcion}));

  Future<void> cambiarEstado(TipoCatalogo tipo, String id, {required bool activa}) =>
      protegerApi(() => _dio.post<void>('/api/${tipo.ruta}/$id/${activa ? 'activar' : 'desactivar'}'));
}

final catalogosRepositorioProvider = Provider((ref) => CatalogosRepositorio(ref.watch(clienteApiProvider)));

/// Catálogo completo (incluye inactivos) para la pantalla de gestión.
final catalogoCompletoProvider = FutureProvider.autoDispose.family<List<CatalogoDto>, TipoCatalogo>(
  (ref, tipo) => ref.watch(catalogosRepositorioProvider).listar(tipo, incluirInactivas: true),
);

/// Solo elementos activos, para selectores de formularios.
final catalogoActivoProvider = FutureProvider.autoDispose.family<List<CatalogoDto>, TipoCatalogo>(
  (ref, tipo) => ref.watch(catalogosRepositorioProvider).listar(tipo),
);
