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

/// Estructura adaptativa: barra inferior en móvil (con «Más» si hay más de cinco secciones),
/// raíl lateral en tablet y escritorio.
class ShellNavegacion extends ConsumerWidget {
  const ShellNavegacion({super.key, required this.ubicacion, required this.child});

  final String ubicacion;
  final Widget child;

  static const _maxEnBarra = 5;

  static List<_Destino> _destinos(AppLocalizations t, PerfilUsuario perfil) {
    final admin = perfil.tieneRol(Rol.administradorSociedad);
    final lectura = admin || perfil.tieneRol(Rol.coordinadorMedico) || perfil.tieneRol(Rol.responsableCentro);
    final gestion = admin || perfil.tieneRol(Rol.coordinadorMedico);
    return [
      _Destino('/', Icons.home_outlined, t.menuInicio),
      if (lectura) _Destino('/planificacion', Icons.calendar_month_outlined, t.menuPlanificacion),
      if (perfil.tieneRol(Rol.profesionalSanitario)) _Destino('/mi-planificacion', Icons.event_available_outlined, t.menuMiPlanificacion),
      if (lectura) _Destino('/cuadrantes', Icons.view_timeline_outlined, t.menuCuadrantes),
      if (lectura) _Destino('/profesionales', Icons.badge_outlined, t.menuProfesionales),
      if (lectura) _Destino('/centros', Icons.local_hospital_outlined, t.menuCentros),
      if (gestion) _Destino('/reglas', Icons.rule_folder_outlined, t.menuReglas),
      if (admin) _Destino('/catalogos', Icons.category_outlined, t.menuCatalogos),
      if (admin) _Destino('/usuarios', Icons.group_outlined, t.menuUsuarios),
      if (perfil.tieneRol(Rol.administradorPlataforma)) _Destino('/organizaciones', Icons.business_outlined, t.menuOrganizaciones),
    ];
  }

  /// Sección del menú a la que pertenece una ruta anidada (p. ej. /servicios/x pertenece a Centros).
  static String _seccion(String ubicacion) {
    if (ubicacion.startsWith('/servicios/')) return '/centros';
    final partes = ubicacion.split('/');
    return partes.length > 1 && partes[1].isNotEmpty ? '/${partes[1]}' : '/';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final perfil = ref.watch(authProvider).perfil;
    if (perfil == null) return const SizedBox.shrink();

    final destinos = _destinos(t, perfil);
    final seccion = _seccion(ubicacion);
    final indice = destinos.indexWhere((d) => d.ruta == seccion).clamp(0, destinos.length - 1);
    final esDetalle = ubicacion != seccion && ubicacion.length > seccion.length || ubicacion.startsWith('/servicios/');
    final ancho = MediaQuery.sizeOf(context).width;
    final conRail = ancho >= 600 && destinos.length > 1;
    final conBarra = ancho < 600 && destinos.length > 1;

    void ir(int i) => context.go(destinos[i].ruta);

    // En la barra inferior caben cinco; si hay más, la última posición es «Más».
    final conMas = destinos.length > _maxEnBarra;
    final visiblesEnBarra = conMas ? destinos.take(_maxEnBarra - 1).toList() : destinos;
    final indiceBarra = conMas && indice >= _maxEnBarra - 1 ? _maxEnBarra - 1 : indice;

    Future<void> abrirMas() async {
      final elegido = await showModalBottomSheet<int>(
        context: context,
        builder: (c) => SafeArea(
          child: ListView(shrinkWrap: true, children: [
            for (var i = _maxEnBarra - 1; i < destinos.length; i++)
              ListTile(leading: Icon(destinos[i].icono), title: Text(destinos[i].etiqueta), selected: i == indice, onTap: () => Navigator.pop(c, i)),
          ]),
        ),
      );
      if (elegido != null) ir(elegido);
    }

    return Scaffold(
      appBar: AppBar(
        leading: esDetalle ? IconButton(icon: const Icon(Icons.arrow_back), onPressed: () => context.go(seccion)) : null,
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
            scrollable: true,
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
              selectedIndex: indiceBarra,
              onDestinationSelected: (i) => conMas && i == _maxEnBarra - 1 ? abrirMas() : ir(i),
              destinations: [
                for (final d in visiblesEnBarra) NavigationDestination(icon: Icon(d.icono), label: d.etiqueta),
                if (conMas) NavigationDestination(icon: const Icon(Icons.more_horiz), label: t.menuMas),
              ],
            )
          : null,
    );
  }
}
