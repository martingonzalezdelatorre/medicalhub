import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:medicalhub_app/core/api/cliente_api.dart';
import 'package:medicalhub_app/core/auth/almacen_tokens.dart';
import 'package:medicalhub_app/core/auth/auth_controlador.dart';
import 'package:medicalhub_app/core/auth/modelos.dart';
import 'package:medicalhub_app/core/errores/fallo_api.dart';
import 'package:medicalhub_app/core/router/enrutador.dart';

import 'ayudas_prueba.dart';

EstadoAuth _estado({bool iniciando = false, List<String>? roles, bool mfaPendiente = false}) => EstadoAuth(
      iniciando: iniciando,
      sesion: roles == null ? null : RespuestaSesion.fromJson(sesionJson(roles: roles, mfaPendiente: mfaPendiente)),
    );

void main() {
  group('redireccionar', () {
    test('mientras se restaura la sesión muestra la pantalla de carga', () {
      expect(redireccionar(_estado(iniciando: true), '/'), '/cargando');
      expect(redireccionar(_estado(iniciando: true), '/cargando'), isNull);
    });

    test('sin sesión todo lleva al login', () {
      expect(redireccionar(_estado(), '/'), '/login');
      expect(redireccionar(_estado(), '/usuarios'), '/login');
      expect(redireccionar(_estado(), '/login'), isNull);
    });

    test('con MFA pendiente solo se permite la pantalla de MFA', () {
      final e = _estado(roles: ['AdministradorSociedad'], mfaPendiente: true);
      expect(redireccionar(e, '/'), '/mfa');
      expect(redireccionar(e, '/usuarios'), '/mfa');
      expect(redireccionar(e, '/mfa'), isNull);
    });

    test('con sesión completa se sale de login/mfa', () {
      final e = _estado(roles: ['ProfesionalSanitario']);
      expect(redireccionar(e, '/login'), '/');
      expect(redireccionar(e, '/mfa'), '/');
    });

    test('las rutas de gestión exigen el rol correspondiente', () {
      final prof = _estado(roles: ['ProfesionalSanitario']);
      final admin = _estado(roles: ['AdministradorSociedad']);
      final plataforma = _estado(roles: ['AdministradorPlataforma']);
      expect(redireccionar(prof, '/usuarios'), '/');
      expect(redireccionar(admin, '/usuarios'), isNull);
      expect(redireccionar(admin, '/organizaciones'), '/');
      expect(redireccionar(plataforma, '/organizaciones'), isNull);
      expect(redireccionar(plataforma, '/usuarios'), '/');
    });
  });

  group('AuthControlador y cliente con renovación', () {
    ProviderContainer crear(ServidorFalso servidor, {AlmacenTokens? almacen}) {
      final c = ProviderContainer(overrides: servidor.sobrescrituras(almacen: almacen));
      addTearDown(c.dispose);
      return c;
    }

    test('login guarda el token de refresco y deja la sesión en memoria', () async {
      final almacen = AlmacenTokensMemoria();
      final servidor = ServidorFalso((p) => json(sesionJson()));
      final c = crear(servidor, almacen: almacen);

      await c.read(authProvider.notifier).login('ana@sociedad.es', 'x');

      expect(c.read(authProvider).autenticado, isTrue);
      expect(almacen.valor, 'REFRESCO-1');
      expect(c.read(authProvider).perfil!.roles, [Rol.administradorSociedad]);
    });

    test('mfa_requerido se propaga como código para que la UI pida el segundo factor', () async {
      final servidor = ServidorFalso((p) => json(problema(401, 'Introduzca el código', codigo: 'mfa_requerido'), 401));
      final c = crear(servidor);

      await expectLater(
        c.read(authProvider.notifier).login('ana@sociedad.es', 'x'),
        throwsA(isA<FalloApi>().having((f) => f.codigo, 'codigo', 'mfa_requerido')),
      );
      expect(c.read(authProvider).autenticado, isFalse);
    });

    test('un 401 renueva la sesión una sola vez y reintenta la petición', () async {
      final servidor = ServidorFalso((p) {
        if (p.path == '/api/autenticacion/login') return json(sesionJson());
        if (p.path == '/api/autenticacion/refrescar') return json(sesionJson(acceso: 'ACCESO-2', refresco: 'REFRESCO-2'));
        return p.headers['Authorization'] == 'Bearer ACCESO-2' ? json(<dynamic>[]) : json(problema(401, 'caducado'), 401);
      });
      final c = crear(servidor);
      await c.read(authProvider.notifier).login('ana@sociedad.es', 'x');

      // Dos peticiones simultáneas con el token caducado comparten UNA renovación
      final dio = c.read(clienteApiProvider);
      final r = await Future.wait([dio.get<dynamic>('/api/usuarios'), dio.get<dynamic>('/api/usuarios')]);

      expect(r.map((x) => x.statusCode), [200, 200]);
      expect(servidor.contar('POST', '/api/autenticacion/refrescar'), 1);
      expect(c.read(authProvider.notifier).tokenAcceso, 'ACCESO-2');
    });

    test('si la renovación es rechazada se cierra la sesión local', () async {
      final almacen = AlmacenTokensMemoria();
      final servidor = ServidorFalso((p) {
        if (p.path == '/api/autenticacion/login') return json(sesionJson());
        return json(problema(401, 'no válida'), 401);
      });
      final c = crear(servidor, almacen: almacen);
      await c.read(authProvider.notifier).login('ana@sociedad.es', 'x');

      await expectLater(c.read(clienteApiProvider).get<dynamic>('/api/usuarios'), throwsA(isA<DioException>()));

      expect(c.read(authProvider).autenticado, isFalse);
      expect(almacen.valor, isNull);
    });

    test('al arrancar restaura la sesión con el token guardado', () async {
      final almacen = AlmacenTokensMemoria()..valor = 'GUARDADO';
      final servidor = ServidorFalso((p) => json(sesionJson(refresco: 'NUEVO')));
      final c = crear(servidor, almacen: almacen);

      expect(c.read(authProvider).iniciando, isTrue);
      await Future<void>.delayed(const Duration(milliseconds: 50));

      expect(c.read(authProvider).autenticado, isTrue);
      expect(almacen.valor, 'NUEVO');
      expect(servidor.peticiones.single.data, {'tokenRefresco': 'GUARDADO'});
    });
  });

  group('Sesión web con cookie HttpOnly', () {
    ProviderContainer crearCookie(ServidorFalso servidor, AlmacenTokensMemoria almacen) {
      final c = ProviderContainer(overrides: [...servidor.sobrescrituras(almacen: almacen), sesionCookieProvider.overrideWithValue(true)]);
      addTearDown(c.dispose);
      return c;
    }

    test('las peticiones piden el modo cookie y credenciales; sin él (móvil) no se envía ninguna de las dos cosas', () async {
      final web = ServidorFalso((p) => json(sesionJson(refresco: '')));
      await crearCookie(web, AlmacenTokensMemoria()).read(authProvider.notifier).login('ana@sociedad.es', 'x');
      final p = web.peticiones.firstWhere((x) => x.path == '/api/autenticacion/login');
      expect(p.headers['X-Sesion-Cookie'], '1');
      expect(p.extra['withCredentials'], true);

      final movil = ServidorFalso((p) => json(sesionJson()));
      final c = ProviderContainer(overrides: movil.sobrescrituras());
      addTearDown(c.dispose);
      await c.read(authProvider.notifier).login('ana@sociedad.es', 'x');
      final m = movil.peticiones.firstWhere((x) => x.path == '/api/autenticacion/login');
      expect(m.headers.containsKey('X-Sesion-Cookie'), isFalse);
      expect(m.extra.containsKey('withCredentials'), isFalse);
    });

    test('tras el login no se guarda ningún token de refresco en el cliente', () async {
      final almacen = AlmacenTokensMemoria();
      final c = crearCookie(ServidorFalso((p) => json(sesionJson(refresco: ''))), almacen);
      await c.read(authProvider.notifier).login('ana@sociedad.es', 'x');
      expect(c.read(authProvider).autenticado, isTrue);
      expect(almacen.valor, isNull);
    });

    test('al arrancar intenta restaurar la sesión con la cookie, sin enviar ningún token en el cuerpo', () async {
      final almacen = AlmacenTokensMemoria();
      final servidor = ServidorFalso((p) => json(sesionJson(refresco: '')));
      final c = crearCookie(servidor, almacen);
      expect(c.read(authProvider).iniciando, isTrue);
      await Future<void>.delayed(const Duration(milliseconds: 50));

      expect(c.read(authProvider).autenticado, isTrue);
      expect(servidor.peticiones.single.path, '/api/autenticacion/refrescar');
      expect(servidor.peticiones.single.data, <String, dynamic>{});
      expect(almacen.valor, isNull);
    });

    test('si no hay cookie válida el arranque termina en «sin sesión»', () async {
      final c = crearCookie(ServidorFalso((p) => json(problema(401, 'Sesión no válida'), 401)), AlmacenTokensMemoria());
      expect(c.read(authProvider).iniciando, isTrue);
      await Future<void>.delayed(const Duration(milliseconds: 50));
      expect(c.read(authProvider).iniciando, isFalse);
      expect(c.read(authProvider).autenticado, isFalse);
    });

    test('un 401 renueva con la cookie (sin token en el cuerpo) y reintenta', () async {
      final servidor = ServidorFalso((p) {
        if (p.path == '/api/autenticacion/login') return json(sesionJson(refresco: ''));
        if (p.path == '/api/autenticacion/refrescar') return json(sesionJson(acceso: 'ACCESO-2', refresco: ''));
        return p.headers['Authorization'] == 'Bearer ACCESO-2' ? json(<dynamic>[]) : json(problema(401, 'caducado'), 401);
      });
      final c = crearCookie(servidor, AlmacenTokensMemoria());
      await c.read(authProvider.notifier).login('ana@sociedad.es', 'x');

      final r = await c.read(clienteApiProvider).get<dynamic>('/api/usuarios');
      expect(r.statusCode, 200);
      final refresco = servidor.peticiones.where((x) => x.path == '/api/autenticacion/refrescar').single;
      expect(refresco.data, <String, dynamic>{});
    });

    test('cerrar sesión avisa a la API (que borra la cookie) y deja el estado vacío', () async {
      final servidor = ServidorFalso((p) => p.path == '/api/autenticacion/cerrar-sesion' ? json(<String, dynamic>{}, 204) : json(sesionJson(refresco: '')));
      final c = crearCookie(servidor, AlmacenTokensMemoria());
      await c.read(authProvider.notifier).login('ana@sociedad.es', 'x');

      await c.read(authProvider.notifier).cerrarSesion();

      expect(servidor.contar('POST', '/api/autenticacion/cerrar-sesion'), 1);
      expect(servidor.peticiones.last.data, <String, dynamic>{});
      expect(c.read(authProvider).autenticado, isFalse);
    });
  });
}
