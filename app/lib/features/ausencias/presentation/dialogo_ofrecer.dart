import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../planificacion/presentation/hoja_turno.dart' show candidatosProvider;
import '../data/ausencias_repositorio.dart';

/// Ofrece una vacante a varios candidatos a la vez. Solo se pueden elegir quienes podrían ser asignados
/// (sin infracciones o solo con infracciones que admiten excepción justificada).
class DialogoOfrecer extends ConsumerStatefulWidget {
  const DialogoOfrecer({super.key, required this.vacanteId, required this.necesidadId});
  final String vacanteId;
  final String necesidadId;

  @override
  ConsumerState<DialogoOfrecer> createState() => _EstadoDialogoOfrecer();
}

class _EstadoDialogoOfrecer extends ConsumerState<DialogoOfrecer> {
  static const _max = 10;
  final _horas = TextEditingController();
  final _elegidos = <String>{};
  bool _enviando = false;
  String? _error;

  @override
  void dispose() {
    _horas.dispose();
    super.dispose();
  }

  Future<void> _enviar() async {
    final t = AppLocalizations.of(context);
    final horas = _horas.text.trim().isEmpty ? null : int.tryParse(_horas.text.trim());
    if (_horas.text.trim().isNotEmpty && (horas == null || horas < 1 || horas > 720)) {
      setState(() => _error = t.ofrecerPlazo);
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(ausenciasRepositorioProvider).ofrecer(widget.vacanteId, _elegidos.toList(), caducaEnHoras: horas);
      if (mounted) Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (mounted) setState(() => _error = f.errores.isEmpty ? mensajeDeFallo(t, f) : f.errores.values.expand((e) => e).join(' '));
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final candidatos = ref.watch(candidatosProvider(widget.necesidadId));
    return AlertDialog(
      title: Text(t.ofrecerTitulo),
      content: SizedBox(
        width: 520,
        height: 460,
        child: Column(children: [
          Align(alignment: Alignment.centerLeft, child: Text(t.ofrecerSeleccion)),
          Expanded(
            child: EstadoAsincrono(
              valor: candidatos,
              alReintentar: () => ref.invalidate(candidatosProvider(widget.necesidadId)),
              constructor: (lista) {
                final validos = lista.where((c) => c.elegible || c.requiereExcepcion).toList();
                return ListView(children: [
                  for (final c in validos)
                    CheckboxListTile(
                      dense: true,
                      value: _elegidos.contains(c.profesionalId),
                      title: Text(c.nombreCompleto),
                      subtitle: (c.violaciones.isEmpty && c.notas.isEmpty)
                          ? null
                          : Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                              for (final v in c.violaciones) Text(v.mensaje, style: TextStyle(color: Colors.orange.shade800)),
                              for (final n in c.notas) Text(n, style: const TextStyle(color: Colors.green)),
                            ]),
                      onChanged: (v) => setState(() {
                        if (v == true) {
                          if (_elegidos.length < _max) _elegidos.add(c.profesionalId);
                        } else {
                          _elegidos.remove(c.profesionalId);
                        }
                      }),
                    ),
                ]);
              },
            ),
          ),
          TextField(controller: _horas, keyboardType: TextInputType.number, decoration: InputDecoration(labelText: t.ofrecerPlazo)),
          if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
        ]),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context, false), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando || _elegidos.isEmpty ? null : _enviar, child: Text(t.ofrecerEnviar)),
      ],
    );
  }
}
