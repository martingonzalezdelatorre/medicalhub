import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../estructura/data/estructura_repositorio.dart';
import '../data/planificacion_repositorio.dart';

String etiquetaEstado(AppLocalizations t, EstadoCuadrante e) => switch (e) {
      EstadoCuadrante.borrador => t.estadoBorrador,
      EstadoCuadrante.publicado => t.estadoPublicado,
      EstadoCuadrante.archivado => t.estadoArchivado,
    };

Color colorEstado(BuildContext context, EstadoCuadrante e) => switch (e) {
      EstadoCuadrante.borrador => Colors.orange.shade700,
      EstadoCuadrante.publicado => Colors.green.shade700,
      EstadoCuadrante.archivado => Theme.of(context).colorScheme.outline,
    };

class ChipEstado extends StatelessWidget {
  const ChipEstado(this.estado, {super.key});
  final EstadoCuadrante estado;

  @override
  Widget build(BuildContext context) {
    final color = colorEstado(context, estado);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.14), borderRadius: BorderRadius.circular(20)),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        Container(width: 7, height: 7, margin: const EdgeInsets.only(right: 6), decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
        Text(etiquetaEstado(AppLocalizations.of(context), estado), style: TextStyle(color: color, fontWeight: FontWeight.w700, fontSize: 12)),
      ]),
    );
  }
}

String rangoFechas(CuadranteDto c) => '${formatearFecha(c.desde)} – ${formatearFecha(c.hasta)}';

class PaginaCuadrantes extends ConsumerWidget {
  const PaginaCuadrantes({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final cuadrantes = ref.watch(cuadrantesProvider);
    final perfil = ref.watch(authProvider).perfil;
    final gestiona = perfil != null && (perfil.tieneRol(Rol.administradorSociedad) || perfil.tieneRol(Rol.coordinadorMedico));

    Future<void> abrir(Widget dialogo) async {
      final creado = await showDialog<CuadranteDto>(context: context, builder: (_) => dialogo);
      if (creado != null) {
        ref.invalidate(cuadrantesProvider);
        if (context.mounted) {
          mostrarMensaje(context, creado.totalTurnos == 0 ? t.cuadranteSinTurnos : t.cuadranteCreadoResumen(creado.totalTurnos, creado.plazasCubiertas));
          context.go('/cuadrantes/${creado.id}');
        }
      }
    }

    return Scaffold(
      floatingActionButton: gestiona
          ? Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.end, children: [
              FloatingActionButton.small(
                heroTag: 'fab-plantilla',
                tooltip: t.desdePlantilla,
                onPressed: () => abrir(const _DialogoDesdePlantilla()),
                child: const Icon(Icons.copy_all_outlined),
              ),
              const SizedBox(height: 12),
              FloatingActionButton.extended(
                heroTag: 'fab-cuadrante',
                icon: const Icon(Icons.add),
                label: Text(t.nuevoCuadrante),
                onPressed: () => abrir(const _DialogoNuevoCuadrante()),
              ),
            ])
          : null,
      body: EstadoAsincrono(
        valor: cuadrantes,
        alReintentar: () => ref.invalidate(cuadrantesProvider),
        constructor: (lista) => lista.isEmpty
            ? EstadoVacio(mensaje: t.listaVacia, icono: Icons.view_timeline_outlined)
            : ContenidoCentrado(
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 160),
                  itemCount: lista.length,
                  itemBuilder: (_, i) {
                    final c = lista[i];
                    final cs = Theme.of(context).colorScheme;
                    final ratio = c.plazasTotales == 0 ? 0.0 : (c.plazasCubiertas / c.plazasTotales).clamp(0.0, 1.0);
                    final colorBarra = ratio >= 0.95 ? Colors.green.shade600 : (ratio >= 0.5 ? Colors.orange.shade700 : cs.error);
                    return Card(
                      margin: const EdgeInsets.symmetric(vertical: 6),
                      child: InkWell(
                        onTap: () => context.go('/cuadrantes/${c.id}'),
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Row(children: [
                              Expanded(child: Text(c.nombre, style: Theme.of(context).textTheme.titleMedium)),
                              const SizedBox(width: 8),
                              ChipEstado(c.estado),
                            ]),
                            const SizedBox(height: 4),
                            Text('${c.servicioNombre} · ${c.centroNombre}',
                                style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
                            const SizedBox(height: 12),
                            ClipRRect(
                              borderRadius: BorderRadius.circular(6),
                              child: LinearProgressIndicator(value: ratio, minHeight: 8, color: colorBarra, backgroundColor: colorBarra.withValues(alpha: 0.15)),
                            ),
                            const SizedBox(height: 10),
                            Row(children: [
                              Icon(Icons.date_range_outlined, size: 16, color: cs.onSurfaceVariant),
                              const SizedBox(width: 6),
                              Expanded(child: Text(rangoFechas(c), style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant))),
                              Text(t.plazasCubiertasDe(c.plazasCubiertas, c.plazasTotales),
                                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: colorBarra, fontWeight: FontWeight.w700)),
                            ]),
                          ]),
                        ),
                      ),
                    );
                  },
                ),
              ),
      ),
    );
  }
}

Future<DateTime?> _elegirFecha(BuildContext context, DateTime inicial) =>
    showDatePicker(context: context, initialDate: inicial, firstDate: DateTime(2020), lastDate: DateTime(2100));

