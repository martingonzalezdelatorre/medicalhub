import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../catalogos/data/catalogos_repositorio.dart';
import '../../usuarios/data/usuarios_repositorio.dart';
import '../data/estructura_repositorio.dart';

class OpcionesServicio {
  const OpcionesServicio(this.categorias, this.competencias, this.coordinadores);
  final List<CatalogoDto> categorias;
  final List<CatalogoDto> competencias;
  final List<UsuarioDto> coordinadores;
}

final opcionesServicioProvider = FutureProvider.autoDispose<OpcionesServicio>((ref) async {
  final categorias = await ref.watch(catalogoActivoProvider(TipoCatalogo.categorias).future);
  final competencias = await ref.watch(catalogoActivoProvider(TipoCatalogo.competencias).future);
  final usuarios = await ref.watch(usuariosProvider.future);
  return OpcionesServicio(categorias, competencias,
      usuarios.where((u) => u.activo && u.roles.contains(Rol.coordinadorMedico)).toList());
});

class PaginaServicio extends ConsumerWidget {
  const PaginaServicio({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final servicio = ref.watch(servicioProvider(id));
    final admin = ref.watch(authProvider).perfil?.tieneRol(Rol.administradorSociedad) ?? false;
    final gestionaTurnos = admin || (ref.watch(authProvider).perfil?.tieneRol(Rol.recursosHumanos) ?? false);
    final categorias = {for (final c in ref.watch(catalogoCompletoProvider(TipoCatalogo.categorias)).value ?? <CatalogoDto>[]) c.id: c.nombre};
    final competencias = {for (final c in ref.watch(catalogoCompletoProvider(TipoCatalogo.competencias)).value ?? <CatalogoDto>[]) c.id: c.nombre};

    void refrescar() {
      ref.invalidate(servicioProvider(id));
      final centro = servicio.value?.centroId;
      if (centro != null) ref.invalidate(serviciosDeCentroProvider(centro));
    }

    return EstadoAsincrono(
      valor: servicio,
      alReintentar: () => ref.invalidate(servicioProvider(id)),
      constructor: (s) => ContenidoCentrado(
        child: ListView(padding: const EdgeInsets.only(bottom: 32), children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [
                    Expanded(child: Text(s.nombre, style: Theme.of(context).textTheme.titleLarge)),
                    if (!s.activo) Chip(label: Text(t.estadoInactivo)),
                    if (admin)
                      IconButton(
                        tooltip: t.editar,
                        icon: const Icon(Icons.edit_outlined),
                        onPressed: () async {
                          if (await showDialog<bool>(context: context, builder: (_) => DialogoServicio(servicio: s)) == true) refrescar();
                        },
                      ),
                  ]),
                  Text(s.centroNombre, style: Theme.of(context).textTheme.bodySmall),
                  if (s.descripcion != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(s.descripcion!)),
                  if (s.categoriasAdmitidasIds.isNotEmpty) ...[
                    const SizedBox(height: 12),
                    Text(t.campoCategoriasAdmitidas, style: Theme.of(context).textTheme.labelLarge),
                    Wrap(spacing: 6, children: [for (final c in s.categoriasAdmitidasIds) Chip(label: Text(categorias[c] ?? c))]),
                  ],
                  if (s.competenciasRequeridasIds.isNotEmpty) ...[
                    const SizedBox(height: 12),
                    Text(t.campoCompetenciasRequeridas, style: Theme.of(context).textTheme.labelLarge),
                    Wrap(spacing: 6, children: [for (final c in s.competenciasRequeridasIds) Chip(label: Text(competencias[c] ?? c))]),
                  ],
                ]),
              ),
            ),
          ),
          CabeceraSeccion(
            titulo: t.tiposTurnoTitulo,
            accion: gestionaTurnos
                ? TextButton.icon(
                    icon: const Icon(Icons.add),
                    label: Text(t.nuevoTipoTurno),
                    onPressed: () async {
                      if (await showDialog<bool>(context: context, builder: (_) => _DialogoTipoTurno(servicioId: s.id)) == true) refrescar();
                    },
                  )
                : null,
          ),
          if (s.tiposTurno.isEmpty) Padding(padding: const EdgeInsets.all(16), child: Text(t.listaVacia)),
          for (final tipo in s.tiposTurno)
            ListTile(
              leading: Icon(tipo.esGuardia ? Icons.nightlight_outlined : Icons.schedule),
              title: Text(tipo.nombre, style: tipo.activo ? null : const TextStyle(decoration: TextDecoration.lineThrough)),
              subtitle: Text([
                tipo.franja,
                if (tipo.cruzaMedianoche) t.cruzaMedianoche,
                t.duracionHoras(tipo.duracionHoras.toStringAsFixed(tipo.duracionHoras % 1 == 0 ? 0 : 1)),
              ].join(' · ')),
              trailing: gestionaTurnos
                  ? PopupMenuButton<String>(
                      onSelected: (accion) async {
                        if (accion == 'editar') {
                          if (await showDialog<bool>(context: context, builder: (_) => _DialogoTipoTurno(servicioId: s.id, existente: tipo)) == true) {
                            refrescar();
                          }
                        } else {
                          try {
                            await ref.read(estructuraRepositorioProvider).estadoTipoTurno(s.id, tipo.id, activo: !tipo.activo);
                            refrescar();
                          } catch (e) {
                            if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
                          }
                        }
                      },
                      itemBuilder: (_) => [
                        PopupMenuItem(value: 'editar', child: Text(t.editar)),
                        PopupMenuItem(value: 'estado', child: Text(tipo.activo ? t.accionDesactivar : t.accionActivar)),
                      ],
                    )
                  : null,
            ),
          CabeceraSeccion(
            titulo: t.requisitosTitulo,
            accion: admin
                ? TextButton.icon(
                    icon: const Icon(Icons.edit_outlined),
                    label: Text(t.editarRequisitos),
                    onPressed: () async {
                      if (await showDialog<bool>(context: context, builder: (_) => _DialogoRequisitos(servicio: s, categorias: categorias)) == true) {
                        refrescar();
                      }
                    },
                  )
                : null,
          ),
          if (s.requisitos.isEmpty) Padding(padding: const EdgeInsets.all(16), child: Text(t.listaVacia)),
          for (final r in s.requisitos)
            ListTile(
              leading: const Icon(Icons.groups_outlined),
              title: Text(
                  '${s.tiposTurno.where((x) => x.id == r.tipoTurnoId).map((x) => x.nombre).firstOrNull ?? '—'}: ${r.numeroProfesionales}'),
              subtitle: Text(r.categoriaProfesionalId == null ? t.cualquierCategoria : (categorias[r.categoriaProfesionalId] ?? '')),
            ),
          if (admin && s.configuracionReglas != null) ...[
            CabeceraSeccion(titulo: t.campoReglas),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: SelectableText(s.configuracionReglas!, style: const TextStyle(fontFamily: 'monospace')),
            ),
          ],
        ]),
      ),
    );
  }
}

