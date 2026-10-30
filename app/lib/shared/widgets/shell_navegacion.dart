import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth/auth_controlador.dart';
import '../../core/auth/modelos.dart';
import '../../core/tema/tema.dart';
import '../../features/ausencias/presentation/pagina_notificaciones.dart';
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
    final rrhh = perfil.tieneRol(Rol.recursosHumanos);
    return [
      _Destino('/', Icons.home_outlined, t.menuInicio),
      if (lectura) _Destino('/planificacion', Icons.calendar_month_outlined, t.menuPlanificacion),
      if (gestion) _Destino('/cobertura', Icons.groups_2_outlined, t.menuCobertura),
      if (perfil.tieneRol(Rol.profesionalSanitario)) _Destino('/fichaje', Icons.fingerprint, t.menuFichaje),
      if (perfil.tieneRol(Rol.profesionalSanitario)) _Destino('/mi-planificacion', Icons.event_available_outlined, t.menuMiPlanificacion),
      if (perfil.tieneRol(Rol.profesionalSanitario)) _Destino('/mi-disponibilidad', Icons.beach_access_outlined, t.menuMiDisponibilidad),
      if (lectura) _Destino('/cuadrantes', Icons.view_timeline_outlined, t.menuCuadrantes),
      if (lectura) _Destino('/profesionales', Icons.badge_outlined, t.menuProfesionales),
      if (lectura || rrhh) _Destino('/centros', Icons.local_hospital_outlined, t.menuCentros),
      if (lectura) _Destino('/informes', Icons.insights_outlined, t.menuInformes),
      if (gestion) _Destino('/ausencias', Icons.event_busy_outlined, t.menuAusencias),
      if (gestion) _Destino('/reglas', Icons.rule_folder_outlined, t.menuReglas),
      if (admin || rrhh) _Destino('/catalogos', Icons.category_outlined, t.menuCatalogos),
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
    final enNotificaciones = ubicacion == '/notificaciones';
    final esDetalle = enNotificaciones || ubicacion != seccion && ubicacion.length > seccion.length || ubicacion.startsWith('/servicios/');
    final ancho = MediaQuery.sizeOf(context).width;
    final conLateral = ancho >= Pantalla.tableta && destinos.length > 1;
    // Menú lateral: fijo en escritorio y desplegable (cajón) en móvil y tableta, además de las pestañas.
    final conCajon = !conLateral && destinos.length > 1;
    final conBarra = conCajon;

    void ir(int i) => context.go(destinos[i].ruta);

    // En la barra inferior caben cinco; si hay más, la última posición es «Más».
    final conMas = destinos.length > _maxEnBarra;
    final visiblesEnBarra = conMas ? destinos.take(_maxEnBarra - 1).toList() : destinos;
    final indiceBarra = conMas && indice >= _maxEnBarra - 1 ? _maxEnBarra - 1 : indice;

    final cs = Theme.of(context).colorScheme;
    return Scaffold(
      drawer: conCajon
          ? Drawer(
              child: _BarraLateral(
                destinos: destinos,
                indice: indice,
                perfil: perfil,
                enCajon: true,
                onSeleccion: (i) {
                  Navigator.of(context).pop();
                  ir(i);
                },
              ),
            )
          : null,
      appBar: AppBar(
        automaticallyImplyLeading: false,
        foregroundColor: Colors.white,
        backgroundColor: Colors.transparent,
        shape: const Border(),
        titleTextStyle: Theme.of(context).textTheme.titleLarge?.copyWith(color: Colors.white, fontWeight: FontWeight.w800),
        flexibleSpace: const DecoratedBox(decoration: BoxDecoration(gradient: TemaApp.degradadoBarra)),
        leading: esDetalle
            ? IconButton(icon: const Icon(Icons.arrow_back), onPressed: () => context.go(enNotificaciones ? '/' : seccion))
            : (conCajon
                ? Builder(builder: (c) => IconButton(tooltip: t.menuMas, icon: const Icon(Icons.menu), onPressed: () => Scaffold.of(c).openDrawer()))
                : null),
        title: Text(enNotificaciones ? t.notificaciones : destinos[indice].etiqueta),
        actions: [
          CampanaNotificaciones(ubicacion: ubicacion),
          if (!conLateral) _MenuUsuario(perfil: perfil),
          const SizedBox(width: 8),
        ],
      ),
      body: Row(children: [
        if (conLateral) ...[
          _BarraLateral(destinos: destinos, indice: indice, perfil: perfil, onSeleccion: ir),
          VerticalDivider(width: 1, color: cs.outlineVariant),
        ],
        Expanded(child: child),
      ]),
      bottomNavigationBar: conBarra
          ? Builder(
              builder: (c) => NavigationBar(
                selectedIndex: indiceBarra,
                onDestinationSelected: (i) => conMas && i == _maxEnBarra - 1 ? Scaffold.of(c).openDrawer() : ir(i),
                destinations: [
                  for (final d in visiblesEnBarra) NavigationDestination(icon: Icon(d.icono), label: d.etiqueta),
                  if (conMas) NavigationDestination(icon: const Icon(Icons.more_horiz), label: t.menuMas),
                ],
              ),
            )
          : null,
    );
  }
}

