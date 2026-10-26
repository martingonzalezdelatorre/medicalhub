import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../core/tema/tema.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../planificacion/data/planificacion_repositorio.dart' show fechaParaApi;
import '../../profesionales/data/profesionales_repositorio.dart';
import '../data/ausencias_repositorio.dart';
import 'etiquetas_ausencias.dart';
import 'tarjeta_formulario.dart';

/// Solicitud de ausencia en pantalla completa. Con [coordinador] = true permite elegir el profesional
/// y registrarla ya aprobada; el profesional la solicita para sí mismo.
class PaginaSolicitarAusencia extends ConsumerStatefulWidget {
  const PaginaSolicitarAusencia({super.key, this.coordinador = false});
  final bool coordinador;

  @override
  ConsumerState<PaginaSolicitarAusencia> createState() => _EstadoPagina();
}

class _EstadoPagina extends ConsumerState<PaginaSolicitarAusencia> {
  static const _anchoDosColumnas = 860.0;

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

  String get _volverA => widget.coordinador ? '/ausencias' : '/mi-disponibilidad';

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
      ref.invalidate(misAusenciasProvider);
      ref.invalidate(ausenciasProvider);
      if (!mounted) return;
      mostrarMensaje(context, widget.coordinador ? t.avisoAusenciaRegistrada : t.avisoAusenciaSolicitada);
      context.go(_volverA);
    } on FalloApi catch (f) {
      if (mounted) setState(() => _error = f.errores.isEmpty ? mensajeDeFallo(t, f) : f.errores.values.expand((e) => e).join(' '));
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  Widget _formulario(AppLocalizations t, List<ProfesionalDto> profesionales) {
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      if (widget.coordinador) ...[
        TarjetaFormulario(
          icono: Icons.badge_outlined,
          titulo: t.campoProfesional,
          child: DropdownButtonFormField<String>(
            initialValue: _profesional,
            isExpanded: true,
            decoration: const InputDecoration(prefixIcon: Icon(Icons.person_outline)),
            items: [for (final p in profesionales) DropdownMenuItem(value: p.id, child: Text(p.nombreCompleto, overflow: TextOverflow.ellipsis))],
            onChanged: (v) => setState(() => _profesional = v),
          ),
        ),
        const SizedBox(height: 12),
      ],
      TarjetaFormulario(
        icono: Icons.beach_access_outlined,
        titulo: t.campoTipoAusencia,
        child: Wrap(spacing: 8, runSpacing: 8, children: [
          for (final x in TipoAusencia.values)
            ChoiceChip(label: Text(etiquetaTipoAusencia(t, x)), selected: _tipo == x, onSelected: (_) => setState(() => _tipo = x)),
        ]),
      ),
      const SizedBox(height: 12),
      TarjetaFormulario(
        icono: Icons.date_range_outlined,
        titulo: t.seccionPeriodo,
        child: SelectorPeriodo(desde: _desde, hasta: _hasta, alCambiar: (d, h) => setState(() {
              _desde = d;
              _hasta = h;
            })),
      ),
      const SizedBox(height: 12),
      TarjetaFormulario(
        icono: Icons.notes_outlined,
        titulo: t.campoComentarioOpcional,
        child: TextField(
          controller: _comentario,
          maxLength: 300,
          maxLines: 3,
          decoration: InputDecoration(alignLabelWithHint: true, helperText: t.avisoSinDatosSalud, helperMaxLines: 2),
        ),
      ),
      if (widget.coordinador) ...[
        const SizedBox(height: 12),
        Card(
          margin: EdgeInsets.zero,
          child: SwitchListTile(title: Text(t.aprobarAlRegistrar), value: _aprobar, onChanged: (v) => setState(() => _aprobar = v)),
        ),
      ],
    ]);
  }

  Widget _resumen(AppLocalizations t, List<ProfesionalDto> profesionales) {
    final tema = Theme.of(context);
    final cs = tema.colorScheme;
    final profesional = profesionales.where((p) => p.id == _profesional).firstOrNull;
    Widget fila(IconData icono, String texto, {bool negrita = false}) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Icon(icono, size: 18, color: cs.onSurfaceVariant),
            const SizedBox(width: 10),
            Expanded(child: Text(texto, style: negrita ? tema.textTheme.titleSmall : tema.textTheme.bodyMedium)),
          ]),
        );
    final dias = _hasta.difference(_desde).inDays + 1;
    return TarjetaFormulario(
      icono: Icons.fact_check_outlined,
      titulo: t.resumenTitulo,
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        if (widget.coordinador && profesional != null) fila(Icons.person_outline, profesional.nombreCompleto, negrita: true),
        fila(Icons.beach_access_outlined, etiquetaTipoAusencia(t, _tipo), negrita: !widget.coordinador || profesional == null),
        fila(Icons.date_range_outlined, rangoFechasApi(fechaParaApi(_desde), fechaParaApi(_hasta))),
        fila(Icons.timelapse_outlined, t.diasNumero(dias)),
        if (widget.coordinador && _aprobar) fila(Icons.verified_outlined, t.aprobarAlRegistrar),
      ]),
    );
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tema = Theme.of(context);
    final profesionales =
        widget.coordinador ? ref.watch(profesionalesProvider(const FiltroProfesionales())).value ?? const <ProfesionalDto>[] : const <ProfesionalDto>[];
    final ancho = MediaQuery.sizeOf(context).width;

    final contenido = ancho >= _anchoDosColumnas
        ? Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Expanded(flex: 3, child: _formulario(t, profesionales)),
            const SizedBox(width: 16),
            Expanded(flex: 2, child: _resumen(t, profesionales)),
          ])
        : Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [_formulario(t, profesionales), const SizedBox(height: 12), _resumen(t, profesionales)]);

    return Column(children: [
      Expanded(
        child: ContenidoCentrado(
          anchoMaximo: 960,
          child: ListView(padding: EdgeInsets.all(ancho < Pantalla.movil ? 12 : 20), children: [
            Text(widget.coordinador ? t.registrarAusencia : t.solicitarAusencia, style: tema.textTheme.headlineSmall),
            const SizedBox(height: 16),
            contenido,
            if (_error != null) Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: TextStyle(color: tema.colorScheme.error))),
          ]),
        ),
      ),
      Material(
        elevation: 6,
        color: tema.colorScheme.surface,
        child: SafeArea(
          top: false,
          child: ContenidoCentrado(
            anchoMaximo: 960,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              child: Row(mainAxisAlignment: MainAxisAlignment.end, children: [
                TextButton(onPressed: _enviando ? null : () => context.go(_volverA), child: Text(t.botonCancelar)),
                const SizedBox(width: 8),
                FilledButton.icon(
                  icon: const Icon(Icons.check),
                  onPressed: _enviando ? null : _enviar,
                  label: Text(widget.coordinador ? t.registrar : t.solicitar),
                ),
              ]),
            ),
          ),
        ),
      ),
    ]);
  }
}
