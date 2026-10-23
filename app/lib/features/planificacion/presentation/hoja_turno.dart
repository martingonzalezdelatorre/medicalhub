import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/justificacion.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../data/planificacion_repositorio.dart';
import 'tarjeta_turno.dart';

final candidatosProvider = FutureProvider.autoDispose.family<List<CandidatoDto>, String>(
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
                            if (await showDialog<bool>(context: context, builder: (_) => DialogoCandidatos(
                                necesidadId: n.id,
                                asignar: (profesionalId, justificacion) => repo.asignar(n.id, profesionalId, justificacion: justificacion),
                              )) == true) {
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

class DialogoCandidatos extends ConsumerStatefulWidget {
  const DialogoCandidatos({super.key, required this.necesidadId, required this.asignar});
  final String necesidadId;

  /// Realiza la asignación (normal o de una vacante). Recibe el profesional y la justificación de la excepción, si la hay.
  final Future<ResultadoAsignacionDto> Function(String profesionalId, String? justificacion) asignar;

  @override
  ConsumerState<DialogoCandidatos> createState() => _EstadoDialogoCandidatos();
}

class _EstadoDialogoCandidatos extends ConsumerState<DialogoCandidatos> {
  String? _error;
  String? _asignando;

  Future<void> _asignar(CandidatoDto c, {String? justificacion}) async {
    final t = AppLocalizations.of(context);
    setState(() {
      _asignando = c.profesionalId;
      _error = null;
    });
    try {
      final r = await widget.asignar(c.profesionalId, justificacion);
      if (!mounted) return;
      if (r.advertencias.isNotEmpty) mostrarMensaje(context, t.asignadoConAdvertencias(r.advertencias.length));
      if (r.excepciones.isNotEmpty) mostrarMensaje(context, t.asignadoConExcepcion);
      Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (mounted) {
        setState(() => _error = mensajeDeFallo(t, f));
        ref.invalidate(candidatosProvider(widget.necesidadId)); // otro usuario pudo cambiar la situación
      }
    } finally {
      if (mounted) setState(() => _asignando = null);
    }
  }

  Future<void> _conExcepcion(CandidatoDto c) async {
    final t = AppLocalizations.of(context);
    final justificacion = await pedirJustificacion(
      context,
      titulo: t.conExcepcionTitulo(c.nombreCompleto),
      infracciones: [for (final v in c.violaciones.where((v) => v.bloquea)) v.mensaje],
    );
    if (justificacion != null) await _asignar(c, justificacion: justificacion);
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final candidatos = ref.watch(candidatosProvider(widget.necesidadId));
    final aviso = Colors.orange.shade800;
    return AlertDialog(
      title: Text(t.candidatosTitulo),
      content: SizedBox(
        width: 520,
        height: 440,
        child: Column(children: [
          if (_error != null)
            Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
          Expanded(
            child: EstadoAsincrono(
              valor: candidatos,
              alReintentar: () => ref.invalidate(candidatosProvider(widget.necesidadId)),
              constructor: (lista) => ListView.separated(
                itemCount: lista.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (_, i) {
                  final c = lista[i];
                  final ocupado = _asignando != null;
                  final bloqueos = c.violaciones.where((v) => v.bloquea).map((v) => v.mensaje).toList();
                  final avisos = c.advertencias.map((v) => v.mensaje).toList();
                  // Sin detalle estructurado (API antigua) se muestran los motivos tal cual
                  final motivosSueltos = c.violaciones.isEmpty ? c.motivos : const <String>[];
                  final estado = c.elegible ? 0 : (c.requiereExcepcion ? 1 : 2);

                  return ListTile(
                    dense: true,
                    enabled: estado != 2,
                    leading: Icon(
                      switch (estado) { 0 => Icons.check_circle_outline, 1 => Icons.rule_folder_outlined, _ => Icons.block },
                      color: switch (estado) { 0 => Colors.green, 1 => aviso, _ => null },
                    ),
                    title: Text(c.nombreCompleto),
                    subtitle: (bloqueos.isEmpty && avisos.isEmpty && motivosSueltos.isEmpty && c.notas.isEmpty)
                        ? null
                        : Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            for (final m in [...bloqueos, ...motivosSueltos]) Text(m),
                            for (final m in avisos) Text(m, style: TextStyle(color: aviso)),
                            for (final m in c.notas) Text(m, style: const TextStyle(color: Colors.green)),
                          ]),
                    trailing: _asignando == c.profesionalId
                        ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                        : switch (estado) {
                            0 => FilledButton.tonal(onPressed: ocupado ? null : () => _asignar(c), child: Text(t.asignar)),
                            1 => c.puedeAutorizar
                                ? OutlinedButton(onPressed: ocupado ? null : () => _conExcepcion(c), child: Text(t.conExcepcion))
                                : Text(t.soloAdministrador, style: Theme.of(context).textTheme.bodySmall),
                            _ => null,
                          },
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