/// Menú de usuario (avatar): muestra quién ha iniciado sesión y permite cerrarla.
class _MenuUsuario extends ConsumerWidget {
  const _MenuUsuario({required this.perfil});
  final PerfilUsuario perfil;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;
    return PopupMenuButton<String>(
      tooltip: perfil.nombreCompleto,
      position: PopupMenuPosition.under,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      onSelected: (v) {
        if (v == 'salir') ref.read(authProvider.notifier).cerrarSesion();
      },
      itemBuilder: (c) => [
        PopupMenuItem<String>(
          enabled: false,
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(perfil.nombreCompleto, style: Theme.of(c).textTheme.titleSmall?.copyWith(color: cs.onSurface)),
            Text(perfil.email, style: Theme.of(c).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
          ]),
        ),
        const PopupMenuDivider(),
        PopupMenuItem<String>(value: 'salir', child: Row(children: [const Icon(Icons.logout, size: 20), const SizedBox(width: 12), Text(t.cerrarSesion)])),
      ],
      child: Container(
        padding: const EdgeInsets.all(2),
        decoration: const BoxDecoration(shape: BoxShape.circle, color: Colors.white),
        child: _Avatar(perfil.nombreCompleto, radio: 16),
      ),
    );
  }
}

class _Avatar extends StatelessWidget {
  const _Avatar(this.nombre, {this.radio = 20});
  final String nombre;
  final double radio;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final n = nombre.trim();
    return Container(
      width: radio * 2,
      height: radio * 2,
      alignment: Alignment.center,
      decoration: const BoxDecoration(shape: BoxShape.circle, gradient: TemaApp.degradadoMarca),
      child: Text(n.isEmpty ? '?' : n.characters.first.toUpperCase(),
          style: TextStyle(color: cs.onPrimary, fontWeight: FontWeight.w700, fontSize: radio * 0.9)),
    );
  }
}

/// Menú lateral fijo para escritorio: marca, secciones con indicador y datos de la sesión.
class _BarraLateral extends ConsumerWidget {
  const _BarraLateral({required this.destinos, required this.indice, required this.perfil, required this.onSeleccion, this.enCajon = false});
  final bool enCajon;
  final List<_Destino> destinos;
  final int indice;
  final PerfilUsuario perfil;
  final ValueChanged<int> onSeleccion;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final tema = Theme.of(context);
    final cs = tema.colorScheme;
    return Material(
      color: cs.surface,
      child: SizedBox(
        width: enCajon ? null : 264,
        child: SafeArea(
          child: Column(children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 22, 20, 16),
              child: Row(children: [
                Container(
                  padding: const EdgeInsets.all(9),
                  decoration: BoxDecoration(
                    gradient: TemaApp.degradadoMarca,
                    borderRadius: BorderRadius.circular(12),
                    boxShadow: [BoxShadow(color: cs.primary.withAlpha(70), blurRadius: 12, offset: const Offset(0, 4))],
                  ),
                  child: const Icon(Icons.monitor_heart_outlined, color: Colors.white, size: 22),
                ),
                const SizedBox(width: 12),
                Expanded(child: Text(t.tituloApp, style: tema.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800, letterSpacing: -0.4))),
              ]),
            ),
            Expanded(
              child: ListView.builder(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                itemCount: destinos.length,
                itemBuilder: (c, i) {
                  final d = destinos[i];
                  final sel = i == indice;
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 2),
                    child: Material(
                      color: sel ? cs.primary.withAlpha(28) : Colors.transparent,
                      borderRadius: BorderRadius.circular(12),
                      clipBehavior: Clip.antiAlias,
                      child: InkWell(
                        onTap: () => onSeleccion(i),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 11),
                          child: Row(children: [
                            Icon(d.icono, size: 21, color: sel ? cs.primary : cs.onSurfaceVariant),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Text(d.etiqueta,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: tema.textTheme.bodyMedium?.copyWith(
                                      fontWeight: sel ? FontWeight.w700 : FontWeight.w500, color: sel ? cs.primary : cs.onSurface)),
                            ),
                            if (sel) Container(width: 4, height: 18, decoration: BoxDecoration(color: cs.primary, borderRadius: BorderRadius.circular(2))),
                          ]),
                        ),
                      ),
                    ),
                  );
                },
              ),
            ),
            Container(
              margin: const EdgeInsets.fromLTRB(12, 8, 12, 14),
              padding: const EdgeInsets.fromLTRB(12, 10, 4, 10),
              decoration: BoxDecoration(color: cs.surfaceContainerLow, borderRadius: BorderRadius.circular(14), border: Border.all(color: cs.outlineVariant)),
              child: Row(children: [
                _Avatar(perfil.nombreCompleto),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(perfil.nombreCompleto, maxLines: 1, overflow: TextOverflow.ellipsis, style: tema.textTheme.titleSmall),
                    Text(perfil.organizacion ?? perfil.email,
                        maxLines: 1, overflow: TextOverflow.ellipsis, style: tema.textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                  ]),
                ),
                IconButton(
                  tooltip: t.cerrarSesion,
                  icon: const Icon(Icons.logout, size: 20),
                  onPressed: () => ref.read(authProvider.notifier).cerrarSesion(),
                ),
              ]),
            ),
          ]),
        ),
      ),
    );
  }
}
