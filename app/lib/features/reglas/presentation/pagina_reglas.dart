import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../estructura/data/estructura_repositorio.dart';
import '../data/reglas_repositorio.dart';

/// Reglas de planificación de la sociedad y registro de excepciones autorizadas.
class PaginaReglas extends StatelessWidget {
  const PaginaReglas({super.key});

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return DefaultTabController(
      length: 2,
      child: Column(children: [
        TabBar(tabs: [Tab(text: t.reglasTabReglas), Tab(text: t.reglasTabExcepciones)]),
        const Expanded(child: TabBarView(children: [_ListaReglas(), _ListaExcepciones()])),
      ]),
    );
  }
}

class _ListaReglas extends ConsumerWidget {
  const _ListaReglas();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final reglas = ref.watch(reglasProvider);
    final tipos = ref.watch(tiposReglaProvider).value ?? const <TipoReglaDto>[];
    final servicios = {for (final s in ref.watch(serviciosActivosProvider).value ?? const <ServicioDto>[]) s.id: s.nombre};
    final admin = ref.watch(authProvider).perfil?.tieneRol(Rol.administradorSociedad) ?? false;
    final repo = ref.read(reglasRepositorioProvider);

    Future<void> ejecutar(Future<void> Function() accion) async {
      try {
        await accion();
        ref.invalidate(reglasProvider);
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
    }

    Future<void> editar([ReglaDto? regla]) async {
      if (await showDialog<bool>(context: context, builder: (_) => DialogoRegla(existente: regla)) == true) ref.invalidate(reglasProvider);
    }

    String resumen(ReglaDto r) {
      final def = tipos.where((x) => x.tipo == r.tipo).firstOrNull;
      final p = r.parametros;
      if (def == null || p == null) return '';
      return def.parametros.map((d) {
        final v = p[d.clave];
        return '${d.etiqueta}: ${d.tipo == 'servicio' ? (servicios['$v'] ?? '—') : v}';
      }).join(' · ');
    }

    return Scaffold(
      floatingActionButton: admin ? FloatingActionButton.extended(heroTag: 'fab-regla', icon: const Icon(Icons.add), label: Text(t.nuevaRegla), onPressed: () => editar()) : null,
      body: EstadoAsincrono(
        valor: reglas,
        alReintentar: () => ref.invalidate(reglasProvider),
        constructor: (lista) => ContenidoCentrado(
          child: ListView(padding: const EdgeInsets.only(bottom: 88, top: 8), children: [
            Card(
              margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
              child: Padding(padding: const EdgeInsets.all(12), child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                const Icon(Icons.info_outline),
                const SizedBox(width: 8),
                Expanded(child: Text(t.reglasAviso)),
              ])),
            ),
            if (lista.isEmpty) Padding(padding: const EdgeInsets.all(24), child: Center(child: Text(t.reglasVacio))),
            for (final r in lista)
              Card(
                margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                child: ListTile(
                  isThreeLine: true,
                  leading: Icon(r.bloqueante ? Icons.block : Icons.warning_amber_outlined, color: r.bloqueante ? Theme.of(context).colorScheme.error : Colors.orange.shade800),
                  title: Text(r.nombre, style: r.activa ? null : const TextStyle(decoration: TextDecoration.lineThrough)),
                  subtitle: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text('${tipos.where((x) => x.tipo == r.tipo).map((x) => x.nombre).firstOrNull ?? r.tipo} · ${r.servicioNombre ?? t.ambitoSociedad}'),
                    if (resumen(r).isNotEmpty) Text(resumen(r)),
                    Text([
                      r.bloqueante ? t.severidadBloqueante : t.severidadAdvertencia,
                      if (r.permiteExcepcion) r.excepcionRequiereAdministrador ? t.excepcionSoloAdmin : t.excepcionPosible,
                    ].join(' · '), style: Theme.of(context).textTheme.bodySmall),
                    if (r.fundamento != null) Text(r.fundamento!, style: Theme.of(context).textTheme.bodySmall, maxLines: 2, overflow: TextOverflow.ellipsis),
                    Wrap(spacing: 6, children: [
                      if (r.pendienteDeRevision) Chip(visualDensity: VisualDensity.compact, avatar: const Icon(Icons.rate_review_outlined, size: 16), label: Text(t.pendienteRevision)),
                      if (!r.pendienteDeRevision && r.revisadaEl != null)
                        Chip(visualDensity: VisualDensity.compact, avatar: const Icon(Icons.verified_outlined, size: 16), label: Text(t.revisadaPor(r.revisadaPor ?? '—', _fecha(r.revisadaEl!)))),
                      if (!r.activa) Chip(visualDensity: VisualDensity.compact, label: Text(t.estadoInactivo)),
                    ]),
                  ]),
                  trailing: admin
                      ? PopupMenuButton<String>(
                          onSelected: (a) => switch (a) {
                            'editar' => editar(r),
                            'estado' => ejecutar(() => repo.cambiarEstado(r.id, activa: !r.activa)),
                            _ => ejecutar(() => repo.revisar(r.id)),
                          },
                          itemBuilder: (_) => [
                            PopupMenuItem(value: 'editar', child: Text(t.editar)),
                            PopupMenuItem(value: 'revisar', child: Text(t.marcarRevisada)),
                            PopupMenuItem(value: 'estado', child: Text(r.activa ? t.accionDesactivar : t.accionActivar)),
                          ],
                        )
                      : null,
                ),
              ),
            const SizedBox(height: 8),
            _ComportamientoPorDefecto(tipos: tipos, reglas: lista),
          ]),
        ),
      ),
    );
  }
}

