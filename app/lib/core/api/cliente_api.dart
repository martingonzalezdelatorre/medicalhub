import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/entorno.dart';

final clienteApiProvider = Provider<Dio>((ref) {
  final dio = Dio(BaseOptions(
    baseUrl: Entorno.urlBaseApi,
    connectTimeout: const Duration(seconds: 10),
    receiveTimeout: const Duration(seconds: 30),
    headers: {'Accept': 'application/json'},
  ));
  // E1: interceptores de autenticación (token y refresco) se añadirán aquí.
  return dio;
});
