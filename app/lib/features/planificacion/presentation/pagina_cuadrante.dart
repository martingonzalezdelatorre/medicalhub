import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/descarga/descargar_archivo.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/justificacion.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../estructura/data/estructura_repositorio.dart';
import '../data/planificacion_repositorio.dart';
import 'pagina_cuadrantes.dart';

/// Gestión de un cuadrante: estado, validación y publicación, versiones, plantilla y exportación.
class PaginaCuadrante extends ConsumerWidget {
  const PaginaCuadrante({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final cuadrante = ref.watch(cuadranteProvider(id));
    final perfil = ref.watch(authProvider).perfil;
    final gestiona = perfil != null && (perfil.tieneRol(Rol.administradorSociedad) || perfil.tieneRol(Rol.coordinadorMedico));

    void refrescar() {
      ref.invalidate(cuadranteProvider(id));
      ref.invalidate(cuadrantesProvider);
      ref.invalidate(versionesProvider(id));
    }

    Future<void> ejecutar(Future<void> Function() accion, {String? ok}) async {
      try {
        await accion();
        refrescar();
        if (ok != null && context.mounted) mostrarMensaje(context, ok);
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
    }

    return EstadoAsincrono(
      valor: cuadrante,
      alReintentar: () => ref.invalidate(cuadranteProvider(id)),
      constructor: (c) {
        final repo = ref.read(planificacionRepositorioProvider);
        final porcentaje = c.plazasTotales == 0 ? 0.0 : c.plazasCubiertas / c.plazasTotales;
        return ContenidoCentrado(
          child: ListView(padding: const EdgeInsets.all(16), children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [Expanded(child: Text(c.nombre, style: Theme.of(context).textTheme.titleLarge)), ChipEstado(c.estado)]),
                  const SizedBox(height: 4),
                  Text('${c.servicioNombre} · ${c.centroNombre}'),
                  Text('${rangoFechas(c)} · ${t.turnosCuenta(c.totalTurnos)}'),
                  if (c.version > 0) Text(t.versionPublicada(c.version)),
                  const SizedBox(height: 12),
                  LinearProgressIndicator(value: porcentaje, minHeight: 8, borderRadius: BorderRadius.circular(4)),
                  const SizedBox(height: 4),
                  Text(t.plazasCubiertasDe(c.plazasCubiertas, c.plazasTotales)),
                ]),
              ),
            ),
            const SizedBox(height: 12),
            Wrap(spacing: 8, runSpacing: 8, children: [
              FilledButton.icon(
                icon: const Icon(Icons.calendar_month_outlined),
                label: Text(t.verCalendario),
                onPressed: () => context.go('/planificacion?cuadrante=${c.id}&fecha=${c.desde}'),
              ),
              if (gestiona && c.estado == EstadoCuadrante.borrador) ...[
                FilledButton.tonalIcon(
                  icon: const Icon(Icons.publish_outlined),
                  label: Text(t.validarYPublicar),
                  onPressed: () async {
                    if (await showDialog<bool>(context: context, builder: (_) => _DialogoPublicar(cuadranteId: c.id)) == true) {
                      refrescar();
                      if (context.mounted) mostrarMensaje(context, t.cuadrantePublicado);
                    }
                  },
                ),
                OutlinedButton.icon(
                  icon: const Icon(Icons.add),
                  label: Text(t.anadirTurno),
                  onPressed: () async {
                    if (await showDialog<bool>(context: context, builder: (_) => _DialogoNuevoTurno(cuadrante: c)) == true) refrescar();
                  },
                ),
                if (c.version == 0)
                  OutlinedButton.icon(
                    icon: const Icon(Icons.delete_outline),
                    label: Text(t.eliminar),
                    onPressed: () async {
                      final ok = await showDialog<bool>(
                        context: context,
                        builder: (d) => AlertDialog(
                          title: Text(t.eliminarCuadrante),
                          content: Text(t.confirmaEliminarCuadrante),
                          actions: [
                            TextButton(onPressed: () => Navigator.pop(d, false), child: Text(t.botonCancelar)),
                            FilledButton(onPressed: () => Navigator.pop(d, true), child: Text(t.eliminar)),
                          ],
                        ),
                      );
                      if (ok == true) {
                        try {
                          await repo.eliminar(c.id);
                          ref.invalidate(cuadrantesProvider);
                          if (context.mounted) context.go('/cuadrantes');
                        } catch (e) {
                          if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
                        }
                      }
                    },
                  ),
              ],
              if (gestiona && c.estado == EstadoCuadrante.publicado) ...[
                FilledButton.tonalIcon(
                  icon: const Icon(Icons.edit_calendar_outlined),
                  label: Text(t.reabrir),
                  onPressed: () => ejecutar(() => repo.reabrir(c.id)),
                ),
                OutlinedButton.icon(
                  icon: const Icon(Icons.inventory_2_outlined),
                  label: Text(t.archivar),
                  onPressed: () => ejecutar(() => repo.archivar(c.id)),
                ),
              ],
              if (gestiona) ...[
                OutlinedButton.icon(
                  icon: const Icon(Icons.copy_all_outlined),
                  label: Text(t.guardarPlantilla),
                  onPressed: () async {
                    final nombre = await _pedirNombre(context, t.guardarPlantilla);
                    if (nombre != null) await ejecutar(() => repo.guardarPlantilla(c.id, nombre), ok: t.plantillaGuardada);
                  },
                ),
                OutlinedButton.icon(
                  icon: const Icon(Icons.download_outlined),
                  label: Text(t.exportarCsv),
                  onPressed: () async {
                    try {
                      final bytes = await repo.exportarCsv(c.id);
                      final nombre = 'cuadrante-${c.desde}-${c.hasta}.csv';
                      final descargado = await descargarArchivo(nombre, bytes, 'text/csv');
                      if (!descargado) {
                        await Clipboard.setData(ClipboardData(text: utf8.decode(bytes).replaceFirst('﻿', '')));
                        if (context.mounted) mostrarMensaje(context, t.csvCopiado);
                      }
                    } catch (e) {
                      if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
                    }
                  },
                ),
              ],
            ]),
            CabeceraSeccion(titulo: t.versiones),
            _Versiones(cuadranteId: c.id),
          ]),
        );
      },
    );
  }
}

