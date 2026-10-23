import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/descarga/descargar_archivo.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../estructura/data/estructura_repositorio.dart';
import '../../planificacion/data/planificacion_repositorio.dart' show fechaParaApi;
import '../data/informes_repositorio.dart';

/// Informes de horas y de cobertura, calculados en el servidor. Los gestores además registran horas y descargan CSV.
class PaginaInformes extends ConsumerStatefulWidget {
  const PaginaInformes({super.key});

  @override
  ConsumerState<PaginaInformes> createState() => _EstadoPaginaInformes();
}

class _EstadoPaginaInformes extends ConsumerState<PaginaInformes> {
  late DateTime _desde;
  late DateTime _hasta;
  String? _servicio;

  @override
  void initState() {
    super.initState();
    final hoy = DateTime.now();
    _desde = DateTime(hoy.year, hoy.month, 1);
    _hasta = DateTime(hoy.year, hoy.month + 1, 0);
  }

  FiltroInforme get _filtro => FiltroInforme(desde: _desde, hasta: _hasta, servicioId: _servicio);

  Future<void> _elegir({required bool inicio}) async {
    final f = await showDatePicker(context: context, initialDate: inicio ? _desde : _hasta, firstDate: DateTime(2020), lastDate: DateTime(2100));
    if (f == null) return;
    setState(() {
      if (inicio) {
        _desde = f;
        if (_hasta.isBefore(f)) _hasta = f;
      } else {
        _hasta = f;
        if (_desde.isAfter(f)) _desde = f;
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final servicios = ref.watch(serviciosActivosProvider).value ?? const <ServicioDto>[];
    final gestion = ref.watch(authProvider).perfil?.let((p) => p.tieneRol(Rol.administradorSociedad) || p.tieneRol(Rol.coordinadorMedico)) ?? false;

    return DefaultTabController(
      length: 3,
      child: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 8, 12, 0),
          child: Wrap(spacing: 8, runSpacing: 8, crossAxisAlignment: WrapCrossAlignment.center, children: [
            OutlinedButton(onPressed: () => _elegir(inicio: true), child: Text('${t.campoDesde}: ${formatearFecha(fechaParaApi(_desde))}')),
            OutlinedButton(onPressed: () => _elegir(inicio: false), child: Text('${t.campoHasta}: ${formatearFecha(fechaParaApi(_hasta))}')),
            SizedBox(
              width: 240,
              child: DropdownButtonFormField<String?>(
                initialValue: _servicio,
                isExpanded: true,
                decoration: InputDecoration(labelText: t.filtroServicio, isDense: true),
                items: [
                  DropdownMenuItem(value: null, child: Text(t.filtroTodos)),
                  for (final s in servicios) DropdownMenuItem(value: s.id, child: Text(s.nombre, overflow: TextOverflow.ellipsis)),
                ],
                onChanged: (v) => setState(() => _servicio = v),
              ),
            ),
          ]),
        ),
        TabBar(tabs: [Tab(text: t.informesTabHoras), Tab(text: t.informesTabRegistro), Tab(text: t.informesTabCobertura)]),
        Expanded(
          child: TabBarView(children: [
            _TablaHoras(filtro: _filtro, puedeExportar: gestion),
            _RegistroHoras(filtro: _filtro, puedeEditar: gestion),
            _TablaCobertura(filtro: _filtro, puedeExportar: gestion),
          ]),
        ),
      ]),
    );
  }
}

extension on PerfilUsuario {
  R let<R>(R Function(PerfilUsuario p) f) => f(this);
}

Future<void> _descargar(BuildContext context, WidgetRef ref, String informe, FiltroInforme f) async {
  final t = AppLocalizations.of(context);
  try {
    final bytes = await ref.read(informesRepositorioProvider).csv(informe, f);
    final nombre = 'informe-$informe-${fechaParaApi(f.desde)}-${fechaParaApi(f.hasta)}.csv';
    if (!await descargarArchivo(nombre, bytes, 'text/csv')) {
      await Clipboard.setData(ClipboardData(text: utf8.decode(bytes).replaceFirst('﻿', '')));
      if (context.mounted) mostrarMensaje(context, t.csvCopiado);
    }
  } catch (e) {
    if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
  }
}

