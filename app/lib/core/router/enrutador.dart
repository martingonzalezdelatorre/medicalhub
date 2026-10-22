import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/autenticacion/presentation/pagina_login.dart';
import '../../features/autenticacion/presentation/pagina_mfa.dart';
import '../../features/inicio/presentation/pagina_inicio.dart';
import '../../features/organizaciones/presentation/pagina_organizaciones.dart';
import '../../features/usuarios/presentation/pagina_usuarios.dart';
import '../../features/catalogos/presentation/pagina_catalogos.dart';
import '../../features/estructura/presentation/pagina_centros.dart';
import '../../features/estructura/presentation/pagina_servicio.dart';
import '../../features/profesionales/presentation/pagina_profesional.dart';
import '../../features/profesionales/presentation/pagina_profesionales.dart';
import '../../shared/widgets/shell_navegacion.dart';
import '../auth/auth_controlador.dart';
import '../auth/modelos.dart';

/// Decide la ruta según la sesión. La API repite siempre estas comprobaciones: aquí solo se evita mostrar pantallas inútiles.
String? redireccionar(EstadoAuth auth, String ubicacion) {
  if (auth.iniciando) return ubicacion == '/cargando' ? null : '/cargando';
  if (!auth.autenticado) return ubicacion == '/login' ? null : '/login';
  if (auth.mfaPendiente) return ubicacion == '/mfa' ? null : '/mfa';

  if (const ['/login', '/mfa', '/cargando'].contains(ubicacion)) return '/';
  final perfil = auth.perfil!;
  final admin = perfil.tieneRol(Rol.administradorSociedad);
  final lectura = admin || perfil.tieneRol(Rol.coordinadorMedico) || perfil.tieneRol(Rol.responsableCentro);
  bool en(String base) => ubicacion == base || ubicacion.startsWith('$base/');

  if (en('/usuarios') && !admin) return '/';
  if (en('/catalogos') && !admin) return '/';
  if (ubicacion == '/profesionales/nuevo' && !admin) return '/';
  if ((en('/profesionales') || en('/centros') || en('/servicios')) && !lectura) return '/';
  if (en('/organizaciones') && !perfil.tieneRol(Rol.administradorPlataforma)) return '/';
  return null;
}

final enrutadorProvider = Provider<GoRouter>((ref) {
  final cambios = ValueNotifier<int>(0);
  ref.listen(authProvider, (_, _) => cambios.value++);
  ref.onDispose(cambios.dispose);

  final router = GoRouter(
    initialLocation: '/',
    refreshListenable: cambios,
    redirect: (context, estado) => redireccionar(ref.read(authProvider), estado.uri.path),
    routes: [
      GoRoute(path: '/cargando', builder: (_, _) => const Scaffold(body: Center(child: CircularProgressIndicator()))),
      GoRoute(path: '/login', builder: (_, _) => const PaginaLogin()),
      GoRoute(path: '/mfa', builder: (_, _) => const PaginaMfa()),
      ShellRoute(
        builder: (context, estado, child) => ShellNavegacion(ubicacion: estado.uri.path, child: child),
        routes: [
          GoRoute(path: '/', builder: (_, _) => const PaginaInicio()),
          GoRoute(path: '/usuarios', builder: (_, _) => const PaginaUsuarios()),
          GoRoute(path: '/organizaciones', builder: (_, _) => const PaginaOrganizaciones()),
          GoRoute(path: '/catalogos', builder: (_, _) => const PaginaCatalogos()),
          GoRoute(path: '/centros', builder: (_, _) => const PaginaCentros()),
          GoRoute(path: '/centros/:id', builder: (_, e) => PaginaCentroDetalle(id: e.pathParameters['id']!)),
          GoRoute(path: '/servicios/:id', builder: (_, e) => PaginaServicio(id: e.pathParameters['id']!)),
          GoRoute(path: '/profesionales', builder: (_, _) => const PaginaProfesionales()),
          GoRoute(path: '/profesionales/nuevo', builder: (_, _) => const PaginaProfesional()),
          GoRoute(path: '/profesionales/:id', builder: (_, e) => PaginaProfesional(id: e.pathParameters['id'])),
        ],
      ),
    ],
  );
  ref.onDispose(router.dispose);
  return router;
});
