import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../informes/data/informes_repositorio.dart';
import '../data/ausencias_repositorio.dart';
import 'etiquetas_ausencias.dart';

/// Una declaración por turno y servicio se guarda por separado (el mismo turno puede existir en varios servicios del
/// hospital); al usuario se le muestra como una sola.
List<List<DisponibilidadDto>> _agrupar(List<DisponibilidadDto> lista) {
  final grupos = <String, List<DisponibilidadDto>>{};
  for (final d in lista) {
    final clave = [
      d.tipo,
      d.desde,
      d.hasta,
      d.dias,
      d.centro,
      d.tipoTurno,
      d.comentario,
      d.estado,
      d.motivoDecision,
    ].join('|');
    grupos.putIfAbsent(clave, () => []).add(d);
  }
  return grupos.values.toList();
}

/// Ausencias y disponibilidad del propio profesional.
class PaginaMiDisponibilidad extends ConsumerWidget {
  const PaginaMiDisponibilidad({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final ausencias = ref.watch(misAusenciasProvider);
    final declaraciones = ref.watch(miDisponibilidadProvider);
    final ofertas = ref.watch(misOfertasProvider);
    final horas = ref.watch(misHorasProvider);
    final repo = ref.read(ausenciasRepositorioProvider);

    Future<void> ejecutar(Future<void> Function() accion) async {
      try {
        await accion();
        ref.invalidate(misAusenciasProvider);
        ref.invalidate(miDisponibilidadProvider);
        ref.invalidate(misOfertasProvider);
        ref.invalidate(noLeidasProvider);
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
    }

    return ContenidoCentrado(
      child: ListView(
        padding: const EdgeInsets.all(12),
        children: [
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              FilledButton.icon(
                icon: const Icon(Icons.beach_access_outlined),
                label: Text(t.solicitarAusencia),
                onPressed: () => context.go('/mi-disponibilidad/ausencia'),
              ),
              OutlinedButton.icon(
                icon: const Icon(Icons.event_busy_outlined),
                label: Text(t.declararDisponibilidad),
                onPressed: () => context.go('/mi-disponibilidad/nueva'),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Text(t.ofertasTitulo, style: Theme.of(context).textTheme.titleMedium),
          EstadoAsincrono(
            valor: ofertas,
            alReintentar: () => ref.invalidate(misOfertasProvider),
            constructor: (lista) => lista.isEmpty
                ? Padding(
                    padding: const EdgeInsets.all(16),
                    child: Text(t.sinOfertas),
                  )
                : Column(
                    children: [
                      for (final o in lista)
                        Card(
                          child: ListTile(
                            isThreeLine: true,
                            title: Text(
                              '${formatearFecha(o.fecha)} · ${o.turno}',
                            ),
                            subtitle: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('${o.centro} · ${o.servicio}'),
                                if (o.caduca != null &&
                                    o.estado == EstadoOferta.pendiente)
                                  Text(
                                    t.ofertaCaduca(fechaHoraLocal(o.caduca!)),
                                  ),
                                Align(
                                  alignment: Alignment.centerLeft,
                                  child: ChipEstadoAusencia(
                                    etiqueta: etiquetaEstadoOferta(t, o.estado),
                                    color: colorEstadoOferta(context, o.estado),
                                  ),
                                ),
                              ],
                            ),
                            trailing: o.estado == EstadoOferta.pendiente
                                ? Wrap(
                                    spacing: 4,
                                    children: [
                                      FilledButton(
                                        onPressed: () => ejecutar(
                                          () => repo.aceptarOferta(o.id),
                                        ),
                                        child: Text(t.ofertaAceptar),
                                      ),
                                      TextButton(
                                        onPressed: () => ejecutar(
                                          () => repo.rechazarOferta(o.id),
                                        ),
                                        child: Text(t.ofertaRechazar),
                                      ),
                                    ],
                                  )
                                : null,
                          ),
                        ),
                    ],
                  ),
          ),
          const SizedBox(height: 16),
          Text(t.misAusencias, style: Theme.of(context).textTheme.titleMedium),
          EstadoAsincrono(
            valor: ausencias,
            alReintentar: () => ref.invalidate(misAusenciasProvider),
            constructor: (lista) => lista.isEmpty
                ? Padding(
                    padding: const EdgeInsets.all(16),
                    child: Text(t.sinAusencias),
                  )
                : Column(
                    children: [
                      for (final a in lista)
                        Card(
                          child: ListTile(
                            title: Text(
                              '${etiquetaTipoAusencia(t, a.tipo)} · ${rangoFechasApi(a.desde, a.hasta)}',
                            ),
                            subtitle: Text(
                              [
                                t.diasNumero(a.dias),
                                if (a.comentario != null) a.comentario!,
                              ].join(' · '),
                            ),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                ChipEstadoAusencia(
                                  etiqueta: etiquetaEstadoAusencia(t, a.estado),
                                  color: colorEstadoAusencia(context, a.estado),
                                ),
                                if (a.vigente)
                                  IconButton(
                                    tooltip: t.accionCancelarAusencia,
                                    icon: const Icon(Icons.close),
                                    onPressed: () => ejecutar(
                                      () => repo.cancelarPropia(a.id),
                                    ),
                                  ),
                              ],
                            ),
                          ),
                        ),
                    ],
                  ),
          ),
          const SizedBox(height: 16),
          Text(
            t.misDeclaraciones,
            style: Theme.of(context).textTheme.titleMedium,
          ),
          EstadoAsincrono(
            valor: declaraciones,
            alReintentar: () => ref.invalidate(miDisponibilidadProvider),
            constructor: (lista) => lista.isEmpty
                ? Padding(
                    padding: const EdgeInsets.all(16),
                    child: Text(t.sinDeclaraciones),
                  )
                : Column(
                    children: [
                      for (final g in _agrupar(lista))
                        Builder(
                          builder: (context) {
                            final d = g.first;
                            return Card(
                              child: ListTile(
                                isThreeLine: true,
                                title: Text(
                                  '${etiquetaTipoDisponibilidad(t, d.tipo)} · ${rangoFechasApi(d.desde, d.hasta)}',
                                ),
                                subtitle: Text(
                                  [
                                    if (d.centro != null) d.centro!,
                                    etiquetaDias(t, d.dias),
                                    if (d.tipoTurno != null) d.tipoTurno!,
                                    if (d.comentario != null) d.comentario!,
                                    if (d.motivoDecision != null)
                                      d.motivoDecision!,
                                  ].join(' · '),
                                ),
                                trailing: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    ChipEstadoAusencia(
                                      etiqueta: etiquetaEstadoDisponibilidad(
                                        t,
                                        d.estado,
                                      ),
                                      color: switch (d.estado) {
                                        EstadoDisponibilidad.declarada =>
                                          Colors.orange.shade800,
                                        EstadoDisponibilidad.aprobada =>
                                          Colors.green.shade700,
                                        EstadoDisponibilidad.rechazada =>
                                          Theme.of(context).colorScheme.error,
                                      },
                                    ),
                                    IconButton(
                                      tooltip: t.accionRetirar,
                                      icon: const Icon(Icons.delete_outline),
                                      onPressed: () => ejecutar(() async {
                                        for (final x in g) {
                                          await repo.retirarDeclaracion(x.id);
                                        }
                                      }),
                                    ),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
                    ],
                  ),
          ),
          const SizedBox(height: 16),
          Text(t.misHoras, style: Theme.of(context).textTheme.titleMedium),
          EstadoAsincrono(
            valor: horas,
            alReintentar: () => ref.invalidate(misHorasProvider),
            constructor: (lista) => lista.isEmpty
                ? Padding(
                    padding: const EdgeInsets.all(16),
                    child: Text(t.sinDatosPeriodo),
                  )
                : Column(
                    children: [
                      for (final l in lista.reversed)
                        Card(
                          child: ListTile(
                            title: Text(
                              '${formatearFecha(l.fecha)} · ${l.turno}',
                            ),
                            subtitle: Text(
                              '${t.colPlanificadas}: ${formatearMinutos(l.planificadasMin)}'
                              '${l.registrada ? ' · ${t.colEfectivas}: ${formatearMinutos(l.efectivasMin!)}' : ''}',
                            ),
                          ),
                        ),
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}
