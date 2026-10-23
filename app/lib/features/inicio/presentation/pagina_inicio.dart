import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../informes/data/informes_repositorio.dart';

class PaginaInicio extends ConsumerWidget {
  const PaginaInicio({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final perfil = ref.watch(authProvider).perfil;
    if (perfil == null) return const SizedBox.shrink();
    // El panel es complementario: si falla o aún carga, la pantalla de inicio sigue mostrando el perfil.
    final panel = ref.watch(dashboardProvider).value;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: ContenidoCentrado(
        anchoMaximo: 760,
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(t.bienvenida(perfil.nombreCompleto), style: Theme.of(context).textTheme.headlineSmall),
                const SizedBox(height: 4),
                Text(perfil.email),
                const Divider(height: 32),
                if (perfil.organizacion != null) _Dato(etiqueta: t.perfilSociedad, valor: perfil.organizacion!),
                _Dato(etiqueta: t.perfilRoles, valor: perfil.roles.map((r) => etiquetaRol(t, r)).join(', ')),
                _Dato(etiqueta: t.perfilMfa, valor: perfil.mfaActivado ? t.mfaActiva : t.mfaInactiva),
              ]),
            ),
          ),
          if (panel?.gestion != null) _PanelGestion(panel!.gestion!),
          if (panel?.profesional != null) _PanelProfesional(panel!.profesional!),
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
    final color = g.porcentaje >= 95 ? Colors.green.shade700 : (g.porcentaje >= 75 ? Colors.orange.shade800 : tema.colorScheme.error);
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(t.dashCoberturaTitulo, style: tema.textTheme.titleMedium),
            const SizedBox(height: 8),
            Text(t.dashPlazasCubiertas(g.plazasCubiertas, g.plazasTotales), style: tema.textTheme.headlineSmall?.copyWith(color: color)),
            const SizedBox(height: 8),
            LinearProgressIndicator(value: g.plazasTotales == 0 ? 1 : g.plazasCubiertas / g.plazasTotales, color: color, minHeight: 8),
            const SizedBox(height: 8),
            Text('${t.dashBorradores}: ${g.cuadrantesBorrador}'),
          ]),
        ),
      ),
      if (p != null)
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(t.dashPendientes, style: tema.textTheme.titleMedium),
              const SizedBox(height: 8),
              Wrap(spacing: 12, runSpacing: 12, children: [
                _Indicador(t.dashVacantes, p.vacantesAbiertas, alertar: p.vacantesAbiertas > 0, ruta: '/ausencias'),
                _Indicador(t.dashAusencias, p.ausenciasSolicitadas, alertar: p.ausenciasSolicitadas > 0, ruta: '/ausencias'),
                _Indicador(t.dashOfertas, p.ofertasPorAprobar, alertar: p.ofertasPorAprobar > 0, ruta: '/ausencias'),
                _Indicador(t.dashDisponibilidades, p.disponibilidadesPendientes, alertar: false, ruta: '/ausencias'),
                _Indicador(t.dashExcepciones, p.excepcionesUltimos30Dias, alertar: false, ruta: '/reglas'),
              ]),
            ]),
          ),
        ),
      if (g.proximosSinCubrir.isNotEmpty)
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(t.dashSinCubrir, style: tema.textTheme.titleMedium),
              for (final s in g.proximosSinCubrir)
                ListTile(
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.warning_amber_outlined, color: Colors.orange.shade800),
                  title: Text('${formatearFecha(s.fecha)} · ${s.turno}'),
                  subtitle: Text('${s.centro} · ${s.servicio}'),
                  trailing: Text(t.dashLibres(s.plazasLibres)),
                  onTap: () => context.go('/planificacion?fecha=${s.fecha}'),
                ),
            ]),
          ),
        ),
    ]);
  }
}

class _Indicador extends StatelessWidget {
  const _Indicador(this.etiqueta, this.valor, {required this.alertar, required this.ruta});
  final String etiqueta;
  final int valor;
  final bool alertar;
  final String ruta;

  @override
  Widget build(BuildContext context) {
    final color = alertar ? Colors.orange.shade800 : Theme.of(context).colorScheme.onSurface;
    return InkWell(
      onTap: () => context.go(ruta),
      borderRadius: BorderRadius.circular(8),
      child: Container(
        width: 170,
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(border: Border.all(color: Theme.of(context).colorScheme.outlineVariant), borderRadius: BorderRadius.circular(8)),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('$valor', style: Theme.of(context).textTheme.headlineMedium?.copyWith(color: color)),
          Text(etiqueta),
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
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(t.dashProximosTurnos, style: tema.textTheme.titleMedium),
          if (p.proximosTurnos.isEmpty) Padding(padding: const EdgeInsets.symmetric(vertical: 8), child: Text(t.dashSinProximos)),
          for (final x in p.proximosTurnos)
            ListTile(
              dense: true,
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.event_available_outlined),
              title: Text('${formatearFecha(x.fecha)} · ${x.turno}'),
              subtitle: Text('${x.centro} · ${x.servicio}'),
            ),
          const Divider(),
          _Dato(etiqueta: t.dashHorasMes, valor: formatearMinutos(p.horasPlanificadasMesMin)),
          _Dato(etiqueta: t.dashOfertasPendientes, valor: '${p.ofertasPendientes}'),
          _Dato(etiqueta: t.menuAusencias, valor: '${p.ausenciasSolicitadas}'),
        ]),
      ),
    );
  }
}

class _Dato extends StatelessWidget {
  const _Dato({required this.etiqueta, required this.valor});
  final String etiqueta;
  final String valor;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          SizedBox(width: 190, child: Text(etiqueta, style: Theme.of(context).textTheme.labelLarge)),
          Expanded(child: Text(valor)),
        ]),
      );
}
