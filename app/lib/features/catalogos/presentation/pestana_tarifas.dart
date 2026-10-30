import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../estructura/data/estructura_repositorio.dart';
import '../data/catalogos_repositorio.dart';
import '../data/tarifas_repositorio.dart';

/// Coste por turno de cada perfil, servicio a servicio.
class PestanaTarifas extends ConsumerStatefulWidget {
  const PestanaTarifas({super.key});

  @override
  ConsumerState<PestanaTarifas> createState() => _EstadoPestanaTarifas();
}

class _EstadoPestanaTarifas extends ConsumerState<PestanaTarifas> {
  String? _servicioId;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final servicios = ref.watch(serviciosActivosProvider);
    final perfiles = ref.watch(catalogoActivoProvider(TipoCatalogo.categorias));

    return EstadoAsincrono(
      valor: servicios,
      alReintentar: () => ref.invalidate(serviciosActivosProvider),
      constructor: (lista) => EstadoAsincrono(
        valor: perfiles,
        alReintentar: () => ref.invalidate(catalogoActivoProvider(TipoCatalogo.categorias)),
        constructor: (cats) {
          final servicio = lista.where((s) => s.id == _servicioId).firstOrNull;
          return ContenidoCentrado(
            child: ListView(padding: const EdgeInsets.fromLTRB(16, 12, 16, 32), children: [
              Text(t.tarifasAyuda, style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: servicio?.id,
                isExpanded: true,
                decoration: InputDecoration(labelText: t.tarifasServicio),
                items: [
                  for (final s in lista) DropdownMenuItem(value: s.id, child: Text('${s.centroNombre} · ${s.nombre}', overflow: TextOverflow.ellipsis)),
                ],
                onChanged: (v) => setState(() => _servicioId = v),
              ),
              const SizedBox(height: 12),
              if (servicio == null)
                EstadoVacio(mensaje: t.tarifasServicio, icono: Icons.payments_outlined)
              else if (cats.isEmpty)
                Text(t.tarifasSinPerfiles)
              else
                _EditorTarifas(key: ValueKey(servicio.id), servicio: servicio, perfiles: cats),
            ]),
          );
        },
      ),
    );
  }
}

class _EditorTarifas extends ConsumerStatefulWidget {
  const _EditorTarifas({super.key, required this.servicio, required this.perfiles});
  final ServicioDto servicio;
  final List<CatalogoDto> perfiles;

  @override
  ConsumerState<_EditorTarifas> createState() => _EstadoEditorTarifas();
}

class _EstadoEditorTarifas extends ConsumerState<_EditorTarifas> {
  final _controles = <String, TextEditingController>{};
  bool _cargado = false;
  bool _enviando = false;
  String? _error;

  String _clave(String tipo, String perfil) => '$tipo|$perfil';

  TextEditingController _control(String tipo, String perfil) =>
      _controles.putIfAbsent(_clave(tipo, perfil), () => TextEditingController());

  @override
  void dispose() {
    for (final c in _controles.values) {
      c.dispose();
    }
    super.dispose();
  }

  String _formato(double v) => v.toStringAsFixed(2).replaceFirst(RegExp(r'\.?0+$'), '').replaceAll('.', ',');

  /// null = vacío (sin tarifa); NaN = no válido.
  double? _importe(String texto) {
    final v = texto.trim().replaceAll(',', '.');
    if (v.isEmpty) return null;
    final n = double.tryParse(v);
    return n == null || n < 0 || n > 1000000 ? double.nan : n;
  }

  Future<void> _guardar() async {
    final t = AppLocalizations.of(context);
    final tarifas = <TarifaDto>[];
    for (final e in _controles.entries) {
      final importe = _importe(e.value.text);
      if (importe == null) continue;
      if (importe.isNaN) {
        setState(() => _error = t.validaImporte);
        return;
      }
      final partes = e.key.split('|');
      tarifas.add(TarifaDto(tipoTurnoId: partes[0], categoriaProfesionalId: partes[1], importe: importe));
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(tarifasRepositorioProvider).guardar(widget.servicio.id, tarifas);
      ref.invalidate(tarifasDeServicioProvider(widget.servicio.id));
      if (mounted) mostrarMensaje(context, t.tarifasGuardado);
    } on FalloApi catch (f) {
      if (mounted) setState(() => _error = f.errores.isEmpty ? mensajeDeFallo(t, f) : f.errores.values.expand((e) => e).join(' '));
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tema = Theme.of(context);
    final tarifas = ref.watch(tarifasDeServicioProvider(widget.servicio.id));
    final tipos = widget.servicio.tiposTurno;

    return EstadoAsincrono(
      valor: tarifas,
      alReintentar: () => ref.invalidate(tarifasDeServicioProvider(widget.servicio.id)),
      constructor: (existentes) {
        if (!_cargado) {
          for (final x in existentes) {
            _control(x.tipoTurnoId, x.categoriaProfesionalId).text = _formato(x.importe);
          }
          _cargado = true;
        }
        return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          if (tipos.isEmpty) Text(t.tarifasSinTurnos),
          for (final tipo in tipos)
            Card(
              margin: const EdgeInsets.symmetric(vertical: 6),
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [
                    Icon(tipo.esGuardia ? Icons.nightlight_outlined : Icons.schedule, size: 20),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text('${tipo.nombre} · ${tipo.franja}',
                          style: tema.textTheme.titleSmall?.copyWith(decoration: tipo.activo ? null : TextDecoration.lineThrough)),
                    ),
                  ]),
                  const SizedBox(height: 10),
                  Wrap(spacing: 12, runSpacing: 12, children: [
                    for (final p in widget.perfiles)
                      SizedBox(
                        width: 190,
                        child: TextField(
                          controller: _control(tipo.id, p.id),
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
                          decoration: InputDecoration(labelText: p.nombre, suffixText: '€', helperText: t.tarifasImporte, isDense: true),
                        ),
                      ),
                  ]),
                ]),
              ),
            ),
          if (_error != null)
            Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: TextStyle(color: tema.colorScheme.error))),
          const SizedBox(height: 12),
          Wrap(spacing: 12, runSpacing: 8, children: [
            FilledButton.icon(
              icon: const Icon(Icons.save_outlined),
              label: Text(t.botonGuardar),
              onPressed: _enviando || tipos.isEmpty ? null : _guardar,
            ),
            OutlinedButton.icon(
              icon: const Icon(Icons.schedule),
              label: Text(t.tarifasGestionarTurnos),
              onPressed: () => context.go('/servicios/${widget.servicio.id}'),
            ),
          ]),
        ]);
      },
    );
  }
}
