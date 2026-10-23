import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/tema/tema.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../ausencias/presentation/dialogo_ausencia.dart';
import '../../informes/data/informes_repositorio.dart';

const _alerta = Color(0xFFE08A00);
const _correcto = Color(0xFF2E7D32);

class PaginaInicio extends ConsumerWidget {
  const PaginaInicio({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final perfil = ref.watch(authProvider).perfil;
    if (perfil == null) return const SizedBox.shrink();
    // El panel es complementario: si falla o aún carga, la pantalla de inicio sigue mostrando el perfil.
    final panel = ref.watch(dashboardProvider).value;

    return SingleChildScrollView(
      padding: EdgeInsets.all(Pantalla.margen(MediaQuery.sizeOf(context).width) + 4),
      child: ContenidoCentrado(
        anchoMaximo: 1000,
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          _Cabecera(perfil: perfil),
          if (perfil.tieneRol(Rol.profesionalSanitario)) const _AccionesRapidas(),
          if (panel?.gestion != null) _PanelGestion(panel!.gestion!),
          if (panel?.profesional != null) _PanelProfesional(panel!.profesional!),
        ]),
      ),
    );
  }
}

class _Cabecera extends StatelessWidget {
  const _Cabecera({required this.perfil});
  final PerfilUsuario perfil;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tema = Theme.of(context);
    final cs = tema.colorScheme;
    final nombre = perfil.nombreCompleto.trim();
    final inicial = nombre.isEmpty ? '?' : nombre.characters.first.toUpperCase();
    final mfa = perfil.mfaActivado;

    const blanco = Colors.white;
    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        gradient: TemaApp.degradadoMarca,
        borderRadius: BorderRadius.circular(24),
        boxShadow: [BoxShadow(color: cs.primary.withAlpha(60), blurRadius: 24, offset: const Offset(0, 10))],
      ),
      child: Stack(children: [
        Positioned(
          right: -40,
          top: -50,
          child: Container(width: 190, height: 190, decoration: BoxDecoration(shape: BoxShape.circle, color: blanco.withAlpha(22))),
        ),
        Positioned(
          right: 70,
          bottom: -70,
          child: Container(width: 140, height: 140, decoration: BoxDecoration(shape: BoxShape.circle, color: blanco.withAlpha(16))),
        ),
        Padding(
          padding: const EdgeInsets.all(24),
          child: Row(children: [
            Container(
              width: 64,
              height: 64,
              alignment: Alignment.center,
              decoration: BoxDecoration(color: blanco.withAlpha(40), shape: BoxShape.circle, border: Border.all(color: blanco.withAlpha(110), width: 2)),
              child: Text(inicial, style: tema.textTheme.headlineSmall?.copyWith(color: blanco)),
            ),
            const SizedBox(width: 18),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(t.bienvenida(nombre), style: tema.textTheme.headlineSmall?.copyWith(color: blanco)),
                const SizedBox(height: 4),
                Text([perfil.email, if (perfil.organizacion != null) perfil.organizacion!].join(' · '),
                    style: tema.textTheme.bodyMedium?.copyWith(color: blanco.withAlpha(215))),
                const SizedBox(height: 14),
                Wrap(spacing: 8, runSpacing: 8, children: [
                  for (final r in perfil.roles) _Etiqueta(Icons.badge_outlined, etiquetaRol(t, r)),
                  _Etiqueta(mfa ? Icons.verified_user_outlined : Icons.gpp_maybe_outlined,
                      '${t.perfilMfa}: ${mfa ? t.mfaActiva : t.mfaInactiva}',
                      acento: mfa ? const Color(0xFF9BF0B0) : const Color(0xFFFFD27A)),
                ]),
              ]),
            ),
          ]),
        ),
      ]),
    );
  }
}

/// Accesos directos del profesional: declarar disponibilidad y solicitar ausencias sin buscar en el menú.
class _AccionesRapidas extends StatelessWidget {
  const _AccionesRapidas();

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    Future<void> abrir(Widget dialogo) async {
      if (await showDialog<bool>(context: context, builder: (_) => dialogo) == true && context.mounted) {
        context.go('/mi-disponibilidad');
      }
    }

    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: LayoutBuilder(builder: (context, c) {
        final dos = c.maxWidth >= 520;
        final ancho = dos ? (c.maxWidth - 12) / 2 : c.maxWidth;
        return Wrap(spacing: 12, runSpacing: 12, children: [
          SizedBox(
            width: ancho,
            child: _Acceso(
              icono: Icons.event_available_outlined,
              titulo: t.declararDisponibilidad,
              color: const Color(0xFF0B6E8A),
              onTap: () => abrir(const DialogoDisponibilidad()),
            ),
          ),
          SizedBox(
            width: ancho,
            child: _Acceso(
              icono: Icons.beach_access_outlined,
              titulo: t.solicitarAusencia,
              color: const Color(0xFF7C5CE0),
              onTap: () => abrir(const DialogoAusencia()),
            ),
          ),
        ]);
      }),
    );
  }
}

