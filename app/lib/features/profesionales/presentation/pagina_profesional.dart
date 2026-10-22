import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../catalogos/data/catalogos_repositorio.dart';
import '../../estructura/data/estructura_repositorio.dart';
import '../../usuarios/data/usuarios_repositorio.dart';
import '../data/profesionales_repositorio.dart';

class OpcionesProfesional {
  const OpcionesProfesional(this.categorias, this.especialidades, this.competencias, this.servicios, this.usuarios);
  final List<CatalogoDto> categorias;
  final List<CatalogoDto> especialidades;
  final List<CatalogoDto> competencias;
  final List<ServicioDto> servicios;
  final List<UsuarioDto> usuarios;
}

final opcionesProfesionalProvider = FutureProvider.autoDispose<OpcionesProfesional>((ref) async {
  final categorias = await ref.watch(catalogoActivoProvider(TipoCatalogo.categorias).future);
  final especialidades = await ref.watch(catalogoActivoProvider(TipoCatalogo.especialidades).future);
  final competencias = await ref.watch(catalogoActivoProvider(TipoCatalogo.competencias).future);
  final servicios = await ref.watch(serviciosActivosProvider.future);
  final usuarios = await ref.watch(usuariosProvider.future);
  return OpcionesProfesional(categorias, especialidades, competencias, servicios,
      usuarios.where((u) => u.activo && u.roles.contains(Rol.profesionalSanitario)).toList());
});

/// Ficha de un profesional: formulario para el administrador y vista de solo lectura para los demás roles.
class PaginaProfesional extends ConsumerWidget {
  const PaginaProfesional({super.key, this.id});
  final String? id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final admin = ref.watch(authProvider).perfil?.tieneRol(Rol.administradorSociedad) ?? false;
    if (id == null) return const _Formulario();
    final ficha = ref.watch(profesionalProvider(id!));
    return EstadoAsincrono(
      valor: ficha,
      alReintentar: () => ref.invalidate(profesionalProvider(id!)),
      constructor: (p) => admin ? _Formulario(existente: p) : _VistaSoloLectura(profesional: p),
    );
  }
}

class _VistaSoloLectura extends ConsumerWidget {
  const _VistaSoloLectura({required this.profesional});
  final ProfesionalDto profesional;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    Map<String, String> nombres(TipoCatalogo tipo) =>
        {for (final c in ref.watch(catalogoCompletoProvider(tipo)).value ?? <CatalogoDto>[]) c.id: c.nombre};
    final categorias = nombres(TipoCatalogo.categorias);
    final especialidades = nombres(TipoCatalogo.especialidades);
    final competencias = nombres(TipoCatalogo.competencias);
    final p = profesional;

    return ContenidoCentrado(
      anchoMaximo: 600,
      child: ListView(padding: const EdgeInsets.all(16), children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(p.nombreCompleto, style: Theme.of(context).textTheme.titleLarge),
              Text([categorias[p.categoriaId] ?? '', if (p.numeroColegiado != null) p.numeroColegiado!].where((x) => x.isNotEmpty).join(' · ')),
              const Divider(height: 24),
              Text(t.datosContacto, style: Theme.of(context).textTheme.labelLarge),
              Text(p.email == null && p.telefono == null ? t.sinContacto : [?p.email, ?p.telefono].join(' · ')),
              const SizedBox(height: 12),
              Text(t.campoEspecialidades, style: Theme.of(context).textTheme.labelLarge),
              Wrap(spacing: 6, children: [for (final e in p.especialidadesIds) Chip(label: Text(especialidades[e] ?? e))]),
              const SizedBox(height: 12),
              Text(t.campoCompetencias, style: Theme.of(context).textTheme.labelLarge),
              Wrap(spacing: 6, children: [
                for (final c in p.competencias)
                  Chip(
                      label: Text(
                          '${competencias[c.competenciaId] ?? c.competenciaId}${c.vigenteHasta == null ? '' : ' · ${t.vigenteHasta(formatearFecha(c.vigenteHasta!))}'}')),
              ]),
            ]),
          ),
        ),
      ]),
    );
  }
}

class _Formulario extends ConsumerStatefulWidget {
  const _Formulario({this.existente});
  final ProfesionalDto? existente;

  @override
  ConsumerState<_Formulario> createState() => _EstadoFormulario();
}

