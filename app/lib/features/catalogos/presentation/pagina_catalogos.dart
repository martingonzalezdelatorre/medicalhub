import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../data/catalogos_repositorio.dart';

class PaginaCatalogos extends StatelessWidget {
  const PaginaCatalogos({super.key});

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return DefaultTabController(
      length: TipoCatalogo.values.length,
      child: Column(children: [
        TabBar(tabs: [
          Tab(text: t.catEspecialidades),
          Tab(text: t.catCategorias),
          Tab(text: t.catCompetencias),
        ]),
        const Expanded(
          child: TabBarView(children: [
            _ListaCatalogo(TipoCatalogo.especialidades),
            _ListaCatalogo(TipoCatalogo.categorias),
            _ListaCatalogo(TipoCatalogo.competencias),
          ]),
        ),
      ]),
    );
  }
}

class _ListaCatalogo extends ConsumerWidget {
  const _ListaCatalogo(this.tipo);
  final TipoCatalogo tipo;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final datos = ref.watch(catalogoCompletoProvider(tipo));
    final repo = ref.read(catalogosRepositorioProvider);

    Future<void> editar([CatalogoDto? existente]) async {
      final guardado = await showDialog<bool>(context: context, builder: (_) => _DialogoCatalogo(tipo: tipo, existente: existente));
      if (guardado == true) ref.invalidate(catalogoCompletoProvider(tipo));
    }

    return Scaffold(
      floatingActionButton: FloatingActionButton.extended(
        heroTag: 'fab-${tipo.name}',
        icon: const Icon(Icons.add),
        label: Text(t.catNuevo),
        onPressed: () => editar(),
      ),
      body: EstadoAsincrono(
        valor: datos,
        alReintentar: () => ref.invalidate(catalogoCompletoProvider(tipo)),
        constructor: (lista) => lista.isEmpty
            ? EstadoVacio(mensaje: t.listaVacia, icono: Icons.category_outlined)
            : ContenidoCentrado(
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 88),
                  itemCount: lista.length,
                  itemBuilder: (_, i) {
                    final c = lista[i];
                    return TarjetaElemento(
                      icono: Icons.label_outline,
                      titulo: c.nombre,
                      tachado: !c.activa,
                      subtitulo: c.descripcion,
                      trailing: PopupMenuButton<String>(
                        onSelected: (accion) async {
                          if (accion == 'editar') return editar(c);
                          try {
                            await repo.cambiarEstado(tipo, c.id, activa: !c.activa);
                            ref.invalidate(catalogoCompletoProvider(tipo));
                          } catch (e) {
                            if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
                          }
                        },
                        itemBuilder: (_) => [
                          PopupMenuItem(value: 'editar', child: Text(t.editar)),
                          PopupMenuItem(value: 'estado', child: Text(c.activa ? t.accionDesactivar : t.accionActivar)),
                        ],
                      ),
                    );
                  },
                ),
              ),
      ),
    );
  }
}

class _DialogoCatalogo extends ConsumerStatefulWidget {
  const _DialogoCatalogo({required this.tipo, this.existente});
  final TipoCatalogo tipo;
  final CatalogoDto? existente;

  @override
  ConsumerState<_DialogoCatalogo> createState() => _EstadoDialogoCatalogo();
}

class _EstadoDialogoCatalogo extends ConsumerState<_DialogoCatalogo> {
  final _formulario = GlobalKey<FormState>();
  late final _nombre = TextEditingController(text: widget.existente?.nombre);
  late final _descripcion = TextEditingController(text: widget.existente?.descripcion);
  bool _enviando = false;
  String? _error;

  @override
  void dispose() {
    _nombre.dispose();
    _descripcion.dispose();
    super.dispose();
  }

  Future<void> _guardar() async {
    if (!_formulario.currentState!.validate()) return;
    final t = AppLocalizations.of(context);
    setState(() {
      _enviando = true;
      _error = null;
    });
    final repo = ref.read(catalogosRepositorioProvider);
    final descripcion = _descripcion.text.trim().isEmpty ? null : _descripcion.text.trim();
    try {
      if (widget.existente == null) {
        await repo.crear(widget.tipo, _nombre.text.trim(), descripcion);
      } else {
        await repo.actualizar(widget.tipo, widget.existente!.id, _nombre.text.trim(), descripcion);
      }
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
      title: Text(widget.existente == null ? t.catNuevo : t.catEditar),
      content: SizedBox(
        width: 400,
        child: Form(
          key: _formulario,
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            TextFormField(
              controller: _nombre,
              autofocus: true,
              decoration: InputDecoration(labelText: t.campoNombre),
              validator: Validadores(t).obligatorio,
            ),
            TextFormField(controller: _descripcion, decoration: InputDecoration(labelText: t.campoDescripcion), maxLines: 2),
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
