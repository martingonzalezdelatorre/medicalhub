import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../planificacion/data/planificacion_repositorio.dart' show fechaParaApi;
import '../../profesionales/data/profesionales_repositorio.dart';
import '../data/ausencias_repositorio.dart';
import 'etiquetas_ausencias.dart';

/// Solicitud de ausencia. Con [coordinador] = true permite elegir el profesional y registrarla ya aprobada.
class DialogoAusencia extends ConsumerStatefulWidget {
  const DialogoAusencia({super.key, this.coordinador = false});
  final bool coordinador;

  @override
  ConsumerState<DialogoAusencia> createState() => _EstadoDialogoAusencia();
}

class _EstadoDialogoAusencia extends ConsumerState<DialogoAusencia> {
  final _comentario = TextEditingController();
  TipoAusencia _tipo = TipoAusencia.vacaciones;
  DateTime _desde = DateTime.now().add(const Duration(days: 7));
  late DateTime _hasta = _desde;
  String? _profesional;
  bool _aprobar = false;
  bool _enviando = false;
  String? _error;

  @override
  void dispose() {
    _comentario.dispose();
    super.dispose();
  }

  Future<void> _enviar() async {
    final t = AppLocalizations.of(context);
    if (widget.coordinador && _profesional == null) {
      setState(() => _error = '${t.campoProfesional}: ${t.validaObligatorio}');
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    final repo = ref.read(ausenciasRepositorioProvider);
    final comentario = _comentario.text.trim().isEmpty ? null : _comentario.text.trim();
    try {
      if (widget.coordinador) {
        await repo.registrar(profesionalId: _profesional!, tipo: _tipo, desde: _desde, hasta: _hasta, comentario: comentario, aprobar: _aprobar);
      } else {
        await repo.solicitar(tipo: _tipo, desde: _desde, hasta: _hasta, comentario: comentario);
      }
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
    final profesionales = widget.coordinador ? ref.watch(profesionalesProvider(const FiltroProfesionales())).value ?? const <ProfesionalDto>[] : const <ProfesionalDto>[];
    return AlertDialog(
      title: Text(widget.coordinador ? t.registrarAusencia : t.solicitarAusencia),
      content: SizedBox(
        width: 440,
        child: SingleChildScrollView(
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            if (widget.coordinador)
              DropdownButtonFormField<String>(
                initialValue: _profesional,
                isExpanded: true,
                decoration: InputDecoration(labelText: t.campoProfesional),
                items: [for (final p in profesionales) DropdownMenuItem(value: p.id, child: Text(p.nombreCompleto, overflow: TextOverflow.ellipsis))],
                onChanged: (v) => setState(() => _profesional = v),
              ),
            DropdownButtonFormField<TipoAusencia>(
              initialValue: _tipo,
              decoration: InputDecoration(labelText: t.campoTipoAusencia),
              items: [for (final x in TipoAusencia.values) DropdownMenuItem(value: x, child: Text(etiquetaTipoAusencia(t, x)))],
              onChanged: (v) => setState(() => _tipo = v ?? _tipo),
            ),
            const SizedBox(height: 12),
            Row(children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: () async {
                    final f = await elegirFecha(context, _desde);
                    if (f != null) {
                      setState(() {
                        _desde = f;
                        if (_hasta.isBefore(f)) _hasta = f;
                      });
                    }
                  },
                  child: Text('${t.campoDesde}: ${formatearFecha(fechaParaApi(_desde))}'),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: OutlinedButton(
                  onPressed: () async {
                    final f = await elegirFecha(context, _hasta);
                    if (f != null) setState(() => _hasta = f);
                  },
                  child: Text('${t.campoHasta}: ${formatearFecha(fechaParaApi(_hasta))}'),
                ),
              ),
            ]),
            TextField(
              controller: _comentario,
              maxLength: 300,
              maxLines: 2,
              decoration: InputDecoration(labelText: t.campoComentarioOpcional, helperText: t.avisoSinDatosSalud, helperMaxLines: 2),
            ),
            if (widget.coordinador) SwitchListTile(contentPadding: EdgeInsets.zero, title: Text(t.aprobarAlRegistrar), value: _aprobar, onChanged: (v) => setState(() => _aprobar = v)),
            if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
          ]),
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _enviar, child: Text(widget.coordinador ? t.registrar : t.solicitar)),
      ],
    );
  }
}

