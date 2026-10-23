import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../catalogos/data/catalogos_repositorio.dart';
import '../data/profesionales_repositorio.dart';

class PaginaProfesionales extends ConsumerStatefulWidget {
  const PaginaProfesionales({super.key});

  @override
  ConsumerState<PaginaProfesionales> createState() => _EstadoPaginaProfesionales();
}

class _EstadoPaginaProfesionales extends ConsumerState<PaginaProfesionales> {
  final _busqueda = TextEditingController();
  FiltroProfesionales _filtro = const FiltroProfesionales();

  @override
  void dispose() {
    _busqueda.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final admin = ref.watch(authProvider).perfil?.tieneRol(Rol.administradorSociedad) ?? false;
    final profesionales = ref.watch(profesionalesProvider(_filtro));
    final categorias = {for (final c in ref.watch(catalogoCompletoProvider(TipoCatalogo.categorias)).value ?? <CatalogoDto>[]) c.id: c.nombre};

    return Scaffold(
      floatingActionButton: admin
          ? FloatingActionButton.extended(
              icon: const Icon(Icons.person_add_alt),
              label: Text(t.nuevoProfesional),
              onPressed: () => context.go('/profesionales/nuevo'),
            )
          : null,
      body: ContenidoCentrado(
        child: Column(children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: Row(children: [
              Expanded(
                child: TextField(
                  controller: _busqueda,
                  decoration: InputDecoration(prefixIcon: const Icon(Icons.search), labelText: t.buscar, isDense: true, border: const OutlineInputBorder()),
                  textInputAction: TextInputAction.search,
                  onSubmitted: (v) => setState(() => _filtro = FiltroProfesionales(texto: v, soloActivos: _filtro.soloActivos)),
                ),
              ),
              const SizedBox(width: 12),
              FilterChip(
                label: Text(t.soloActivos),
                selected: _filtro.soloActivos,
                onSelected: (v) => setState(() => _filtro = FiltroProfesionales(texto: _filtro.texto, soloActivos: v)),
              ),
            ]),
          ),
          Expanded(
            child: EstadoAsincrono(
              valor: profesionales,
              alReintentar: () => ref.invalidate(profesionalesProvider(_filtro)),
              constructor: (lista) => lista.isEmpty
                  ? EstadoVacio(mensaje: t.listaVacia, icono: Icons.badge_outlined)
                  : ListView.builder(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 88),
                      itemCount: lista.length,
                      itemBuilder: (_, i) {
                        final p = lista[i];
                        return TarjetaElemento(
                          inicial: p.nombre.characters.first.toUpperCase(),
                          titulo: p.nombreCompleto,
                          tachado: !p.activo,
                          subtitulo: [categorias[p.categoriaId] ?? '', if (p.numeroColegiado != null) p.numeroColegiado!].where((x) => x.isNotEmpty).join(' · '),
                          onTap: () => context.go('/profesionales/${p.id}'),
                        );
                      },
                    ),
            ),
          ),
        ]),
      ),
    );
  }
}
