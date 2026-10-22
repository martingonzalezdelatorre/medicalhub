import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../data/planificacion_repositorio.dart';
import 'tarjeta_turno.dart';

final _candidatosProvider = FutureProvider.autoDispose.family<List<CandidatoDto>, String>(
  (ref, necesidadId) => ref.watch(planificacionRepositorioProvider).candidatos(necesidadId),
);

/// Detalle de un turno: necesidades, profesionales asignados y, si procede, asignación y baja.
/// Se mantiene sincronizado con la consulta del calendario (`filtro`) para reflejar los cambios al instante.
class HojaTurno extends ConsumerWidget {
  const HojaTurno({super.key, required this.turnoId, required this.filtro, required this.puedeGestionar});

  final String turnoId;
  final FiltroCalendario filtro;
  final bool puedeGestionar;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final turnos = ref.watch(turnosProvider(filtro));
    final repo = ref.read(planificacionRepositorioProvider);
    final turno = turnos.value?.where((x) => x.id == turnoId).firstOrNull;

    if (turno == null) {
      return AlertDialog(content: EstadoAsincrono(valor: turnos, constructor: (_) => Text(t.sinElementos)), actions: [_Cerrar(t)]);
    }

    final editable = puedeGestionar && turno.editable;
    Future<void> ejecutar(Future<void> Function() accion, {bool cerrar = false}) async {
      try {
        await accion();
        ref.invalidate(turnosProvider(filtro));
        ref.invalidate(cuadrantesProvider);
        ref.invalidate(cuadranteProvider(turno.cuadranteId));
        if (cerrar && context.mounted) Navigator.pop(context);
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
    }

    return AlertDialog(
      title: Text('${turno.tipoTurno} · ${franjaTurno(turno)}'),
      content: SizedBox(
        width: 480,
        child: SingleChildScrollView(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: [
            Text('${formatearFecha(turno.fecha)} · ${turno.servicio} · ${turno.centro}'),
            Text(t.duracionHoras(turno.duracionHoras.toStringAsFixed(turno.duracionHoras % 1 == 0 ? 0 : 1)) +
                (turno.cruzaMedianoche ? ' · ${t.cruzaMedianoche}' : '')),
            if (!turno.editable)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(t.cuadranteNoEditable, style: Theme.of(context).textTheme.bodySmall),
              ),
            for (final n in turno.necesidades) ...[
              const Divider(height: 24),
              Text('${n.categoria ?? t.cualquierCategoria} · ${n.asignaciones.length}/${n.numeroPlazas}', style: Theme.of(context).textTheme.titleSmall),
              for (final a in n.asignaciones)
                ListTile(
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(Icons.person_outline),
                  title: Text(a.profesional),
                  trailing: editable
                      ? IconButton(tooltip: t.quitar, icon: const Icon(Icons.person_remove_outlined), onPressed: () => ejecutar(() => repo.quitarAsignacion(a.id)))
                      : null,
                ),
              if (n.plazasLibres > 0)
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: editable
                      ? OutlinedButton.icon(
                          icon: const Icon(Icons.person_add_alt),
                          label: Text(t.asignarProfesional(n.plazasLibres)),
                          onPressed: () async {
                            if (await showDialog<bool>(context: context, builder: (_) => _DialogoAsignar(necesidad: n)) == true) {
                              ref.invalidate(turnosProvider(filtro));
                              ref.invalidate(cuadrantesProvider);
                              ref.invalidate(cuadranteProvider(turno.cuadranteId));
                            }
                          },
                        )
                      : Text(t.plazasLibres(n.plazasLibres), style: TextStyle(color: Theme.of(context).colorScheme.error)),
                ),
            ],
            if (turno.necesidades.isEmpty) Padding(padding: const EdgeInsets.only(top: 12), child: Text(t.sinNecesidades)),
          ]),
        ),
      ),
      actions: [
        if (editable)
          TextButton(
            onPressed: () async {
              final ok = await showDialog<bool>(
                context: context,
                builder: (c) => AlertDialog(
                  title: Text(t.eliminarTurno),
                  content: Text(t.confirmaEliminarTurno),
                  actions: [
                    TextButton(onPressed: () => Navigator.pop(c, false), child: Text(t.botonCancelar)),
                    FilledButton(onPressed: () => Navigator.pop(c, true), child: Text(t.eliminar)),
                  ],
                ),
              );
              if (ok == true) await ejecutar(() => repo.eliminarTurno(turno.id), cerrar: true);
            },
            child: Text(t.eliminarTurno),
          ),
        _Cerrar(t),
      ],
    );
  }
}

class _Cerrar extends StatelessWidget {
  const _Cerrar(this.t);
  final AppLocalizations t;

  @override
  Widget build(BuildContext context) => TextButton(onPressed: () => Navigator.pop(context), child: Text(t.cerrar));
}

class _DialogoAsignar extends ConsumerStatefulWidget {
  const _DialogoAsignar({required this.necesidad});
  final NecesidadDto necesidad;

  @override
  ConsumerState<_DialogoAsignar> createState() => _EstadoDialogoAsignar();
}

class _EstadoDialogoAsignar extends ConsumerState<_DialogoAsignar> {
  String? _error;
  String? _asignando;

  Future<void> _asignar(CandidatoDto c) async {
    final t = AppLocalizations.of(context);
    setState(() {
      _asignando = c.profesionalId;
      _error = null;
    });
    try {
      await ref.read(planificacionRepositorioProvider).asignar(widget.necesidad.id, c.profesionalId);
      if (mounted) Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (mounted) {
        setState(() => _error = mensajeDeFallo(t, f));
        ref.invalidate(_candidatosProvider(widget.necesidad.id)); // otro usuario pudo cambiar la situación
      }
    } finally {
      if (mounted) setState(() => _asignando = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final candidatos = ref.watch(_candidatosProvider(widget.necesidad.id));
    return AlertDialog(
      title: Text(t.candidatosTitulo),
      content: SizedBox(
        width: 480,
        height: 420,
        child: Column(children: [
          if (_error != null)
            Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
          Expanded(
            child: EstadoAsincrono(
              valor: candidatos,
              alReintentar: () => ref.invalidate(_candidatosProvider(widget.necesidad.id)),
              constructor: (lista) => ListView.separated(
                itemCount: lista.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (_, i) {
                  final c = lista[i];
                  return ListTile(
                    dense: true,
                    enabled: c.elegible,
                    leading: Icon(c.elegible ? Icons.check_circle_outline : Icons.block, color: c.elegible ? Colors.green : null),
                    title: Text(c.nombreCompleto),
                    subtitle: c.motivos.isEmpty ? null : Text(c.motivos.join(' ')),
                    trailing: c.elegible
                        ? (_asignando == c.profesionalId
                            ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                            : FilledButton.tonal(onPressed: _asignando == null ? () => _asignar(c) : null, child: Text(t.asignar)))
                        : null,
                  );
                },
              ),
            ),
          ),
        ]),
      ),
      actions: [TextButton(onPressed: () => Navigator.pop(context, false), child: Text(t.botonCancelar))],
    );
  }
}
