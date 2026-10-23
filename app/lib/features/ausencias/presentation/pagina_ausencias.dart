import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../planificacion/presentation/hoja_turno.dart' show DialogoCandidatos;
import '../data/ausencias_repositorio.dart';
import 'dialogo_ausencia.dart';
import 'etiquetas_ausencias.dart';

/// Gestión de ausencias, vacantes y disponibilidad (coordinación).
class PaginaAusencias extends StatelessWidget {
  const PaginaAusencias({super.key});

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return DefaultTabController(
      length: 3,
      child: Column(children: [
        TabBar(tabs: [Tab(text: t.ausTabSolicitudes), Tab(text: t.ausTabVacantes), Tab(text: t.ausTabDisponibilidad)]),
        const Expanded(child: TabBarView(children: [_ListaAusencias(), _ListaVacantes(), _ListaDisponibilidad()])),
      ]),
    );
  }
}

/// Pide un motivo; devuelve null si se cancela. Con [obligatorio] exige texto.
Future<String?> _pedirMotivo(BuildContext context, String titulo, {required bool obligatorio}) {
  return showDialog<String>(
    context: context,
    builder: (_) {
      final t = AppLocalizations.of(context);
      final c = TextEditingController();
      return StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(titulo),
          content: TextField(controller: c, maxLength: 300, maxLines: 2, onChanged: (_) => setState(() {}), decoration: InputDecoration(labelText: obligatorio ? t.motivoObligatorio : t.motivoOpcional)),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: Text(t.botonCancelar)),
            FilledButton(onPressed: obligatorio && c.text.trim().isEmpty ? null : () => Navigator.pop(ctx, c.text.trim()), child: Text(titulo)),
          ],
        ),
      );
    },
  );
}

class _ListaAusencias extends ConsumerWidget {
  const _ListaAusencias();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final datos = ref.watch(ausenciasProvider(null));
    final repo = ref.read(ausenciasRepositorioProvider);

    void refrescar() {
      ref.invalidate(ausenciasProvider);
      ref.invalidate(vacantesProvider);
    }

    Future<void> ejecutar(Future<void> Function() accion) async {
      try {
        await accion();
        refrescar();
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
    }

    Future<void> aprobar(AusenciaDto a) => ejecutar(() async {
          final r = await repo.aprobar(a.id);
          if (context.mounted) mostrarMensaje(context, t.vacantesCreadas(r.vacantes.length));
        });

    Future<void> rechazar(AusenciaDto a) async {
      final motivo = await _pedirMotivo(context, t.accionRechazar, obligatorio: true);
      if (motivo != null) await ejecutar(() => repo.rechazar(a.id, motivo));
    }

    Future<void> cancelar(AusenciaDto a) async {
      final motivo = await _pedirMotivo(context, t.accionCancelarAusencia, obligatorio: false);
      if (motivo != null) await ejecutar(() => repo.cancelar(a.id, motivo: motivo.isEmpty ? null : motivo));
    }

    Future<void> nueva() async {
      if (await showDialog<bool>(context: context, builder: (_) => const DialogoAusencia(coordinador: true)) == true) refrescar();
    }

    return Scaffold(
      floatingActionButton: FloatingActionButton.extended(heroTag: 'fab-ausencia', icon: const Icon(Icons.add), label: Text(t.registrarAusencia), onPressed: nueva),
      body: EstadoAsincrono(
        valor: datos,
        alReintentar: () => ref.invalidate(ausenciasProvider),
        constructor: (lista) => lista.isEmpty
            ? Center(child: Text(t.sinAusencias))
            : ContenidoCentrado(
                child: ListView(padding: const EdgeInsets.only(bottom: 88, top: 8), children: [
                  for (final a in lista)
                    Card(
                      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                      child: ListTile(
                        isThreeLine: true,
                        title: Text('${a.profesional} · ${etiquetaTipoAusencia(t, a.tipo)}'),
                        subtitle: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text('${rangoFechasApi(a.desde, a.hasta)} · ${t.diasNumero(a.dias)}'),
                          if (a.comentario != null) Text(a.comentario!),
                          Align(alignment: Alignment.centerLeft, child: ChipEstadoAusencia(etiqueta: etiquetaEstadoAusencia(t, a.estado), color: colorEstadoAusencia(context, a.estado))),
                        ]),
                        trailing: a.vigente
                            ? PopupMenuButton<String>(
                                onSelected: (x) => switch (x) { 'aprobar' => aprobar(a), 'rechazar' => rechazar(a), _ => cancelar(a) },
                                itemBuilder: (_) => [
                                  if (a.estado == EstadoAusencia.solicitada) PopupMenuItem(value: 'aprobar', child: Text(t.accionAprobar)),
                                  if (a.estado == EstadoAusencia.solicitada) PopupMenuItem(value: 'rechazar', child: Text(t.accionRechazar)),
                                  PopupMenuItem(value: 'cancelar', child: Text(t.accionCancelarAusencia)),
                                ],
                              )
                            : null,
                      ),
                    ),
                ]),
              ),
      ),
    );
  }
}