class DialogoServicio extends ConsumerStatefulWidget {
  const DialogoServicio({super.key, this.centroId, this.servicio}) : assert(centroId != null || servicio != null);
  final String? centroId;
  final ServicioDto? servicio;

  @override
  ConsumerState<DialogoServicio> createState() => _EstadoDialogoServicio();
}

class _EstadoDialogoServicio extends ConsumerState<DialogoServicio> {
  final _formulario = GlobalKey<FormState>();
  late final _nombre = TextEditingController(text: widget.servicio?.nombre);
  late final _descripcion = TextEditingController(text: widget.servicio?.descripcion);
  late final _reglas = TextEditingController(text: widget.servicio?.configuracionReglas);
  late Set<String> _categorias = {...?widget.servicio?.categoriasAdmitidasIds};
  late Set<String> _competencias = {...?widget.servicio?.competenciasRequeridasIds};
  late String? _coordinador = widget.servicio?.coordinadorUsuarioId;
  bool _enviando = false;
  FalloApi? _fallo;
  String? _error;

  @override
  void dispose() {
    _nombre.dispose();
    _descripcion.dispose();
    _reglas.dispose();
    super.dispose();
  }

  Future<void> _guardar() async {
    if (!_formulario.currentState!.validate()) return;
    final t = AppLocalizations.of(context);
    setState(() {
      _enviando = true;
      _error = null;
      _fallo = null;
    });
    try {
      await ref.read(estructuraRepositorioProvider).guardarServicio(widget.servicio?.id, {
        'centroId': widget.servicio?.centroId ?? widget.centroId,
        'nombre': _nombre.text.trim(),
        'descripcion': _descripcion.text.trim().isEmpty ? null : _descripcion.text.trim(),
        'coordinadorUsuarioId': _coordinador,
        'configuracionReglas': _reglas.text.trim().isEmpty ? null : _reglas.text.trim(),
        'categoriasAdmitidasIds': _categorias.toList(),
        'competenciasRequeridasIds': _competencias.toList(),
      });
      if (mounted) Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (!mounted) return;
      setState(() {
        _fallo = f;
        _error = f.errores.isEmpty ? mensajeDeFallo(t, f) : f.errores.values.expand((e) => e).join(' ');
      });
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  String? _validarJson(String? v) {
    if (v == null || v.trim().isEmpty) return null;
    try {
      jsonDecode(v);
      return null;
    } on FormatException {
      return AppLocalizations.of(context).validaJson;
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final opciones = ref.watch(opcionesServicioProvider);
    return AlertDialog(
      title: Text(widget.servicio == null ? t.nuevoServicio : t.editarServicio),
      content: SizedBox(
        width: 480,
        child: EstadoAsincrono(
          valor: opciones,
          alReintentar: () => ref.invalidate(opcionesServicioProvider),
          constructor: (o) => SingleChildScrollView(
            child: Form(
              key: _formulario,
              child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
                TextFormField(
                    controller: _nombre,
                    decoration: InputDecoration(labelText: t.campoNombre),
                    validator: (v) => Validadores(t).obligatorio(v) ?? _fallo?.errorDe('Nombre')),
                TextFormField(controller: _descripcion, decoration: InputDecoration(labelText: t.campoDescripcion), maxLines: 2),
                DropdownButtonFormField<String?>(
                  initialValue: o.coordinadores.any((u) => u.id == _coordinador) ? _coordinador : null,
                  decoration: InputDecoration(labelText: t.campoCoordinador),
                  items: [
                    DropdownMenuItem(value: null, child: Text(t.sinCoordinador)),
                    for (final u in o.coordinadores) DropdownMenuItem(value: u.id, child: Text(u.nombreCompleto)),
                  ],
                  onChanged: (v) => setState(() => _coordinador = v),
                ),
                ChipsSeleccion(
                  etiqueta: t.campoCategoriasAdmitidas,
                  opciones: [for (final c in o.categorias) OpcionSeleccion(c.id, c.nombre)],
                  seleccion: _categorias,
                  alCambiar: (s) => setState(() => _categorias = s),
                ),
                ChipsSeleccion(
                  etiqueta: t.campoCompetenciasRequeridas,
                  opciones: [for (final c in o.competencias) OpcionSeleccion(c.id, c.nombre)],
                  seleccion: _competencias,
                  alCambiar: (s) => setState(() => _competencias = s),
                ),
                TextFormField(
                  controller: _reglas,
                  decoration: InputDecoration(labelText: t.campoReglas),
                  maxLines: 3,
                  style: const TextStyle(fontFamily: 'monospace'),
                  validator: (v) => _validarJson(v) ?? _fallo?.errorDe('ConfiguracionReglas'),
                ),
                if (_error != null)
                  Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
              ]),
            ),
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _guardar, child: Text(t.botonGuardar)),
      ],
    );
  }
}

class _DialogoTipoTurno extends ConsumerStatefulWidget {
  const _DialogoTipoTurno({required this.servicioId, this.existente});
  final String servicioId;
  final TipoTurnoDto? existente;

  @override
  ConsumerState<_DialogoTipoTurno> createState() => _EstadoDialogoTipoTurno();
}

class _EstadoDialogoTipoTurno extends ConsumerState<_DialogoTipoTurno> {
  final _formulario = GlobalKey<FormState>();
  late final _nombre = TextEditingController(text: widget.existente?.nombre);
  late TimeOfDay _inicio = widget.existente == null ? const TimeOfDay(hour: 8, minute: 0) : desdeHoraApi(widget.existente!.horaInicio);
  late TimeOfDay _fin = widget.existente == null ? const TimeOfDay(hour: 15, minute: 0) : desdeHoraApi(widget.existente!.horaFin);
  late bool _guardia = widget.existente?.esGuardia ?? false;
  bool _enviando = false;
  String? _error;

  @override
  void dispose() {
    _nombre.dispose();
    super.dispose();
  }

  bool get _cruza => (_fin.hour * 60 + _fin.minute) <= (_inicio.hour * 60 + _inicio.minute);

  Future<void> _guardar() async {
    if (!_formulario.currentState!.validate()) return;
    final t = AppLocalizations.of(context);
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(estructuraRepositorioProvider).guardarTipoTurno(widget.servicioId, widget.existente?.id, {
        'nombre': _nombre.text.trim(),
        'horaInicio': aHoraApi(_inicio),
        'horaFin': aHoraApi(_fin),
        'esGuardia': _guardia,
      });
      if (mounted) Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (mounted) setState(() => _error = mensajeDeFallo(t, f));
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return AlertDialog(
      title: Text(widget.existente == null ? t.nuevoTipoTurno : t.editarTipoTurno),
      content: SizedBox(
        width: 400,
        child: Form(
          key: _formulario,
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            TextFormField(controller: _nombre, decoration: InputDecoration(labelText: t.campoNombre), validator: Validadores(t).obligatorio),
            const SizedBox(height: 12),
            Row(children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: () async => _inicio = await elegirHora(context, _inicio) ?? _inicio,
                  child: Text('${t.campoHoraInicio}: ${aHoraApi(_inicio)}'),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: OutlinedButton(
                  onPressed: () async => _fin = await elegirHora(context, _fin) ?? _fin,
                  child: Text('${t.campoHoraFin}: ${aHoraApi(_fin)}'),
                ),
              ),
            ]),
            SwitchListTile(contentPadding: EdgeInsets.zero, title: Text(t.campoGuardia), value: _guardia, onChanged: (v) => setState(() => _guardia = v)),
            if (_cruza) Align(alignment: Alignment.centerLeft, child: Text(t.cruzaMedianoche, style: Theme.of(context).textTheme.bodySmall)),
            if (_error != null)
              Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
          ]),
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _guardar, child: Text(t.botonGuardar)),
      ],
    );
  }
}