String _fecha(DateTime f) {
  final l = f.toLocal();
  String d2(int n) => n.toString().padLeft(2, '0');
  return '${d2(l.day)}/${d2(l.month)}/${l.year}';
}

/// Tipos sin ninguna regla configurada y qué ocurre en ese caso (para que no haya sorpresas).
class _ComportamientoPorDefecto extends StatelessWidget {
  const _ComportamientoPorDefecto({required this.tipos, required this.reglas});
  final List<TipoReglaDto> tipos;
  final List<ReglaDto> reglas;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final sinConfigurar = tipos.where((x) => reglas.every((r) => r.tipo != x.tipo)).toList();
    if (sinConfigurar.isEmpty) return const SizedBox.shrink();
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      child: ExpansionTile(
        title: Text(t.comportamientoPorDefecto),
        children: [
          for (final x in sinConfigurar) ListTile(dense: true, title: Text(x.nombre), subtitle: Text('${x.descripcion}\n${x.comportamientoPorDefecto}'), isThreeLine: true),
        ],
      ),
    );
  }
}

class _ListaExcepciones extends ConsumerWidget {
  const _ListaExcepciones();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    return EstadoAsincrono(
      valor: ref.watch(excepcionesProvider),
      alReintentar: () => ref.invalidate(excepcionesProvider),
      constructor: (lista) => lista.isEmpty
          ? EstadoVacio(mensaje: t.sinExcepciones)
          : ContenidoCentrado(
              child: ListView.builder(
                padding: const EdgeInsets.only(top: 8, bottom: 24),
                itemCount: lista.length,
                itemBuilder: (_, i) {
                  final e = lista[i];
                  return Card(
                    margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Row(children: [
                          Expanded(child: Text(e.profesional, style: Theme.of(context).textTheme.titleSmall)),
                          if (!e.asignacionActiva) Chip(visualDensity: VisualDensity.compact, label: Text(t.excepcionAnulada)),
                        ]),
                        Text(e.turno, style: Theme.of(context).textTheme.bodySmall),
                        const SizedBox(height: 4),
                        Text('${e.regla}: ${e.mensaje}'),
                        const SizedBox(height: 4),
                        Text('${t.justificacion}: ${e.justificacion}'),
                        Text('${t.autorizadaPor(e.autorizadaPor)} · ${_fecha(e.fecha)}', style: Theme.of(context).textTheme.bodySmall),
                      ]),
                    ),
                  );
                },
              ),
            ),
    );
  }
}