class _ListaVacantes extends ConsumerWidget {
  const _ListaVacantes();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final datos = ref.watch(vacantesProvider(EstadoVacante.abierta));
    final repo = ref.read(ausenciasRepositorioProvider);

    Future<void> cubrir(VacanteDto v) async {
      final ok = await showDialog<bool>(
        context: context,
        builder: (_) => DialogoCandidatos(
          necesidadId: v.necesidadId,
          asignar: (profesionalId, justificacion) => repo.cubrirVacante(v.id, profesionalId, justificacion: justificacion),
        ),
      );
      if (ok == true) {
        ref.invalidate(vacantesProvider);
        if (context.mounted) mostrarMensaje(context, t.vacanteCubierta);
      }
    }

    return EstadoAsincrono(
      valor: datos,
      alReintentar: () => ref.invalidate(vacantesProvider),
      constructor: (lista) => lista.isEmpty
          ? Center(child: Text(t.sinVacantes))
          : ContenidoCentrado(
              child: ListView(padding: const EdgeInsets.symmetric(vertical: 8), children: [
                for (final v in lista)
                  Card(
                    margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    child: ListTile(
                      isThreeLine: true,
                      leading: Icon(Icons.person_off_outlined, color: Colors.orange.shade800),
                      title: Text('${formatearFecha(v.fecha)} · ${v.turno}'),
                      subtitle: Text('${v.centro} · ${v.servicio}${v.categoria == null ? '' : ' · ${v.categoria}'}\n${t.vacanteAusente(v.profesionalAusente)} · ${v.motivo}'),
                      trailing: FilledButton(onPressed: () => cubrir(v), child: Text(t.accionCubrir)),
                    ),
                  ),
              ]),
            ),
    );
  }
}

class _ListaDisponibilidad extends ConsumerWidget {
  const _ListaDisponibilidad();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final datos = ref.watch(disponibilidadesPendientesProvider);
    final repo = ref.read(ausenciasRepositorioProvider);

    Future<void> decidir(DisponibilidadDto d, bool aprobar) async {
      String? motivo;
      if (!aprobar) {
        motivo = await _pedirMotivo(context, t.accionRechazar, obligatorio: true);
        if (motivo == null) return;
      }
      try {
        await repo.decidirDisponibilidad(d.id, aprobar: aprobar, motivo: motivo);
        ref.invalidate(disponibilidadesPendientesProvider);
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
    }

    return EstadoAsincrono(
      valor: datos,
      alReintentar: () => ref.invalidate(disponibilidadesPendientesProvider),
      constructor: (lista) => lista.isEmpty
          ? Center(child: Text(t.sinDeclaraciones))
          : ContenidoCentrado(
              child: ListView(padding: const EdgeInsets.symmetric(vertical: 8), children: [
                for (final d in lista)
                  Card(
                    margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    child: ListTile(
                      isThreeLine: true,
                      title: Text('${d.profesional} · ${etiquetaTipoDisponibilidad(t, d.tipo)}'),
                      subtitle: Text([rangoFechasApi(d.desde, d.hasta), etiquetaDias(t, d.dias), if (d.tipoTurno != null) d.tipoTurno!, if (d.comentario != null) d.comentario!].join(' · ')),
                      trailing: Row(mainAxisSize: MainAxisSize.min, children: [
                        IconButton(tooltip: t.accionAprobar, icon: const Icon(Icons.check_circle_outline), onPressed: () => decidir(d, true)),
                        IconButton(tooltip: t.accionRechazar, icon: const Icon(Icons.cancel_outlined), onPressed: () => decidir(d, false)),
                      ]),
                    ),
                  ),
              ]),
            ),
    );
  }
}