class _TablaHoras extends ConsumerWidget {
  const _TablaHoras({required this.filtro, required this.puedeExportar});
  final FiltroInforme filtro;
  final bool puedeExportar;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final datos = ref.watch(informeHorasProvider(filtro));
    return EstadoAsincrono(
      valor: datos,
      alReintentar: () => ref.invalidate(informeHorasProvider),
      constructor: (i) => i.filas.isEmpty
          ? Center(child: Text(t.sinDatosPeriodo))
          : ListView(padding: const EdgeInsets.all(12), children: [
              if (puedeExportar)
                Align(
                  alignment: Alignment.centerRight,
                  child: OutlinedButton.icon(icon: const Icon(Icons.download_outlined), label: Text(t.exportarCsv), onPressed: () => _descargar(context, ref, 'horas', filtro)),
                ),
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: DataTable(
                  columns: [
                    DataColumn(label: Text(t.campoProfesional)),
                    DataColumn(label: Text(t.colTurnos), numeric: true),
                    DataColumn(label: Text(t.colGuardias), numeric: true),
                    DataColumn(label: Text(t.colPlanificadas), numeric: true),
                    DataColumn(label: Text(t.colNocturnas), numeric: true),
                    DataColumn(label: Text(t.colRegistradas), numeric: true),
                    DataColumn(label: Text(t.colAjustes), numeric: true),
                    DataColumn(label: Text(t.colEfectivas), numeric: true),
                  ],
                  rows: [
                    for (final f in [...i.filas, i.total])
                      DataRow(cells: [
                        DataCell(Text(f == i.total ? t.colTotal : f.profesional, style: f == i.total ? const TextStyle(fontWeight: FontWeight.bold) : null)),
                        DataCell(Text('${f.turnos}')),
                        DataCell(Text('${f.guardias}')),
                        DataCell(Text(formatearMinutos(f.planificadasMin))),
                        DataCell(Text(formatearMinutos(f.nocturnasPlanMin))),
                        DataCell(Text(f.turnosRegistrados == 0 ? '—' : formatearMinutos(f.realesMin))),
                        DataCell(Text(formatearMinutos(f.ajustesMin))),
                        DataCell(Text(f.turnosRegistrados == 0 ? '—' : formatearMinutos(f.efectivasMin))),
                      ]),
                  ],
                ),
              ),
            ]),
    );
  }
}

class _TablaCobertura extends ConsumerWidget {
  const _TablaCobertura({required this.filtro, required this.puedeExportar});
  final FiltroInforme filtro;
  final bool puedeExportar;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final datos = ref.watch(informeCoberturaProvider(filtro));
    return EstadoAsincrono(
      valor: datos,
      alReintentar: () => ref.invalidate(informeCoberturaProvider),
      constructor: (i) => i.filas.isEmpty
          ? Center(child: Text(t.sinDatosPeriodo))
          : ListView(padding: const EdgeInsets.all(12), children: [
              if (puedeExportar)
                Align(
                  alignment: Alignment.centerRight,
                  child: OutlinedButton.icon(icon: const Icon(Icons.download_outlined), label: Text(t.exportarCsv), onPressed: () => _descargar(context, ref, 'cobertura', filtro)),
                ),
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: DataTable(
                  columns: [
                    DataColumn(label: Text(t.filtroCentro)),
                    DataColumn(label: Text(t.filtroServicio)),
                    DataColumn(label: Text(t.colPlazas), numeric: true),
                    DataColumn(label: Text(t.colCubiertas), numeric: true),
                    DataColumn(label: Text(t.colLibres), numeric: true),
                    DataColumn(label: Text(t.colPorcentaje), numeric: true),
                    DataColumn(label: Text(t.colVacantes), numeric: true),
                    DataColumn(label: Text(t.colExcepciones), numeric: true),
                  ],
                  rows: [
                    for (final f in [...i.filas, i.total])
                      DataRow(cells: [
                        DataCell(Text(f.centro)),
                        DataCell(Text(f == i.total ? t.colTotal : f.servicio, style: f == i.total ? const TextStyle(fontWeight: FontWeight.bold) : null)),
                        DataCell(Text('${f.plazas}')),
                        DataCell(Text('${f.cubiertas}')),
                        DataCell(Text('${f.libres}')),
                        DataCell(Text('${f.porcentajeCobertura.toStringAsFixed(1).replaceAll('.', ',')} %')),
                        DataCell(Text('${f.vacantesAbiertas}')),
                        DataCell(Text('${f.excepciones}')),
                      ]),
                  ],
                ),
              ),
            ]),
    );
  }
}