/// Declaración de disponibilidad del propio profesional.
class DialogoDisponibilidad extends ConsumerStatefulWidget {
  const DialogoDisponibilidad({super.key});

  @override
  ConsumerState<DialogoDisponibilidad> createState() => _EstadoDialogoDisponibilidad();
}

class _EstadoDialogoDisponibilidad extends ConsumerState<DialogoDisponibilidad> {
  final _comentario = TextEditingController();
  TipoDisponibilidad _tipo = TipoDisponibilidad.bloqueo;
  DateTime _desde = DateTime.now().add(const Duration(days: 1));
  late DateTime _hasta = _desde;
  int _dias = diasTodos;
  String? _tipoTurno;
  bool _enviando = false;
  String? _error;

  @override
  void dispose() {
    _comentario.dispose();
    super.dispose();
  }

  Future<void> _enviar() async {
    final t = AppLocalizations.of(context);
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(ausenciasRepositorioProvider).declarar(
            tipo: _tipo,
            desde: _desde,
            hasta: _hasta,
            dias: _dias,
            tipoTurnoId: _tipoTurno,
            comentario: _comentario.text.trim().isEmpty ? null : _comentario.text.trim(),
          );
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
    final tipos = ref.watch(misTiposTurnoProvider).value ?? const <TipoTurnoPropioDto>[];
    const iniciales = ['L', 'M', 'X', 'J', 'V', 'S', 'D'];
    return AlertDialog(
      title: Text(t.declararDisponibilidad),
      content: SizedBox(
        width: 460,
        child: SingleChildScrollView(
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            DropdownButtonFormField<TipoDisponibilidad>(
              initialValue: _tipo,
              decoration: InputDecoration(labelText: t.campoTipoDisponibilidad),
              items: [for (final x in TipoDisponibilidad.values) DropdownMenuItem(value: x, child: Text(etiquetaTipoDisponibilidad(t, x)))],
              onChanged: (v) => setState(() => _tipo = v ?? _tipo),
            ),
            const SizedBox(height: 12),
            Row(children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: () async {
                    final f = await elegirFecha(context, _desde);
                    if (f != null) {
                      setState(() {
                        _desde = f;
                        if (_hasta.isBefore(f)) _hasta = f;
                      });
                    }
                  },
                  child: Text('${t.campoDesde}: ${formatearFecha(fechaParaApi(_desde))}'),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: OutlinedButton(
                  onPressed: () async {
                    final f = await elegirFecha(context, _hasta);
                    if (f != null) setState(() => _hasta = f);
                  },
                  child: Text('${t.campoHasta}: ${formatearFecha(fechaParaApi(_hasta))}'),
                ),
              ),
            ]),
            Padding(padding: const EdgeInsets.only(top: 12, bottom: 4), child: Text(t.campoDiasSemana, style: Theme.of(context).textTheme.labelLarge)),
            Wrap(spacing: 6, children: [
              for (var i = 0; i < 7; i++)
                FilterChip(
                  label: Text(iniciales[i]),
                  selected: _dias & (1 << i) != 0,
                  onSelected: (v) => setState(() {
                    final nuevo = v ? _dias | (1 << i) : _dias & ~(1 << i);
                    if (nuevo != 0) _dias = nuevo; // siempre al menos un día
                  }),
                ),
            ]),
            if (tipos.isNotEmpty)
              DropdownButtonFormField<String?>(
                initialValue: _tipoTurno,
                isExpanded: true,
                decoration: InputDecoration(labelText: t.campoTipoTurnoOpcional),
                items: [
                  DropdownMenuItem(value: null, child: Text(t.cualquierTurno)),
                  for (final x in tipos) DropdownMenuItem(value: x.id, child: Text('${x.servicio} · ${x.nombre} (${x.franja})', overflow: TextOverflow.ellipsis)),
                ],
                onChanged: (v) => setState(() => _tipoTurno = v),
              ),
            TextField(controller: _comentario, maxLength: 300, maxLines: 2, decoration: InputDecoration(labelText: t.campoComentarioOpcional)),
            Text(t.avisoDisponibilidad, style: Theme.of(context).textTheme.bodySmall),
            if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
          ]),
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _enviar, child: Text(t.declarar)),
      ],
    );
  }
}