class _LineaReq {
  _LineaReq(this.tipoTurnoId, this.numero, this.categoriaId);
  String? tipoTurnoId;
  int numero;
  String? categoriaId;
}

class _DialogoRequisitos extends ConsumerStatefulWidget {
  const _DialogoRequisitos({required this.servicio, required this.categorias});
  final ServicioDto servicio;
  final Map<String, String> categorias;

  @override
  ConsumerState<_DialogoRequisitos> createState() => _EstadoDialogoRequisitos();
}

class _EstadoDialogoRequisitos extends ConsumerState<_DialogoRequisitos> {
  late final List<_LineaReq> _lineas = [
    for (final r in widget.servicio.requisitos) _LineaReq(r.tipoTurnoId, r.numeroProfesionales, r.categoriaProfesionalId),
  ];
  bool _enviando = false;
  String? _error;

  Future<void> _guardar() async {
    final t = AppLocalizations.of(context);
    if (_lineas.any((l) => l.tipoTurnoId == null || l.numero < 1 || l.numero > 50)) {
      setState(() => _error = t.validaNumero);
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(estructuraRepositorioProvider).guardarRequisitos(widget.servicio.id, [
        for (final l in _lineas)
          {'tipoTurnoId': l.tipoTurnoId, 'numeroProfesionales': l.numero, 'categoriaProfesionalId': l.categoriaId},
      ]);
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
    final tipos = widget.servicio.tiposTurno.where((x) => x.activo).toList();
    return AlertDialog(
      title: Text(t.requisitosTitulo),
      content: SizedBox(
        width: 520,
        child: SingleChildScrollView(
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            for (var i = 0; i < _lineas.length; i++)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Wrap(spacing: 8, runSpacing: 8, crossAxisAlignment: WrapCrossAlignment.center, children: [
                  SizedBox(
                    width: 180,
                    child: DropdownButtonFormField<String>(
                      initialValue: tipos.any((x) => x.id == _lineas[i].tipoTurnoId) ? _lineas[i].tipoTurnoId : null,
                      decoration: InputDecoration(labelText: t.campoTipoTurno, isDense: true),
                      isExpanded: true,
                      items: [for (final x in tipos) DropdownMenuItem(value: x.id, child: Text(x.nombre, overflow: TextOverflow.ellipsis))],
                      onChanged: (v) => setState(() => _lineas[i].tipoTurnoId = v),
                    ),
                  ),
                  SizedBox(
                    width: 90,
                    child: TextFormField(
                      initialValue: '${_lineas[i].numero}',
                      decoration: InputDecoration(labelText: t.campoNumeroProfesionales, isDense: true),
                      keyboardType: TextInputType.number,
                      onChanged: (v) => _lineas[i].numero = int.tryParse(v) ?? 0,
                    ),
                  ),
                  SizedBox(
                    width: 180,
                    child: DropdownButtonFormField<String?>(
                      initialValue: widget.categorias.containsKey(_lineas[i].categoriaId) ? _lineas[i].categoriaId : null,
                      decoration: InputDecoration(labelText: t.campoCategoria, isDense: true),
                      isExpanded: true,
                      items: [
                        DropdownMenuItem(value: null, child: Text(t.cualquierCategoria, overflow: TextOverflow.ellipsis)),
                        for (final e in widget.categorias.entries) DropdownMenuItem(value: e.key, child: Text(e.value, overflow: TextOverflow.ellipsis)),
                      ],
                      onChanged: (v) => setState(() => _lineas[i].categoriaId = v),
                    ),
                  ),
                  IconButton(tooltip: t.quitar, icon: const Icon(Icons.delete_outline), onPressed: () => setState(() => _lineas.removeAt(i))),
                ]),
              ),
            TextButton.icon(
              icon: const Icon(Icons.add),
              label: Text(t.anadir),
              onPressed: tipos.isEmpty ? null : () => setState(() => _lineas.add(_LineaReq(tipos.first.id, 1, null))),
            ),
            if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
          ]),
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _guardar, child: Text(t.botonGuardar)),
      ],
    );
  }
}
