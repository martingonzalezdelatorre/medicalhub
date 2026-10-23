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
import '../../usuarios/data/usuarios_repositorio.dart';
import '../data/estructura_repositorio.dart';
import 'pagina_servicio.dart';

bool _esAdministrador(WidgetRef ref) => ref.watch(authProvider).perfil?.tieneRol(Rol.administradorSociedad) ?? false;

String etiquetaTipoCentro(AppLocalizations t, TipoCentro tipo) => switch (tipo) {
      TipoCentro.hospital => t.tipoHospital,
      TipoCentro.clinica => t.tipoClinica,
      TipoCentro.centroAmbulatorio => t.tipoCentroAmbulatorio,
      TipoCentro.otro => t.tipoOtro,
    };

class PaginaCentros extends ConsumerWidget {
  const PaginaCentros({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final centros = ref.watch(centrosProvider);
    return Scaffold(
      floatingActionButton: _esAdministrador(ref)
          ? FloatingActionButton.extended(
              icon: const Icon(Icons.add_business),
              label: Text(t.nuevoCentro),
              onPressed: () async {
                if (await showDialog<bool>(context: context, builder: (_) => const DialogoCentro()) == true) {
                  ref.invalidate(centrosProvider);
                }
              },
            )
          : null,
      body: EstadoAsincrono(
        valor: centros,
        alReintentar: () => ref.invalidate(centrosProvider),
        constructor: (lista) => lista.isEmpty
            ? EstadoVacio(mensaje: t.listaVacia, icono: Icons.local_hospital_outlined)
            : ContenidoCentrado(
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 88),
                  itemCount: lista.length,
                  itemBuilder: (_, i) {
                    final c = lista[i];
                    return TarjetaElemento(
                      icono: Icons.local_hospital_outlined,
                      titulo: c.nombre,
                      subtitulo: '${etiquetaTipoCentro(t, c.tipo)} · ${c.zonaHoraria}',
                      trailing: c.activo ? null : Chip(label: Text(t.estadoInactivo)),
                      onTap: () => context.go('/centros/${c.id}'),
                    );
                  },
                ),
              ),
      ),
    );
  }
}

class PaginaCentroDetalle extends ConsumerWidget {
  const PaginaCentroDetalle({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final centro = ref.watch(centroProvider(id));
    final servicios = ref.watch(serviciosDeCentroProvider(id));
    final admin = _esAdministrador(ref);

    return Scaffold(
      floatingActionButton: admin
          ? FloatingActionButton.extended(
              icon: const Icon(Icons.add),
              label: Text(t.nuevoServicio),
              onPressed: () async {
                if (await showDialog<bool>(context: context, builder: (_) => DialogoServicio(centroId: id)) == true) {
                  ref.invalidate(serviciosDeCentroProvider(id));
                }
              },
            )
          : null,
      body: EstadoAsincrono(
        valor: centro,
        alReintentar: () => ref.invalidate(centroProvider(id)),
        constructor: (c) => ContenidoCentrado(
          child: ListView(padding: const EdgeInsets.only(bottom: 88), children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Card(
                child: ListTile(
                  title: Text(c.nombre, style: Theme.of(context).textTheme.titleLarge),
                  subtitle: Text([etiquetaTipoCentro(t, c.tipo), c.zonaHoraria, if (c.direccion != null) c.direccion!].join(' · ')),
                  trailing: admin
                      ? Row(mainAxisSize: MainAxisSize.min, children: [
                          IconButton(
                            tooltip: t.campoResponsables,
                            icon: const Icon(Icons.manage_accounts_outlined),
                            onPressed: () async {
                              if (await showDialog<bool>(context: context, builder: (_) => _DialogoResponsables(centro: c)) == true) {
                                ref.invalidate(centroProvider(id));
                              }
                            },
                          ),
                          IconButton(
                            tooltip: t.editar,
                            icon: const Icon(Icons.edit_outlined),
                            onPressed: () async {
                              if (await showDialog<bool>(context: context, builder: (_) => DialogoCentro(existente: c)) == true) {
                                ref.invalidate(centroProvider(id));
                                ref.invalidate(centrosProvider);
                              }
                            },
                          ),
                        ])
                      : null,
                ),
              ),
            ),
            CabeceraSeccion(titulo: t.serviciosTitulo),
            EstadoAsincrono(
              valor: servicios,
              alReintentar: () => ref.invalidate(serviciosDeCentroProvider(id)),
              constructor: (lista) => lista.isEmpty
                  ? Padding(padding: const EdgeInsets.all(16), child: Text(t.listaVacia))
                  : Column(children: [
                      for (final s in lista)
                        ListTile(
                          leading: const Icon(Icons.medical_services_outlined),
                          title: Text(s.nombre),
                          subtitle: Text(s.tiposTurno.where((x) => x.activo).map((x) => x.nombre).join(' · ')),
                          trailing: s.activo ? const Icon(Icons.chevron_right) : Chip(label: Text(t.estadoInactivo)),
                          onTap: () => context.go('/servicios/${s.id}'),
                        ),
                    ]),
            ),
          ]),
        ),
      ),
    );
  }
}