class _DialogoNuevoCuadrante extends ConsumerStatefulWidget {
  const _DialogoNuevoCuadrante();

  @override
  ConsumerState<_DialogoNuevoCuadrante> createState() => _EstadoDialogoNuevoCuadrante();
}

class _EstadoDialogoNuevoCuadrante extends ConsumerState<_DialogoNuevoCuadrante> {
  final _nombre = TextEditingController();
  String? _servicio;
  late DateTime _desde = _lunesProximo();
  late DateTime _hasta = _desde.add(const Duration(days: 6));
  bool _generar = true;
  bool _enviando = false;
  String? _error;

  static DateTime _lunesProximo() {
    final hoy = DateTime.now();
    final dias = (DateTime.monday - hoy.weekday + 7) % 7;
    return DateTime(hoy.year, hoy.month, hoy.day + (dias == 0 ? 7 : dias));
  }

  @override
  void dispose() {
    _nombre.dispose();
    super.dispose();
  }

  Future<void> _crear() async {
    final t = AppLocalizations.of(context);
    if (_servicio == null) {
      setState(() => _error = '${t.filtroServicio}: ${t.validaObligatorio}');
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      final c = await ref.read(planificacionRepositorioProvider).crear(
            servicioId: _servicio!,
            nombre: _nombre.text.trim().isEmpty ? null : _nombre.text.trim(),
            desde: _desde,
            hasta: _hasta,
            generarTurnos: _generar,
          );
      if (mounted) Navigator.pop(context, c);
    } on FalloApi catch (f) {
      if (mounted) setState(() => _error = f.errores.isEmpty ? mensajeDeFallo(t, f) : f.errores.values.expand((e) => e).join(' '));
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final servicios = ref.watch(serviciosActivosProvider);
    return AlertDialog(
      title: Text(t.nuevoCuadrante),
      content: SizedBox(
        width: 440,
        child: SingleChildScrollView(
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            EstadoAsincrono(
              valor: servicios,
              constructor: (lista) => DropdownButtonFormField<String>(
                initialValue: _servicio,
                isExpanded: true,
                decoration: InputDecoration(labelText: t.filtroServicio),
                items: [for (final s in lista) DropdownMenuItem(value: s.id, child: Text('${s.centroNombre} · ${s.nombre}', overflow: TextOverflow.ellipsis))],
                onChanged: (v) => setState(() => _servicio = v),
              ),
            ),
            TextField(controller: _nombre, decoration: InputDecoration(labelText: t.nombreOpcional)),
            const SizedBox(height: 12),
            Row(children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: () async {
                    final f = await _elegirFecha(context, _desde);
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
                    final f = await _elegirFecha(context, _hasta);
                    if (f != null) setState(() => _hasta = f);
                  },
                  child: Text('${t.campoHasta}: ${formatearFecha(fechaParaApi(_hasta))}'),
                ),
              ),
            ]),
            SwitchListTile(contentPadding: EdgeInsets.zero, title: Text(t.generarTurnos), value: _generar, onChanged: (v) => setState(() => _generar = v)),
            if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
          ]),
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _crear, child: Text(t.crear)),
      ],
    );
  }
}

class _DialogoDesdePlantilla extends ConsumerStatefulWidget {
  const _DialogoDesdePlantilla();

  @override
  ConsumerState<_DialogoDesdePlantilla> createState() => _EstadoDialogoDesdePlantilla();
}

class _EstadoDialogoDesdePlantilla extends ConsumerState<_DialogoDesdePlantilla> {
  String? _plantilla;
  DateTime _desde = DateTime.now().add(const Duration(days: 7));
  bool _enviando = false;
  String? _error;

  Future<void> _crear() async {
    final t = AppLocalizations.of(context);
    if (_plantilla == null) {
      setState(() => _error = '${t.plantilla}: ${t.validaObligatorio}');
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      final c = await ref.read(planificacionRepositorioProvider).desdePlantilla(plantillaId: _plantilla!, desde: _desde);
      if (mounted) Navigator.pop(context, c);
    } on FalloApi catch (f) {
      if (mounted) setState(() => _error = mensajeDeFallo(t, f));
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final plantillas = ref.watch(plantillasProvider);
    return AlertDialog(
      title: Text(t.desdePlantilla),
      content: SizedBox(
        width: 420,
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          EstadoAsincrono(
            valor: plantillas,
            constructor: (lista) => lista.isEmpty
                ? Text(t.sinPlantillas)
                : DropdownButtonFormField<String>(
                    initialValue: _plantilla,
                    isExpanded: true,
                    decoration: InputDecoration(labelText: t.plantilla),
                    items: [for (final p in lista) DropdownMenuItem(value: p.id, child: Text('${p.nombre} (${t.diasPlantilla(p.diasPeriodo)})', overflow: TextOverflow.ellipsis))],
                    onChanged: (v) => setState(() => _plantilla = v),
                  ),
          ),
          const SizedBox(height: 12),
          OutlinedButton(
            onPressed: () async {
              final f = await _elegirFecha(context, _desde);
              if (f != null) setState(() => _desde = f);
            },
            child: Text('${t.campoDesde}: ${formatearFecha(fechaParaApi(_desde))}'),
          ),
          if (_error != null) Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
        ]),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _crear, child: Text(t.crear)),
      ],
    );
  }
}
