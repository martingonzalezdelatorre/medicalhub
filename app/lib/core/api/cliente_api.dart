import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/auth_controlador.dart';
import '../config/entorno.dart';

/// Permite sustituir el transporte HTTP (pruebas). En producción es null.
final adaptadorHttpProvider = Provider<HttpClientAdapter?>((ref) => null);

BaseOptions _opciones() => BaseOptions(
      baseUrl: Entorno.urlBaseApi,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      headers: {'Accept': 'application/json'},
    );

/// Cliente sin autenticación (login y refresco).
final clientePublicoProvider = Provider<Dio>((ref) {
  final dio = Dio(_opciones());
  final adaptador = ref.watch(adaptadorHttpProvider);
  if (adaptador != null) dio.httpClientAdapter = adaptador;
  return dio;
});

/// Cliente con el token de acceso; ante un 401 renueva la sesión una vez y reintenta.
final clienteApiProvider = Provider<Dio>((ref) {
  final dio = Dio(_opciones());
  final adaptador = ref.watch(adaptadorHttpProvider);
  if (adaptador != null) dio.httpClientAdapter = adaptador;
  dio.interceptors.add(InterceptorsWrapper(
    onRequest: (options, handler) {
      final token = ref.read(authProvider.notifier).tokenAcceso;
      if (token != null) options.headers['Authorization'] = 'Bearer $token';
      handler.next(options);
    },
    onError: (error, handler) async {
      final opciones = error.requestOptions;
      if (error.response?.statusCode == 401 && opciones.extra['reintentado'] != true) {
        final auth = ref.read(authProvider.notifier);
        if (await auth.refrescarSilencioso()) {
          opciones.extra['reintentado'] = true;
          opciones.headers['Authorization'] = 'Bearer ${auth.tokenAcceso}';
          try {
            return handler.resolve(await dio.fetch<dynamic>(opciones));
          } on DioException catch (e) {
            return handler.next(e);
          }
        }
      }
      handler.next(error);
    },
  ));
  return dio;
});