class _Versiones extends ConsumerWidget {
  const _Versiones({required this.cuadranteId});
  final String cuadranteId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    return EstadoAsincrono(
      valor: ref.watch(versionesProvider(cuadranteId)),
      constructor: (lista) => lista.isEmpty
          ? Padding(padding: const EdgeInsets.all(16), child: Text(t.sinVersiones))
          : Column(children: [
              for (final v in lista)
                ListTile(
                  leading: CircleAvatar(child: Text('${v.numero}')),
                  title: Text(t.versionTitulo(v.numero)),
                  subtitle: Text('${_fechaHora(v.fecha.toLocal())} · ${v.usuario ?? '—'}\n${t.plazasCubiertasDe(v.plazasCubiertas, v.plazasTotales)} · ${t.turnosCuenta(v.turnos)}'),
                  isThreeLine: true,
                ),
            ]),
    );
  }
}

String _fechaHora(DateTime f) {
  String d2(int n) => n.toString().padLeft(2, '0');
  return '${d2(f.day)}/${d2(f.month)}/${f.year} ${d2(f.hour)}:${d2(f.minute)}';
}

Future<String?> _pedirNombre(BuildContext context, String titulo) async {
  final t = AppLocalizations.of(context);
  final c = TextEditingController();
  final r = await showDialog<String>(
    context: context,
    builder: (d) => AlertDialog(
      title: Text(titulo),
      content: TextField(controller: c, autofocus: true, decoration: InputDecoration(labelText: t.campoNombre)),
      actions: [
        TextButton(onPressed: () => Navigator.pop(d), child: Text(t.botonCancelar)),
        FilledButton(onPressed: () => Navigator.pop(d, c.text.trim().isEmpty ? null : c.text.trim()), child: Text(t.botonGuardar)),
      ],
    ),
  );
  c.dispose();
  return r;
}

/// Muestra las comprobaciones previas: las bloqueantes impiden publicar (salvo excepción autorizada);
/// las advertencias son revisables y no bloquean.
class _DialogoPublicar extends ConsumerStatefulWidget {
  const _DialogoPublicar({required this.cuadranteId});
  final String cuadranteId;

  @override
  ConsumerState<_DialogoPublicar> createState() => _EstadoDialogoPublicar();
}

class _EstadoDialogoPublicar extends ConsumerState<_DialogoPublicar> {
  late Future<ValidacionDto> _validacion = ref.read(planificacionRepositorioProvider).validar(widget.cuadranteId);
  bool _publicando = false;
  String? _error;

  void _recargar() => setState(() => _validacion = ref.read(planificacionRepositorioProvider).validar(widget.cuadranteId));

  Future<void> _publicar() async {
    final t = AppLocalizations.of(context);
    setState(() {
      _publicando = true;
      _error = null;
    });
    try {
      await ref.read(planificacionRepositorioProvider).publicar(widget.cuadranteId);
      if (mounted) Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (mounted) setState(() => _error = mensajeDeFallo(t, f));
    } finally {
      if (mounted) setState(() => _publicando = false);
    }
  }

