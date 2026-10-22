import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/inicio/presentation/pagina_inicio.dart';

final enrutadorProvider = Provider<GoRouter>((ref) => GoRouter(
      initialLocation: '/',
      routes: [
        GoRoute(path: '/', builder: (_, _) => const PaginaInicio()),
      ],
      // E1: redirect según sesión y rol.
    ));