class _Acceso extends StatelessWidget {
  const _Acceso({required this.icono, required this.titulo, required this.color, required this.onTap});
  final IconData icono;
  final String titulo;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Card(
      margin: EdgeInsets.zero,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(12)),
              child: Icon(icono, color: Colors.white),
            ),
            const SizedBox(width: 14),
            Expanded(child: Text(titulo, style: tema.textTheme.titleSmall)),
            Icon(Icons.add_circle_outline, color: color),
          ]),
        ),
      ),
    );
  }
}

class _Etiqueta extends StatelessWidget {
  const _Etiqueta(this.icono, this.texto, {this.acento = Colors.white});
  final IconData icono;
  final String texto;
  final Color acento;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: Colors.white.withAlpha(36),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: Colors.white.withAlpha(70)),
      ),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        Icon(icono, size: 15, color: acento),
        const SizedBox(width: 6),
        Flexible(child: Text(texto, style: Theme.of(context).textTheme.labelMedium?.copyWith(color: Colors.white))),
      ]),
    );
  }
}

class _Seccion extends StatelessWidget {
  const _Seccion({required this.titulo, required this.icono, required this.child});
  final String titulo;
  final IconData icono;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final cs = tema.colorScheme;
    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(color: cs.primary.withAlpha(24), borderRadius: BorderRadius.circular(10)),
              child: Icon(icono, color: cs.primary, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(child: Text(titulo, style: tema.textTheme.titleMedium)),
          ]),
          const SizedBox(height: 16),
          child,
        ]),
      ),
    );
  }
}

class _PanelGestion extends StatelessWidget {
  const _PanelGestion(this.g);
  final DashboardGestionDto g;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final p = g.pendientes;
    final tema = Theme.of(context);
    final color = g.porcentaje >= 95 ? _correcto : (g.porcentaje >= 75 ? _alerta : tema.colorScheme.error);
    final ratio = g.plazasTotales == 0 ? 1.0 : (g.plazasCubiertas / g.plazasTotales).clamp(0.0, 1.0);

    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      _Seccion(
        titulo: t.dashCoberturaTitulo,
        icono: Icons.donut_large_outlined,
        child: Row(children: [
          SizedBox(
            width: 96,
            height: 96,
            child: Stack(alignment: Alignment.center, children: [
              SizedBox.expand(
                child: CircularProgressIndicator(
                  value: ratio,
                  strokeWidth: 10,
                  strokeCap: StrokeCap.round,
                  color: color,
                  backgroundColor: color.withAlpha(35),
                ),
              ),
              Text('${(ratio * 100).round()}%', style: tema.textTheme.titleLarge?.copyWith(color: color, fontWeight: FontWeight.w700)),
            ]),
          ),
          const SizedBox(width: 24),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(t.dashPlazasCubiertas(g.plazasCubiertas, g.plazasTotales),
                  style: tema.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              Row(children: [
                Icon(Icons.edit_note_outlined, size: 18, color: tema.colorScheme.onSurfaceVariant),
                const SizedBox(width: 6),
                Flexible(
                  child: Text('${t.dashBorradores}: ${g.cuadrantesBorrador}',
                      style: tema.textTheme.bodyMedium?.copyWith(color: tema.colorScheme.onSurfaceVariant)),
                ),
              ]),
            ]),
          ),
        ]),
      ),
      if (p != null)
        _Seccion(
          titulo: t.dashPendientes,
          icono: Icons.pending_actions_outlined,
          child: LayoutBuilder(builder: (context, c) {
            final columnas = c.maxWidth >= 760 ? 5 : (c.maxWidth >= 480 ? 3 : 2);
            const sep = 12.0;
            final ancho = (c.maxWidth - sep * (columnas - 1)) / columnas;
            Widget ind(String e, int v, IconData i, String r, Color col, {bool alertar = false}) =>
                SizedBox(width: ancho, child: _Indicador(e, v, icono: i, alertar: alertar, ruta: r, color: col));
            return Wrap(spacing: sep, runSpacing: sep, children: [
              ind(t.dashVacantes, p.vacantesAbiertas, Icons.person_search_outlined, '/ausencias', const Color(0xFFE5484D), alertar: p.vacantesAbiertas > 0),
              ind(t.dashAusencias, p.ausenciasSolicitadas, Icons.beach_access_outlined, '/ausencias', const Color(0xFFE08A00), alertar: p.ausenciasSolicitadas > 0),
              ind(t.dashOfertas, p.ofertasPorAprobar, Icons.local_offer_outlined, '/ausencias', const Color(0xFF7C5CE0), alertar: p.ofertasPorAprobar > 0),
              ind(t.dashDisponibilidades, p.disponibilidadesPendientes, Icons.event_available_outlined, '/ausencias', const Color(0xFF0B6E8A)),
              ind(t.dashExcepciones, p.excepcionesUltimos30Dias, Icons.rule_outlined, '/reglas', const Color(0xFF2E9E6B)),
            ]);
          }),
        ),
      if (g.proximosSinCubrir.isNotEmpty)
        _Seccion(
          titulo: t.dashSinCubrir,
          icono: Icons.warning_amber_outlined,
          child: Column(children: [
            for (final s in g.proximosSinCubrir)
              ListTile(
                contentPadding: EdgeInsets.zero,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                leading: CircleAvatar(
                  backgroundColor: _alerta.withAlpha(35),
                  child: const Icon(Icons.warning_amber_outlined, color: _alerta),
                ),
                title: Text('${formatearFecha(s.fecha)} · ${s.turno}'),
                subtitle: Text('${s.centro} · ${s.servicio}'),
                trailing: Chip(
                  visualDensity: VisualDensity.compact,
                  label: Text(t.dashLibres(s.plazasLibres)),
                  side: BorderSide.none,
                  backgroundColor: _alerta.withAlpha(35),
                ),
                onTap: () => context.go('/planificacion?fecha=${s.fecha}'),
              ),
          ]),
        ),
    ]);
  }
}

