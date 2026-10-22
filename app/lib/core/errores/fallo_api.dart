import 'package:dio/dio.dart';

enum TipoFallo { conexion, noAutorizado, prohibido, noEncontrado, validacion, conflicto, servidor, desconocido }

/// Error uniforme de la capa de datos; la UI solo conoce este tipo.
class FalloApi implements Exception {
  FalloApi(this.tipo, {this.detalle, this.codigo, this.errores = const {}});

  final TipoFallo tipo;
  final String? detalle;

  /// Código de negocio opcional enviado por la API (p. ej. `mfa_requerido`).
  final String? codigo;
  final Map<String, List<String>> errores;

  factory FalloApi.desde(Object error) {
    if (error is FalloApi) return error;
    if (error is DioException) {
      final estado = error.response?.statusCode;
      final datos = error.response?.data;
      final detalle = datos is Map ? datos['detail'] as String? : null;
      final codigo = datos is Map ? datos['codigo'] as String? : null;
      final errores = <String, List<String>>{};
      if (datos is Map && datos['errores'] is Map) {
        (datos['errores'] as Map).forEach((k, v) {
          errores['$k'] = [for (final e in (v as List)) '$e'];
        });
      }
      FalloApi f(TipoFallo t) => FalloApi(t, detalle: detalle, codigo: codigo, errores: errores);
      return switch (estado) {
        null => FalloApi(TipoFallo.conexion),
        400 || 422 => f(TipoFallo.validacion),
        401 => f(TipoFallo.noAutorizado),
        403 => f(TipoFallo.prohibido),
        404 => f(TipoFallo.noEncontrado),
        409 => f(TipoFallo.conflicto),
        >= 500 => f(TipoFallo.servidor),
        _ => f(TipoFallo.desconocido),
      };
    }
    return FalloApi(TipoFallo.desconocido);
  }

  /// Primer mensaje de error de un campo, si lo hay (los nombres de campo vienen del backend en PascalCase).
  String? errorDe(String campo) {
    for (final e in errores.entries) {
      if (e.key.toLowerCase() == campo.toLowerCase() && e.value.isNotEmpty) return e.value.first;
    }
    return null;
  }

  @override
  String toString() => 'FalloApi($tipo, $codigo, $detalle)';
}

/// Ejecuta una llamada a la API y convierte cualquier error en [FalloApi].
Future<T> protegerApi<T>(Future<T> Function() accion) async {
  try {
    return await accion();
  } catch (e) {
    throw FalloApi.desde(e);
  }
}