  Future<void> _autorizar(IncidenciaDto i) async {
    final t = AppLocalizations.of(context);
    final justificacion = await pedirJustificacion(context, titulo: t.autorizarExcepcion, infracciones: [i.mensaje]);
    if (justificacion == null) return;
    try {
      await ref.read(planificacionRepositorioProvider).autorizarExcepcion(i.asignacionId!, justificacion);
      _recargar();
    } catch (e) {
      if (mounted) setState(() => _error = mensajeDeFallo(t, e));
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final esquema = Theme.of(context).colorScheme;
    return AlertDialog(
      title: Text(t.validarYPublicar),
      content: SizedBox(
        width: 560,
        child: FutureBuilder<ValidacionDto>(
          future: _validacion,
          builder: (context, snap) {
            if (snap.connectionState != ConnectionState.done) return const SizedBox(height: 80, child: Center(child: CircularProgressIndicator()));
            if (snap.hasError) return Text(mensajeDeFallo(t, snap.error!));
            final v = snap.data!;
            final bloqueantes = v.incidencias.where((i) => i.bloqueante && !i.excepcionada).toList();
            final excepciones = v.incidencias.where((i) => i.excepcionada).toList();
            final avisos = v.incidencias.where((i) => !i.bloqueante).toList();
            return SingleChildScrollView(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: [
                if (v.incidencias.isEmpty) Text(t.sinIncidencias),
                if (bloqueantes.isNotEmpty) ...[
                  Text(t.errores, style: Theme.of(context).textTheme.titleSmall?.copyWith(color: esquema.error)),
                  for (final i in bloqueantes)
                    ListTile(
                      dense: true,
                      leading: Icon(Icons.error_outline, color: esquema.error),
                      title: Text(i.mensaje),
                      trailing: i.permiteExcepcion && i.asignacionId != null
                          ? OutlinedButton(onPressed: () => _autorizar(i), child: Text(t.autorizarExcepcion))
                          : null,
                    ),
                ],
                if (excepciones.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  Text(t.excepcionesAutorizadas, style: Theme.of(context).textTheme.titleSmall),
                  for (final i in excepciones)
                    ListTile(
                      dense: true,
                      leading: const Icon(Icons.verified_outlined),
                      title: Text(i.mensaje),
                      subtitle: Text('${t.justificacion}: ${i.justificacion ?? ''}\n${t.autorizadaPor(i.autorizadaPor ?? '—')}'),
                      isThreeLine: true,
                    ),
                ],
                if (avisos.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  Text(t.advertencias, style: Theme.of(context).textTheme.titleSmall?.copyWith(color: Colors.orange.shade800)),
                  for (final i in avisos.take(30)) ListTile(dense: true, leading: Icon(Icons.warning_amber_outlined, color: Colors.orange.shade800), title: Text(i.mensaje)),
                  if (avisos.length > 30) Text(t.yMasAvisos(avisos.length - 30)),
                ],
                if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: TextStyle(color: esquema.error))),
              ]),
            );
          },
        ),
      ),
      actions: [
        TextButton(onPressed: _publicando ? null : () => Navigator.pop(context, false), child: Text(t.botonCancelar)),
        FutureBuilder<ValidacionDto>(
          future: _validacion,
          builder: (context, snap) => FilledButton(
            onPressed: _publicando || !(snap.data?.puedePublicarse ?? false) ? null : _publicar,
            child: Text(t.publicar),
          ),
        ),
      ],
    );
  }
}

class _DialogoNuevoTurno extends ConsumerStatefulWidget {
  const _DialogoNuevoTurno({required this.cuadrante});
  final CuadranteDto cuadrante;

  @override
  ConsumerState<_DialogoNuevoTurno> createState() => _EstadoDialogoNuevoTurno();
}

class _EstadoDialogoNuevoTurno extends ConsumerState<_DialogoNuevoTurno> {
  late DateTime _fecha = fechaDeApi(widget.cuadrante.desde);
  String? _tipo;
  bool _enviando = false;
  String? _error;

  Future<void> _crear() async {
    final t = AppLocalizations.of(context);
    if (_tipo == null) {
      setState(() => _error = '${t.campoTipoTurno}: ${t.validaObligatorio}');
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(planificacionRepositorioProvider).crearTurno(widget.cuadrante.id, _fecha, _tipo!);
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
    final servicio = ref.watch(servicioProvider(widget.cuadrante.servicioId));
    return AlertDialog(
      title: Text(t.anadirTurno),
      content: SizedBox(
        width: 400,
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          OutlinedButton(
            onPressed: () async {
              final f = await showDatePicker(
                context: context,
                initialDate: _fecha,
                firstDate: fechaDeApi(widget.cuadrante.desde),
                lastDate: fechaDeApi(widget.cuadrante.hasta),
              );
              if (f != null) setState(() => _fecha = f);
            },
            child: Text(formatearFecha(fechaParaApi(_fecha))),
          ),
          EstadoAsincrono(
            valor: servicio,
            constructor: (s) => DropdownButtonFormField<String>(
              initialValue: _tipo,
              isExpanded: true,
              decoration: InputDecoration(labelText: t.campoTipoTurno),
              items: [for (final x in s.tiposTurno.where((x) => x.activo)) DropdownMenuItem(value: x.id, child: Text('${x.nombre} (${x.franja})'))],
              onChanged: (v) => setState(() => _tipo = v),
            ),
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