class _Indicador extends StatelessWidget {
  const _Indicador(this.etiqueta, this.valor, {required this.icono, required this.alertar, required this.ruta, required this.color});
  final Color color;
  final String etiqueta;
  final int valor;
  final IconData icono;
  final bool alertar;
  final String ruta;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return InkWell(
      onTap: () => context.go(ruta),
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [color.withAlpha(alertar ? 48 : 30), color.withAlpha(10)],
          ),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: color.withAlpha(alertar ? 130 : 60)),
        ),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Container(
            padding: const EdgeInsets.all(7),
            decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(10)),
            child: Icon(icono, color: Colors.white, size: 18),
          ),
          const SizedBox(height: 10),
          Text('$valor',
              style: Theme.of(context).textTheme.headlineMedium?.copyWith(color: color, fontWeight: FontWeight.w700, height: 1)),
          const SizedBox(height: 6),
          Text(etiqueta, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
        ]),
      ),
    );
  }
}

class _PanelProfesional extends StatelessWidget {
  const _PanelProfesional(this.p);
  final DashboardProfesionalDto p;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tema = Theme.of(context);
    final cs = tema.colorScheme;
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      _Seccion(
        titulo: t.dashProximosTurnos,
        icono: Icons.event_outlined,
        child: p.proximosTurnos.isEmpty
            ? Text(t.dashSinProximos, style: tema.textTheme.bodyMedium?.copyWith(color: cs.onSurfaceVariant))
            : Column(children: [
                for (final x in p.proximosTurnos)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: CircleAvatar(
                      backgroundColor: cs.primaryContainer,
                      child: Icon(Icons.event_available_outlined, color: cs.onPrimaryContainer),
                    ),
                    title: Text('${formatearFecha(x.fecha)} · ${x.turno}'),
                    subtitle: Text('${x.centro} · ${x.servicio}'),
                  ),
              ]),
      ),
      LayoutBuilder(builder: (context, c) {
        final columnas = c.maxWidth >= 560 ? 3 : 1;
        const sep = 12.0;
        final ancho = (c.maxWidth - sep * (columnas - 1)) / columnas;
        return Wrap(spacing: sep, runSpacing: sep, children: [
          SizedBox(width: ancho, child: _Resumen(t.dashHorasMes, formatearMinutos(p.horasPlanificadasMesMin), Icons.schedule_outlined)),
          SizedBox(width: ancho, child: _Resumen(t.dashOfertasPendientes, '${p.ofertasPendientes}', Icons.local_offer_outlined)),
          SizedBox(width: ancho, child: _Resumen(t.menuAusencias, '${p.ausenciasSolicitadas}', Icons.beach_access_outlined)),
        ]);
      }),
    ]);
  }
}

class _Resumen extends StatelessWidget {
  const _Resumen(this.etiqueta, this.valor, this.icono);
  final String etiqueta;
  final String valor;
  final IconData icono;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: tema.colorScheme.outlineVariant),
      ),
      child: Row(children: [
        Icon(icono, color: tema.colorScheme.primary),
        const SizedBox(width: 12),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(valor, style: tema.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700)),
            Text(etiqueta, style: tema.textTheme.bodySmall?.copyWith(color: tema.colorScheme.onSurfaceVariant)),
          ]),
        ),
      ]),
    );
  }
}