class DialogoCentro extends ConsumerStatefulWidget {
  const DialogoCentro({super.key, this.existente});
  final CentroDto? existente;

  @override
  ConsumerState<DialogoCentro> createState() => _EstadoDialogoCentro();
}

class _EstadoDialogoCentro extends ConsumerState<DialogoCentro> {
  final _formulario = GlobalKey<FormState>();
  late final _nombre = TextEditingController(text: widget.existente?.nombre);
  late final _direccion = TextEditingController(text: widget.existente?.direccion);
  late TipoCentro _tipo = widget.existente?.tipo ?? TipoCentro.hospital;
  late String _zona = widget.existente?.zonaHoraria ?? zonasHorarias.first;
  bool _enviando = false;
  String? _error;

  @override
  void dispose() {
    _nombre.dispose();
    _direccion.dispose();
    super.dispose();
  }

  Future<void> _guardar() async {
    if (!_formulario.currentState!.validate()) return;
    final t = AppLocalizations.of(context);
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(estructuraRepositorioProvider).guardarCentro(widget.existente?.id, {
        'nombre': _nombre.text.trim(),
        'tipo': _tipo.valorApi,
        'direccion': _direccion.text.trim().isEmpty ? null : _direccion.text.trim(),
        'zonaHoraria': _zona,
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
    final zonas = {...zonasHorarias, _zona}.toList();
    return AlertDialog(
      title: Text(widget.existente == null ? t.nuevoCentro : t.editarCentro),
      content: SizedBox(
        width: 420,
        child: SingleChildScrollView(
          child: Form(
            key: _formulario,
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              TextFormField(controller: _nombre, decoration: InputDecoration(labelText: t.campoNombre), validator: Validadores(t).obligatorio),
              DropdownButtonFormField<TipoCentro>(
                initialValue: _tipo,
                decoration: InputDecoration(labelText: t.campoTipoCentro),
                items: [for (final x in TipoCentro.values) DropdownMenuItem(value: x, child: Text(etiquetaTipoCentro(t, x)))],
                onChanged: (x) => setState(() => _tipo = x ?? _tipo),
              ),
              TextFormField(controller: _direccion, decoration: InputDecoration(labelText: t.campoDireccion)),
              DropdownButtonFormField<String>(
                initialValue: _zona,
                decoration: InputDecoration(labelText: t.campoZonaHoraria),
                items: [for (final z in zonas) DropdownMenuItem(value: z, child: Text(z))],
                onChanged: (z) => setState(() => _zona = z ?? _zona),
              ),
              if (_error != null)
                Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
            ]),
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

class _DialogoResponsables extends ConsumerStatefulWidget {
  const _DialogoResponsables({required this.centro});
  final CentroDto centro;

  @override
  ConsumerState<_DialogoResponsables> createState() => _EstadoDialogoResponsables();
}

class _EstadoDialogoResponsables extends ConsumerState<_DialogoResponsables> {
  late Set<String> _seleccion = {...widget.centro.responsablesIds};
  bool _enviando = false;
  String? _error;

  Future<void> _guardar() async {
    final t = AppLocalizations.of(context);
    setState(() => _enviando = true);
    try {
      await ref.read(estructuraRepositorioProvider).establecerResponsables(widget.centro.id, _seleccion.toList());
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
    final usuarios = ref.watch(usuariosProvider);
    return AlertDialog(
      title: Text(t.campoResponsables),
      content: SizedBox(
        width: 420,
        child: EstadoAsincrono(
          valor: usuarios,
          constructor: (lista) {
            final candidatos = lista.where((u) => u.activo && u.roles.contains(Rol.responsableCentro)).toList();
            if (candidatos.isEmpty) return Text(t.sinElementos);
            return SingleChildScrollView(
              child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
                ChipsSeleccion(
                  etiqueta: t.rolResponsableCentro,
                  opciones: [for (final u in candidatos) OpcionSeleccion(u.id, u.nombreCompleto)],
                  seleccion: _seleccion,
                  alCambiar: (s) => setState(() => _seleccion = s),
                ),
                if (_error != null)
                  Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
              ]),
            );
          },
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _guardar, child: Text(t.botonGuardar)),
      ],
    );
  }
}