class _EstadoFormulario extends ConsumerState<_Formulario> {
  final _clave = GlobalKey<FormState>();
  late final _nombre = TextEditingController(text: widget.existente?.nombre);
  late final _apellidos = TextEditingController(text: widget.existente?.apellidos);
  late final _email = TextEditingController(text: widget.existente?.email);
  late final _telefono = TextEditingController(text: widget.existente?.telefono);
  late final _colegiado = TextEditingController(text: widget.existente?.numeroColegiado);
  late String? _categoria = widget.existente?.categoriaId;
  late String? _usuario = widget.existente?.usuarioId;
  late Set<String> _especialidades = {...?widget.existente?.especialidadesIds};
  late Set<String> _servicios = {...?widget.existente?.serviciosIds};
  late final Map<String, String?> _competencias = {for (final c in widget.existente?.competencias ?? <CompetenciaProfesionalDto>[]) c.competenciaId: c.vigenteHasta};
  bool _enviando = false;
  FalloApi? _fallo;
  String? _error;

  @override
  void dispose() {
    for (final c in [_nombre, _apellidos, _email, _telefono, _colegiado]) {
      c.dispose();
    }
    super.dispose();
  }

  String? _texto(TextEditingController c) => c.text.trim().isEmpty ? null : c.text.trim();