class _RegistroHoras extends ConsumerWidget {
  const _RegistroHoras({required this.filtro, required this.puedeEditar});
  final FiltroInforme filtro;
  final bool puedeEditar;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final datos = ref.watch(lineasHorasProvider(filtro));

    Future<void> registrar(LineaHorasDto l) async {
      if (await showDialog<bool>(context: context, builder: (_) => _DialogoRegistro(linea: l)) == true) {
        ref.invalidate(lineasHorasProvider);
        ref.invalidate(informeHorasProvider);
      }
    }

    Future<void> ajustar(LineaHorasDto l) async {
      if (await showDialog<bool>(context: context, builder: (_) => _DialogoAjuste(linea: l)) == true) {
        ref.invalidate(lineasHorasProvider);
        ref.invalidate(informeHorasProvider);
      }
    }

    return EstadoAsincrono(
      valor: datos,
      alReintentar: () => ref.invalidate(lineasHorasProvider),
      constructor: (lista) => lista.isEmpty
          ? Center(child: Text(t.sinDatosPeriodo))
          : ContenidoCentrado(
              child: ListView(padding: const EdgeInsets.symmetric(vertical: 8), children: [
                for (final l in lista)
                  Card(
                    margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    child: ListTile(
                      isThreeLine: true,
                      title: Text('${l.profesional} · ${formatearFecha(l.fecha)}'),
                      subtitle: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text('${l.turno} · ${l.centro} · ${l.servicio}'),
                        Text('${t.colPlanificadas}: ${formatearMinutos(l.planificadasMin)} (${t.colNocturnas}: ${formatearMinutos(l.nocturnasPlanMin)})'),
                        Text(l.registrada
                            ? '${t.colRegistradas}: ${formatearMinutos(l.realesMin!)} · ${t.colAjustes}: ${formatearMinutos(l.ajustesMin)} · ${t.colEfectivas}: ${formatearMinutos(l.efectivasMin!)}'
                            : t.sinRegistroHoras),
                        for (final a in l.ajustes) Text('${formatearMinutos(a.minutos)} — ${a.motivo} (${a.usuario})', style: Theme.of(context).textTheme.bodySmall),
                      ]),
                      trailing: puedeEditar && !l.inicioUtc.isAfter(DateTime.now())
                          ? Wrap(spacing: 4, children: [
                              TextButton(onPressed: () => registrar(l), child: Text(t.registrarHoras)),
                              if (l.registrada) TextButton(onPressed: () => ajustar(l), child: Text(t.ajustarHoras)),
                            ])
                          : null,
                    ),
                  ),
              ]),
            ),
    );
  }
}

class _DialogoRegistro extends ConsumerStatefulWidget {
  const _DialogoRegistro({required this.linea});
  final LineaHorasDto linea;

  @override
  ConsumerState<_DialogoRegistro> createState() => _EstadoDialogoRegistro();
}

