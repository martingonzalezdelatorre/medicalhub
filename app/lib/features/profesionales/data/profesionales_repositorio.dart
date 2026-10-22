import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/errores/fallo_api.dart';

part 'profesionales_repositorio.g.dart';

@JsonSerializable(createToJson: false)
class CompetenciaProfesionalDto {
  const CompetenciaProfesionalDto({required this.competenciaId, this.vigenteHasta});

  factory CompetenciaProfesionalDto.fromJson(Map<String, dynamic> json) => _$CompetenciaProfesionalDtoFromJson(json);

  final String competenciaId;

  /// yyyy-MM-dd; null = sin caducidad.
  final String? vigenteHasta;
}

@JsonSerializable(createToJson: false)
class ProfesionalDto {
  const ProfesionalDto({
    required this.id,
    this.usuarioId,
    required this.nombre,
    required this.apellidos,
    this.email,
    this.telefono,
    this.numeroColegiado,
    required this.categoriaId,
    required this.especialidadesIds,
    required this.competencias,
    required this.serviciosIds,
    required this.activo,
  });

  factory ProfesionalDto.fromJson(Map<String, dynamic> json) => _$ProfesionalDtoFromJson(json);

  final String id;
  final String? usuarioId;
  final String nombre;
  final String apellidos;
  final String? email;
  final String? telefono;
  final String? numeroColegiado;
  final String categoriaId;
  final List<String> especialidadesIds;
  final List<CompetenciaProfesionalDto> competencias;
  final List<String> serviciosIds;
  final bool activo;

  String get nombreCompleto => '$nombre $apellidos';
}

@JsonSerializable(createToJson: false)
class CambioHistorialDto {
  const CambioHistorialDto({required this.fecha, this.usuario, required this.entidad, required this.accion, this.cambios});

  factory CambioHistorialDto.fromJson(Map<String, dynamic> json) => _$CambioHistorialDtoFromJson(json);

  final DateTime fecha;
  final String? usuario;
  final String entidad;
  final String accion;
  final String? cambios;
}

class ProfesionalesRepositorio {
  ProfesionalesRepositorio(this._dio);
  final Dio _dio;

  Future<List<ProfesionalDto>> listar({bool? activo, String? texto}) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/profesionales', queryParameters: {
          'activo': ?activo,
          if (texto != null && texto.trim().isNotEmpty) 'texto': texto.trim(),
        });
        return [for (final j in r.data!) ProfesionalDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<ProfesionalDto> obtener(String id) => protegerApi(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/profesionales/$id');
        return ProfesionalDto.fromJson(r.data!);
      });

  Future<List<CambioHistorialDto>> historial(String id) => protegerApi(() async {
        final r = await _dio.get<List<dynamic>>('/api/profesionales/$id/historial');
        return [for (final j in r.data!) CambioHistorialDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<void> guardar(String? id, Map<String, dynamic> datos) => protegerApi(
      () => id == null ? _dio.post<void>('/api/profesionales', data: datos) : _dio.put<void>('/api/profesionales/$id', data: datos));

  Future<void> cambiarEstado(String id, {required bool activo}) =>
      protegerApi(() => _dio.post<void>('/api/profesionales/$id/${activo ? 'activar' : 'desactivar'}'));
}

final profesionalesRepositorioProvider = Provider((ref) => ProfesionalesRepositorio(ref.watch(clienteApiProvider)));

/// Filtros de la lista (texto y solo activos).
class FiltroProfesionales {
  const FiltroProfesionales({this.texto = '', this.soloActivos = true});
  final String texto;
  final bool soloActivos;

  @override
  bool operator ==(Object other) => other is FiltroProfesionales && other.texto == texto && other.soloActivos == soloActivos;

  @override
  int get hashCode => Object.hash(texto, soloActivos);
}

final profesionalesProvider = FutureProvider.autoDispose.family<List<ProfesionalDto>, FiltroProfesionales>(
  (ref, f) => ref.watch(profesionalesRepositorioProvider).listar(activo: f.soloActivos ? true : null, texto: f.texto),
);

final profesionalProvider = FutureProvider.autoDispose.family<ProfesionalDto, String>(
  (ref, id) => ref.watch(profesionalesRepositorioProvider).obtener(id),
);
