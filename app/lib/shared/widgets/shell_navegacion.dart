import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth/auth_controlador.dart';
import '../../core/auth/modelos.dart';
import '../../l10n/app_localizations.dart';

class _Destino {
  const _Destino(this.ruta, this.icono, this.etiqueta);
  final String ruta;
  final IconData icono;
  final String etiqueta;
}

/// Estructura adaptativa: barra inferior en móvil, raíl lateral en tablet y escritorio.
class ShellNavegacion extends ConsumerWidget {
  const ShellNavegacion({super.key, required this.ubicacion, required this.child});

  final String ubicacion;
  final Widget child;

  static List<_Destino> _destinos(AppLocalizations t, PerfilUsuario perfil) => [
        _Destino('/', Icons.home_outlined, t.menuInicio),
        if (perfil.tieneRol(Rol.administradorSociedad)) _Destino('/usuarios', Icons.group_outlined, t.menuUsuarios),
        if (perfil.tieneRol(Rol.administradorPlataforma))
          _Destino('/organizaciones', Icons.business_outlined, t.menuOrganizaciones),
      ];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final perfil = ref.watch(authProvider).perfil;
    if (perfil == null) return const SizedBox.shrink();

    final destinos = _destinos(t, perfil);
    final indice = destinos.indexWhere((d) => d.ruta == ubicacion).clamp(0, destinos.length - 1);
    final ancho = MediaQuery.sizeOf(context).width;
    final conRail = ancho >= 600 && destinos.length > 1;
    final conBarra = ancho < 600 && destinos.length > 1;

    void ir(int i) => context.go(destinos[i].ruta);

    return Scaffold(
      appBar: AppBar(
        title: Text(destinos[indice].etiqueta),
        actions: [
          IconButton(
            tooltip: t.cerrarSesion,
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(authProvider.notifier).cerrarSesion(),
          ),
        ],
      ),
      body: Row(children: [
        if (conRail)
          NavigationRail(
            extended: ancho >= 1000,
            selectedIndex: indice,
            onDestinationSelected: ir,
            destinations: [
              for (final d in destinos) NavigationRailDestination(icon: Icon(d.icono), label: Text(d.etiqueta)),
            ],
          ),
        Expanded(child: child),
      ]),
      bottomNavigationBar: conBarra
          ? NavigationBar(
              selectedIndex: indice,
              onDestinationSelected: ir,
              destinations: [for (final d in destinos) NavigationDestination(icon: Icon(d.icono), label: d.etiqueta)],
            )
          : null,
    );
  }
}
