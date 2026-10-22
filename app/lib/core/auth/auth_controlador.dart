import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/cliente_api.dart';
import '../errores/fallo_api.dart';
import 'almacen_tokens.dart';
import 'modelos.dart';

class EstadoAuth {
  const EstadoAuth({this.iniciando = false, this.sesion});

  /// true mientras se intenta restaurar la sesión guardada al arrancar.
  final bool iniciando;
  final RespuestaSesion? sesion;

  bool get autenticado => sesion != null;
  bool get mfaPendiente => sesion?.mfaPendiente ?? false;
  PerfilUsuario? get perfil => sesion?.perfil;
}

final authProvider = NotifierProvider<AuthControlador, EstadoAuth>(AuthControlador.new);

class AuthControlador extends Notifier<EstadoAuth> {
  Future<bool>? _refrescoEnCurso;

  Dio get _publico => ref.read(clientePublicoProvider);
  Dio get _autenticado => ref.read(clienteApiProvider);
  AlmacenTokens get _almacen => ref.read(almacenTokensProvider);

  @override
  EstadoAuth build() {
    Future.microtask(_restaurar);
    return const EstadoAuth(iniciando: true);
  }

  String? get tokenAcceso => state.sesion?.tokenAcceso;

  Future<void> _restaurar() async {
    final token = await _almacen.leer();
    if (token == null) {
      state = const EstadoAuth();
      return;
    }
    try {
      await _aplicar(await _enviar(_publico, '/api/autenticacion/refrescar', {'tokenRefresco': token}));
    } on FalloApi catch (f) {
      if (f.tipo == TipoFallo.noAutorizado) await _almacen.borrar();
      state = const EstadoAuth();
    }
  }

  /// Inicia sesión. Lanza [FalloApi]; con `codigo == 'mfa_requerido'` la UI debe pedir el código.
  Future<void> login(String email, String contrasena, {String? codigoMfa}) async {
    final r = await _enviar(_publico, '/api/autenticacion/login',
        {'email': email, 'contrasena': contrasena, if (codigoMfa != null && codigoMfa.isNotEmpty) 'codigoMfa': codigoMfa});
    await _aplicar(r);
  }

  Future<ConfiguracionMfa> configurarMfa() async {
    try {
      final r = await _autenticado.post<Map<String, dynamic>>('/api/autenticacion/mfa/configurar');
      return ConfiguracionMfa.fromJson(r.data!);
    } catch (e) {
      throw FalloApi.desde(e);
    }
  }

  Future<void> activarMfa(String codigo) async =>
      _aplicar(await _enviar(_autenticado, '/api/autenticacion/mfa/activar', {'codigo': codigo}));

  /// Renueva la sesión con el token de refresco. Varias peticiones fallidas a la vez comparten una única renovación.
  Future<bool> refrescarSilencioso() => _refrescoEnCurso ??= _renovar().whenComplete(() => _refrescoEnCurso = null);

  Future<bool> _renovar() async {
    final token = state.sesion?.tokenRefresco;
    if (token == null) return false;
    try {
      await _aplicar(await _enviar(_publico, '/api/autenticacion/refrescar', {'tokenRefresco': token}));
      return true;
    } on FalloApi catch (f) {
      if (f.tipo == TipoFallo.noAutorizado) await _cerrarLocal();
      return false;
    }
  }

  Future<void> cerrarSesion() async {
    final token = state.sesion?.tokenRefresco;
    if (token != null) {
      try {
        await _autenticado.post<void>('/api/autenticacion/cerrar-sesion', data: {'tokenRefresco': token});
      } catch (_) {
        // aunque el servidor no responda, la sesión local se elimina
      }
    }
    await _cerrarLocal();
  }

  Future<void> _cerrarLocal() async {
    await _almacen.borrar();
    state = const EstadoAuth();
  }

  Future<void> _aplicar(RespuestaSesion r) async {
    await _almacen.guardar(r.tokenRefresco);
    state = EstadoAuth(sesion: r);
  }

  Future<RespuestaSesion> _enviar(Dio dio, String ruta, Map<String, dynamic> cuerpo) async {
    try {
      final r = await dio.post<Map<String, dynamic>>(ruta, data: cuerpo);
      return RespuestaSesion.fromJson(r.data!);
    } catch (e) {
      throw FalloApi.desde(e);
    }
  }
}
