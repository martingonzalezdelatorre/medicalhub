import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Persistencia del token de refresco. El token de acceso vive solo en memoria.
abstract class AlmacenTokens {
  Future<String?> leer();
  Future<void> guardar(String tokenRefresco);
  Future<void> borrar();
}

class AlmacenTokensSeguro implements AlmacenTokens {
  const AlmacenTokensSeguro();

  static const _clave = 'medicalhub.refresco';
  static const _almacen = FlutterSecureStorage();

  @override
  Future<String?> leer() async {
    try {
      return await _almacen.read(key: _clave);
    } catch (_) {
      return null; // almacén no disponible o corrupto: se exige iniciar sesión de nuevo
    }
  }

  @override
  Future<void> guardar(String tokenRefresco) => _almacen.write(key: _clave, value: tokenRefresco);

  @override
  Future<void> borrar() => _almacen.delete(key: _clave);
}

/// Almacén en memoria (pruebas).
class AlmacenTokensMemoria implements AlmacenTokens {
  String? valor;

  @override
  Future<String?> leer() async => valor;

  @override
  Future<void> guardar(String tokenRefresco) async => valor = tokenRefresco;

  @override
  Future<void> borrar() async => valor = null;
}

final almacenTokensProvider = Provider<AlmacenTokens>((ref) => const AlmacenTokensSeguro());