class DialogoRegla extends ConsumerStatefulWidget {
  const DialogoRegla({super.key, this.existente});
  final ReglaDto? existente;

  @override
  ConsumerState<DialogoRegla> createState() => _EstadoDialogoRegla();
}

class _EstadoDialogoRegla extends ConsumerState<DialogoRegla> {
  final _formulario = GlobalKey<FormState>();
  late final _nombre = TextEditingController(text: widget.existente?.nombre);
  late final _descripcion = TextEditingController(text: widget.existente?.descripcion);
  late final _fundamento = TextEditingController(text: widget.existente?.fundamento);
  final _valores = <String, TextEditingController>{};
  final _servicios = <String, String?>{};
  late String? _tipo = widget.existente?.tipo;
  late String? _ambito = widget.existente?.servicioId;
  late bool _bloqueante = widget.existente?.bloqueante ?? true;
  late bool _permite = widget.existente?.permiteExcepcion ?? true;
  late bool _admin = widget.existente?.excepcionRequiereAdministrador ?? false;
  bool _enviando = false;
  FalloApi? _fallo;
  String? _error;

  @override
  void dispose() {
    for (final c in [_nombre, _descripcion, _fundamento, ..._valores.values]) {
      c.dispose();
    }
    super.dispose();
  }

  TextEditingController _controlador(String clave) =>
      _valores.putIfAbsent(clave, () => TextEditingController(text: widget.existente?.parametros?[clave]?.toString()));

