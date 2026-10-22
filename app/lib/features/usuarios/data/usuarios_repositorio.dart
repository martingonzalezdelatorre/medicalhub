import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:json_annotation/json_annotation.dart';

import '../../../core/api/cliente_api.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/errores/fallo_api.dart';

part 'usuarios_repositorio.g.dart';

@JsonSerializable(createToJson: false)
class UsuarioDto {
  const UsuarioDto({
    required this.id,
    required this.email,
    required this.nombreCompleto,
    required this.roles,
    required this.activo,
    required this.mfaActivado,
  });

  factory UsuarioDto.fromJson(Map<String, dynamic> json) => _$UsuarioDtoFromJson(json);

  final String id;
  final String email;
  final String nombreCompleto;
  final List<Rol> roles;
  final bool activo;
  final bool mfaActivado;
}

class UsuariosRepositorio {
  UsuariosRepositorio(this._dio);
  final Dio _dio;

  Future<T> _proteger<T>(Future<T> Function() accion) async {
    try {
      return await accion();
    } catch (e) {
      throw FalloApi.desde(e);
    }
  }

  Future<List<UsuarioDto>> listar() => _proteger(() async {
        final r = await _dio.get<List<dynamic>>('/api/usuarios');
        return [for (final j in r.data!) UsuarioDto.fromJson(j as Map<String, dynamic>)];
      });

  Future<void> crear({
    required String email,
    required String nombreCompleto,
    required String contrasena,
    required List<String> roles,
  }) =>
      _proteger(() => _dio.post<void>('/api/usuarios',
          data: {'email': email, 'nombreCompleto': nombreCompleto, 'contrasena': contrasena, 'roles': roles}));

  Future<void> cambiarEstado(String id, {required bool activo}) =>
      _proteger(() => _dio.post<void>('/api/usuarios/$id/${activo ? 'activar' : 'desactivar'}'));

  Future<void> reiniciarMfa(String id) => _proteger(() => _dio.post<void>('/api/usuarios/$id/reiniciar-mfa'));
}

final usuariosRepositorioProvider = Provider((ref) => UsuariosRepositorio(ref.watch(clienteApiProvider)));

final usuariosProvider = FutureProvider.autoDispose<List<UsuarioDto>>(
  (ref) => ref.watch(usuariosRepositorioProvider).listar(),
);
