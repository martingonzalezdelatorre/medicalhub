import 'package:dio/dio.dart';

enum TipoFallo { conexion, noAutorizado, noEncontrado, validacion, servidor, desconocido }

/// Error uniforme de la capa de datos; la UI solo conoce este tipo.
class FalloApi implements Exception {
  FalloApi(this.tipo, {this.detalle, this.errores = const {}});

  final TipoFallo tipo;
  final String? detalle;
  final Map<String, List<String>> errores;

  factory FalloApi.desde(Object error) {
    if (error is FalloApi) return error;
    if (error is DioException) {
      final estado = error.response?.statusCode;
      final datos = error.response?.data;
      final detalle = datos is Map ? datos['detail'] as String? : null;
      final errores = <String, List<String>>{};
      if (datos is Map && datos['errores'] is Map) {
        (datos['errores'] as Map).forEach((k, v) {
          errores['$k'] = [for (final e in (v as List)) '$e'];
        });
      }
      return switch (estado) {
        null => FalloApi(TipoFallo.conexion),
        400 || 422 => FalloApi(TipoFallo.validacion, detalle: detalle, errores: errores),
        401 || 403 => FalloApi(TipoFallo.noAutorizado, detalle: detalle),
        404 => FalloApi(TipoFallo.noEncontrado, detalle: detalle),
        >= 500 => FalloApi(TipoFallo.servidor, detalle: detalle),
        _ => FalloApi(TipoFallo.desconocido, detalle: detalle),
      };
    }
    return FalloApi(TipoFallo.desconocido);
  }

  @override
  String toString() => 'FalloApi($tipo, $detalle)';
}