class _EstadoDialogoRegistro extends ConsumerState<_DialogoRegistro> {
  late DateTime _inicio = (widget.linea.inicioRealUtc ?? widget.linea.inicioUtc).toLocal();
  late DateTime _fin = (widget.linea.finRealUtc ?? widget.linea.finUtc).toLocal();
  String? _error;
  bool _enviando = false;

  Future<void> _elegir({required bool inicio}) async {
    final actual = inicio ? _inicio : _fin;
    final fecha = await showDatePicker(context: context, initialDate: actual, firstDate: DateTime(2020), lastDate: DateTime(2100));
    if (fecha == null || !mounted) return;
    final hora = await showTimePicker(context: context, initialTime: TimeOfDay.fromDateTime(actual));
    if (hora == null) return;
    final nuevo = DateTime(fecha.year, fecha.month, fecha.day, hora.hour, hora.minute);
    setState(() => inicio ? _inicio = nuevo : _fin = nuevo);
  }

  String _texto(DateTime d) {
    String dos(int n) => n.toString().padLeft(2, '0');
    return '${dos(d.day)}/${dos(d.month)}/${d.year} ${dos(d.hour)}:${dos(d.minute)}';
  }

  Future<void> _guardar() async {
    final t = AppLocalizations.of(context);
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(informesRepositorioProvider).registrarHoras(widget.linea.asignacionId, _inicio, _fin);
      if (mounted) {
        mostrarMensaje(context, t.horasGuardadas);
        Navigator.pop(context, true);
      }
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
      title: Text('${t.registrarHoras} · ${widget.linea.profesional}'),
      content: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text('${widget.linea.turno} · ${formatearFecha(widget.linea.fecha)}'),
        const SizedBox(height: 12),
        OutlinedButton(onPressed: () => _elegir(inicio: true), child: Text('${t.horaInicioReal}: ${_texto(_inicio)}')),
        const SizedBox(height: 8),
        OutlinedButton(onPressed: () => _elegir(inicio: false), child: Text('${t.horaFinReal}: ${_texto(_fin)}')),
        if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
      ]),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context, false), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _guardar, child: Text(t.botonGuardar)),
      ],
    );
  }
}

class _DialogoAjuste extends ConsumerStatefulWidget {
  const _DialogoAjuste({required this.linea});
  final LineaHorasDto linea;

  @override
  ConsumerState<_DialogoAjuste> createState() => _EstadoDialogoAjuste();
}

class _EstadoDialogoAjuste extends ConsumerState<_DialogoAjuste> {
  final _minutos = TextEditingController();
  final _motivo = TextEditingController();
  String? _error;
  bool _enviando = false;

  @override
  void dispose() {
    _minutos.dispose();
    _motivo.dispose();
    super.dispose();
  }

  Future<void> _guardar() async {
    final t = AppLocalizations.of(context);
    final m = int.tryParse(_minutos.text.trim().replaceAll('−', '-'));
    if (m == null || m == 0 || _motivo.text.trim().length < 10) {
      setState(() => _error = t.ajusteInvalido);
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(informesRepositorioProvider).ajustarHoras(widget.linea.asignacionId, m, _motivo.text.trim());
      if (mounted) {
        mostrarMensaje(context, t.ajusteGuardado);
        Navigator.pop(context, true);
      }
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
      title: Text('${t.ajustarHoras} · ${widget.linea.profesional}'),
      content: SizedBox(
        width: 420,
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          TextField(controller: _minutos, keyboardType: const TextInputType.numberWithOptions(signed: true), decoration: InputDecoration(labelText: t.ajusteMinutos)),
          TextField(controller: _motivo, maxLength: 300, maxLines: 2, decoration: InputDecoration(labelText: t.ajusteMotivo)),
          if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
        ]),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context, false), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _guardar, child: Text(t.botonGuardar)),
      ],
    );
  }
}