  Future<void> _guardar() async {
    final t = AppLocalizations.of(context);
    if (!_clave.currentState!.validate() || _categoria == null) {
      setState(() => _error = _categoria == null ? '${t.campoCategoria}: ${t.validaObligatorio}' : null);
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
      _fallo = null;
    });
    try {
      await ref.read(profesionalesRepositorioProvider).guardar(widget.existente?.id, {
        'usuarioId': _usuario,
        'nombre': _nombre.text.trim(),
        'apellidos': _apellidos.text.trim(),
        'email': _texto(_email),
        'telefono': _texto(_telefono),
        'numeroColegiado': _texto(_colegiado),
        'categoriaId': _categoria,
        'especialidadesIds': _especialidades.toList(),
        'competencias': [for (final e in _competencias.entries) {'competenciaId': e.key, 'vigenteHasta': e.value}],
        'serviciosIds': _servicios.toList(),
      });
      ref.invalidate(profesionalesProvider);
      if (widget.existente != null) ref.invalidate(profesionalProvider(widget.existente!.id));
      if (mounted) context.go('/profesionales');
    } on FalloApi catch (f) {
      if (!mounted) return;
      setState(() {
        _fallo = f;
        _error = f.errores.isEmpty ? mensajeDeFallo(t, f) : f.errores.values.expand((e) => e).join(' ');
      });
      _clave.currentState!.validate();
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  Future<void> _cambiarEstado() async {
    final t = AppLocalizations.of(context);
    final p = widget.existente!;
    try {
      await ref.read(profesionalesRepositorioProvider).cambiarEstado(p.id, activo: !p.activo);
      ref.invalidate(profesionalesProvider);
      if (mounted) context.go('/profesionales');
    } catch (e) {
      if (mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
    }
  }

  Future<void> _elegirCaducidad(String competenciaId) async {
    final actual = _competencias[competenciaId];
    final fecha = await showDatePicker(
      context: context,
      initialDate: actual == null ? DateTime.now() : DateTime.parse(actual),
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (fecha != null) {
      setState(() => _competencias[competenciaId] =
          '${fecha.year.toString().padLeft(4, '0')}-${fecha.month.toString().padLeft(2, '0')}-${fecha.day.toString().padLeft(2, '0')}');
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final v = Validadores(t);
    final opciones = ref.watch(opcionesProfesionalProvider);

    return EstadoAsincrono(
      valor: opciones,
      alReintentar: () => ref.invalidate(opcionesProfesionalProvider),
      constructor: (o) => ContenidoCentrado(
        anchoMaximo: 640,
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Form(
            key: _clave,
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(children: [
                Expanded(child: Text(widget.existente == null ? t.nuevoProfesional : t.editarProfesional, style: Theme.of(context).textTheme.titleLarge)),
                if (widget.existente != null) ...[
                  IconButton(
                    tooltip: t.historial,
                    icon: const Icon(Icons.history),
                    onPressed: () => showDialog<void>(context: context, builder: (_) => _DialogoHistorial(id: widget.existente!.id)),
                  ),
                  TextButton(onPressed: _cambiarEstado, child: Text(widget.existente!.activo ? t.accionDesactivar : t.accionActivar)),
                ],
              ]),
              const SizedBox(height: 8),
              TextFormField(
                  controller: _nombre,
                  decoration: InputDecoration(labelText: t.campoNombre),
                  validator: (x) => v.obligatorio(x) ?? _fallo?.errorDe('Nombre')),
              TextFormField(
                  controller: _apellidos,
                  decoration: InputDecoration(labelText: t.campoApellidos),
                  validator: (x) => v.obligatorio(x) ?? _fallo?.errorDe('Apellidos')),
              DropdownButtonFormField<String>(
                initialValue: o.categorias.any((c) => c.id == _categoria) ? _categoria : null,
                decoration: InputDecoration(labelText: t.campoCategoria),
                items: [for (final c in o.categorias) DropdownMenuItem(value: c.id, child: Text(c.nombre))],
                onChanged: (x) => setState(() => _categoria = x),
              ),
              TextFormField(controller: _colegiado, decoration: InputDecoration(labelText: t.campoColegiado)),
              TextFormField(controller: _email, decoration: InputDecoration(labelText: t.campoCorreo), keyboardType: TextInputType.emailAddress),
              TextFormField(controller: _telefono, decoration: InputDecoration(labelText: t.campoTelefono), keyboardType: TextInputType.phone),
              DropdownButtonFormField<String?>(
                initialValue: o.usuarios.any((u) => u.id == _usuario) ? _usuario : null,
                decoration: InputDecoration(labelText: t.campoUsuarioVinculado),
                items: [
                  DropdownMenuItem(value: null, child: Text(t.sinUsuario)),
                  for (final u in o.usuarios) DropdownMenuItem(value: u.id, child: Text('${u.nombreCompleto} (${u.email})', overflow: TextOverflow.ellipsis)),
                ],
                onChanged: (x) => setState(() => _usuario = x),
              ),
              ChipsSeleccion(
                etiqueta: t.campoEspecialidades,
                opciones: [for (final e in o.especialidades) OpcionSeleccion(e.id, e.nombre)],
                seleccion: _especialidades,
                alCambiar: (s) => setState(() => _especialidades = s),
              ),
              ChipsSeleccion(
                etiqueta: t.campoCompetencias,
                opciones: [for (final c in o.competencias) OpcionSeleccion(c.id, c.nombre)],
                seleccion: _competencias.keys.toSet(),
                alCambiar: (s) => setState(() {
                  _competencias.removeWhere((k, _) => !s.contains(k));
                  for (final id in s) {
                    _competencias.putIfAbsent(id, () => null);
                  }
                }),
              ),
              for (final e in _competencias.entries)
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Row(children: [
                    Expanded(child: Text(o.competencias.where((c) => c.id == e.key).map((c) => c.nombre).firstOrNull ?? e.key)),
                    TextButton(
                      onPressed: () => _elegirCaducidad(e.key),
                      child: Text(e.value == null ? t.sinCaducidad : t.vigenteHasta(formatearFecha(e.value!))),
                    ),
                    if (e.value != null) IconButton(tooltip: t.sinCaducidad, icon: const Icon(Icons.clear), onPressed: () => setState(() => _competencias[e.key] = null)),
                  ]),
                ),
              ChipsSeleccion(
                etiqueta: t.campoServiciosAutorizados,
                opciones: [for (final s in o.servicios) OpcionSeleccion(s.id, '${s.centroNombre} · ${s.nombre}')],
                seleccion: _servicios,
                alCambiar: (s) => setState(() => _servicios = s),
              ),
              if (_error != null)
                Padding(padding: const EdgeInsets.only(top: 16), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
              const SizedBox(height: 24),
              Row(mainAxisAlignment: MainAxisAlignment.end, children: [
                TextButton(onPressed: _enviando ? null : () => context.go('/profesionales'), child: Text(t.botonCancelar)),
                const SizedBox(width: 8),
                FilledButton(onPressed: _enviando ? null : _guardar, child: Text(t.botonGuardar)),
              ]),
            ]),
          ),
        ),
      ),
    );
  }
}

class _DialogoHistorial extends ConsumerWidget {
  const _DialogoHistorial({required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final historial = ref.watch(_historialProvider(id));
    return AlertDialog(
      title: Text(t.historial),
      content: SizedBox(
        width: 480,
        child: EstadoAsincrono(
          valor: historial,
          constructor: (lista) => lista.isEmpty
              ? Text(t.historialVacio)
              : ListView.separated(
                  shrinkWrap: true,
                  itemCount: lista.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final c = lista[i];
                    final f = c.fecha.toLocal();
                    String d2(int n) => n.toString().padLeft(2, '0');
                    return ListTile(
                      dense: true,
                      title: Text('${c.accion} · ${c.entidad.replaceFirst('Profesional', '').isEmpty ? 'Ficha' : c.entidad.replaceFirst('Profesional', '')}'),
                      subtitle: Text('${d2(f.day)}/${d2(f.month)}/${f.year} ${d2(f.hour)}:${d2(f.minute)} · ${c.usuario ?? '—'}'),
                    );
                  },
                ),
        ),
      ),
      actions: [TextButton(onPressed: () => Navigator.pop(context), child: Text(t.cerrar))],
    );
  }
}

final _historialProvider = FutureProvider.autoDispose.family<List<CambioHistorialDto>, String>(
  (ref, id) => ref.watch(profesionalesRepositorioProvider).historial(id),
);