  Future<void> _guardar(List<TipoReglaDto> tipos) async {
    final t = AppLocalizations.of(context);
    if (_tipo == null) {
      setState(() => _error = '${t.campoTipoRegla}: ${t.validaObligatorio}');
      return;
    }
    if (!_formulario.currentState!.validate()) return;
    final def = tipos.firstWhere((x) => x.tipo == _tipo);
    final parametros = <String, dynamic>{
      for (final p in def.parametros) p.clave: p.tipo == 'entero' ? int.tryParse(_controlador(p.clave).text.trim()) : (_servicios[p.clave] ?? widget.existente?.parametros?[p.clave]),
    };
    setState(() {
      _enviando = true;
      _error = null;
      _fallo = null;
    });
    try {
      await ref.read(reglasRepositorioProvider).guardar(widget.existente?.id, {
        'tipo': _tipo,
        'nombre': _nombre.text.trim(),
        'descripcion': _descripcion.text.trim().isEmpty ? null : _descripcion.text.trim(),
        'servicioId': _ambito,
        'severidad': _bloqueante ? 'Bloqueante' : 'Advertencia',
        'permiteExcepcion': _bloqueante && _permite,
        'excepcionRequiereAdministrador': _bloqueante && _permite && _admin,
        'parametros': def.parametros.isEmpty ? null : parametros,
        'fundamento': _fundamento.text.trim().isEmpty ? null : _fundamento.text.trim(),
      });
      if (mounted) Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (!mounted) return;
      setState(() {
        _fallo = f;
        _error = f.errores.isEmpty ? mensajeDeFallo(t, f) : f.errores.values.expand((e) => e).join(' ');
      });
      _formulario.currentState!.validate();
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tipos = ref.watch(tiposReglaProvider);
    final servicios = ref.watch(serviciosActivosProvider).value ?? const <ServicioDto>[];
    final v = Validadores(t);

    return AlertDialog(
      title: Text(widget.existente == null ? t.nuevaRegla : t.editarRegla),
      content: SizedBox(
        width: 500,
        child: EstadoAsincrono(
          valor: tipos,
          alReintentar: () => ref.invalidate(tiposReglaProvider),
          constructor: (lista) {
            final def = lista.where((x) => x.tipo == _tipo).firstOrNull;
            return SingleChildScrollView(
              child: Form(
                key: _formulario,
                child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
                  DropdownButtonFormField<String>(
                    initialValue: _tipo,
                    isExpanded: true,
                    decoration: InputDecoration(labelText: t.campoTipoRegla),
                    items: [for (final x in lista) DropdownMenuItem(value: x.tipo, child: Text(x.nombre, overflow: TextOverflow.ellipsis))],
                    onChanged: widget.existente != null
                        ? null
                        : (x) => setState(() {
                              _tipo = x;
                              if (_nombre.text.isEmpty) _nombre.text = lista.firstWhere((d) => d.tipo == x).nombre;
                            }),
                  ),
                  if (def != null) Padding(padding: const EdgeInsets.only(top: 4), child: Text(def.descripcion, style: Theme.of(context).textTheme.bodySmall)),
                  TextFormField(controller: _nombre, decoration: InputDecoration(labelText: t.campoNombre), validator: (x) => v.obligatorio(x) ?? _fallo?.errorDe('Nombre')),
                  TextFormField(controller: _descripcion, decoration: InputDecoration(labelText: t.campoDescripcion)),
                  DropdownButtonFormField<String?>(
                    initialValue: servicios.any((s) => s.id == _ambito) ? _ambito : null,
                    isExpanded: true,
                    decoration: InputDecoration(labelText: t.campoAmbito),
                    items: [
                      DropdownMenuItem(value: null, child: Text(t.ambitoSociedad)),
                      for (final s in servicios) DropdownMenuItem(value: s.id, child: Text('${s.centroNombre} · ${s.nombre}', overflow: TextOverflow.ellipsis)),
                    ],
                    onChanged: (x) => setState(() => _ambito = x),
                  ),
                  if (def != null)
                    for (final p in def.parametros)
                      p.tipo == 'entero'
                          ? TextFormField(
                              controller: _controlador(p.clave),
                              keyboardType: TextInputType.number,
                              decoration: InputDecoration(labelText: '${p.etiqueta} (${p.minimo}–${p.maximo})'),
                              validator: (x) {
                                final n = int.tryParse((x ?? '').trim());
                                if (n == null || n < p.minimo! || n > p.maximo!) return t.validaRango(p.minimo!, p.maximo!);
                                return _fallo?.errorDe('Parametros.${p.clave}');
                              },
                            )
                          : DropdownButtonFormField<String>(
                              initialValue: servicios.any((s) => s.id == (_servicios[p.clave] ?? widget.existente?.parametros?[p.clave])) ? (_servicios[p.clave] ?? widget.existente?.parametros?[p.clave]) as String : null,
                              isExpanded: true,
                              decoration: InputDecoration(labelText: p.etiqueta),
                              items: [for (final s in servicios) DropdownMenuItem(value: s.id, child: Text('${s.centroNombre} · ${s.nombre}', overflow: TextOverflow.ellipsis))],
                              validator: (x) => x == null ? t.validaObligatorio : _fallo?.errorDe('Parametros.${p.clave}'),
                              onChanged: (x) => setState(() => _servicios[p.clave] = x),
                            ),
                  const SizedBox(height: 12),
                  SegmentedButton<bool>(
                    segments: [ButtonSegment(value: true, label: Text(t.severidadBloqueante)), ButtonSegment(value: false, label: Text(t.severidadAdvertencia))],
                    selected: {_bloqueante},
                    onSelectionChanged: (s) => setState(() => _bloqueante = s.first),
                  ),
                  if (_bloqueante) ...[
                    SwitchListTile(contentPadding: EdgeInsets.zero, title: Text(t.campoPermiteExcepcion), value: _permite, onChanged: (x) => setState(() => _permite = x)),
                    if (_permite)
                      SwitchListTile(contentPadding: EdgeInsets.zero, title: Text(t.campoExcepcionAdmin), value: _admin, onChanged: (x) => setState(() => _admin = x)),
                  ],
                  TextFormField(controller: _fundamento, decoration: InputDecoration(labelText: t.campoFundamento, helperText: t.fundamentoAyuda), maxLines: 2),
                  if (_error != null) Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
                ]),
              ),
            );
          },
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando || !tipos.hasValue ? null : () => _guardar(tipos.value!), child: Text(t.botonGuardar)),
      ],
    );
  }
}
