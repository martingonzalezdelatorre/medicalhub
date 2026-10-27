import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/auth_controlador.dart';
import '../config/entorno.dart';

/// Permite sustituir el transporte HTTP (pruebas). En producción es null.
final adaptadorHttpProvider = Provider<HttpClientAdapter?>((ref) => null);

/// ¿La sesión se mantiene con la cookie de refresco (web)? Es sustituible en pruebas.
final sesionCookieProvider = Provider<bool>((ref) => Entorno.sesionCookie);

/// Cabecera que pide a la API usar la cookie HttpOnly. Además de elegir el modo, actúa como defensa CSRF: un sitio ajeno no puede añadirla.
const cabeceraSesionCookie = 'X-Sesion-Cookie';

BaseOptions _opciones(bool cookie) => BaseOptions(
      baseUrl: Entorno.urlBaseApi,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      headers: {'Accept': 'application/json', if (cookie) cabeceraSesionCookie: '1'},
      // En la web, el navegador solo envía y guarda la cookie si se piden credenciales
      extra: {if (cookie) 'withCredentials': true},
    );

/// Cliente sin autenticación (login y refresco).
final clientePublicoProvider = Provider<Dio>((ref) {
  final dio = Dio(_opciones(ref.watch(sesionCookieProvider)));
  final adaptador = ref.watch(adaptadorHttpProvider);
  if (adaptador != null) dio.httpClientAdapter = adaptador;
  return dio;
});

/// Cliente con el token de acceso; ante un 401 renueva la sesión una vez y reintenta.
final clienteApiProvider = Provider<Dio>((ref) {
  final dio = Dio(_opciones(ref.watch(sesionCookieProvider)));
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
