import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../estructura/data/estructura_repositorio.dart';
import '../data/planificacion_repositorio.dart';
import 'hoja_turno.dart';
import 'tarjeta_turno.dart';

enum VistaCalendario { dia, semana, mes, agenda }

DateTime soloFecha(DateTime d) => DateTime(d.year, d.month, d.day);
DateTime lunesDe(DateTime d) => soloFecha(d).subtract(Duration(days: d.weekday - DateTime.monday));

/// Calendario de planificación (día, semana, mes y agenda). En pantallas estrechas la cuadrícula semanal
/// se muestra como agenda vertical y se propone la agenda por defecto.
class PaginaPlanificacion extends ConsumerStatefulWidget {
  const PaginaPlanificacion({super.key, this.cuadranteId, this.fechaInicial, this.propia = false});

  final String? cuadranteId;
  final DateTime? fechaInicial;

  /// true = «mi planificación» (solo los turnos del profesional conectado).
  final bool propia;

  @override
  ConsumerState<PaginaPlanificacion> createState() => _EstadoPaginaPlanificacion();
}

class _EstadoPaginaPlanificacion extends ConsumerState<PaginaPlanificacion> {
  VistaCalendario? _vista;
  late DateTime _ancla = soloFecha(widget.fechaInicial ?? DateTime.now());
  String? _centroId;
  String? _servicioId;
  bool _soloSinCubrir = false;

  static const _estrecho = 600.0;

  (DateTime, DateTime) _rango(VistaCalendario v) => switch (v) {
        VistaCalendario.dia => (_ancla, _ancla),
        VistaCalendario.semana => (lunesDe(_ancla), lunesDe(_ancla).add(const Duration(days: 6))),
        VistaCalendario.mes => (
            lunesDe(DateTime(_ancla.year, _ancla.month, 1)),
            lunesDe(DateTime(_ancla.year, _ancla.month + 1, 0)).add(const Duration(days: 6)),
          ),
        VistaCalendario.agenda => (_ancla, _ancla.add(const Duration(days: 13))),
      };

  void _mover(int sentido, VistaCalendario v) => setState(() {
        _ancla = switch (v) {
          VistaCalendario.dia => _ancla.add(Duration(days: sentido)),
          VistaCalendario.semana => _ancla.add(Duration(days: 7 * sentido)),
          VistaCalendario.mes => DateTime(_ancla.year, _ancla.month + sentido, 1),
          VistaCalendario.agenda => _ancla.add(Duration(days: 14 * sentido)),
        };
      });

  String _titulo(VistaCalendario v, (DateTime, DateTime) r) {
    final largo = DateFormat('EEEE d MMMM yyyy', 'es_ES');
    final corto = DateFormat('d MMM', 'es_ES');
    return switch (v) {
      VistaCalendario.dia => toBeginningOfSentenceCase(largo.format(_ancla)),
      VistaCalendario.mes => toBeginningOfSentenceCase(DateFormat('MMMM yyyy', 'es_ES').format(_ancla)),
      _ => '${corto.format(r.$1)} – ${corto.format(r.$2)} ${r.$2.year}',
    };
  }

  @override
  Widget build(BuildContext context) {
    final ancho = MediaQuery.sizeOf(context).width;
    final vista = _vista ?? (ancho < _estrecho ? VistaCalendario.agenda : VistaCalendario.semana);
    final rango = _rango(vista);
    final perfil = ref.watch(authProvider).perfil;
    final puedeGestionar = perfil != null && (perfil.tieneRol(Rol.administradorSociedad) || perfil.tieneRol(Rol.coordinadorMedico));

    final filtro = FiltroCalendario(
      desde: rango.$1,
      hasta: rango.$2,
      centroId: _centroId,
      servicioId: _servicioId,
      cuadranteId: widget.cuadranteId,
      soloSinCubrir: _soloSinCubrir,
      propia: widget.propia,
    );
    final turnos = ref.watch(turnosProvider(filtro));

    void abrirTurno(TurnoDto turno) => showDialog<void>(
        context: context, builder: (_) => HojaTurno(turnoId: turno.id, filtro: filtro, puedeGestionar: puedeGestionar && !widget.propia));
    void irADia(DateTime d) => setState(() {
          _ancla = d;
          _vista = VistaCalendario.dia;
        });

    return Column(children: [
      _Cabecera(
        titulo: _titulo(vista, rango),
        vista: vista,
        alCambiarVista: (v) => setState(() => _vista = v),
        alMover: (s) => _mover(s, vista),
        alHoy: () => setState(() => _ancla = soloFecha(DateTime.now())),
        ancho: ancho,
      ),
      if (!widget.propia)
        _Filtros(
          centroId: _centroId,
          servicioId: _servicioId,
          soloSinCubrir: _soloSinCubrir,
          alCambiar: (c, s, v) => setState(() {
            _centroId = c;
            _servicioId = s;
            _soloSinCubrir = v;
          }),
        ),
      Expanded(
        child: EstadoAsincrono(
          valor: turnos,
          alReintentar: () => ref.invalidate(turnosProvider(filtro)),
          constructor: (lista) => switch (vista) {
            VistaCalendario.mes => _VistaMes(ancla: _ancla, rango: rango, turnos: lista, alPulsarDia: irADia),
            VistaCalendario.semana when ancho >= _estrecho =>
              _VistaSemana(lunes: rango.$1, turnos: lista, alPulsarTurno: abrirTurno, alPulsarDia: irADia),
            VistaCalendario.dia => _VistaAgenda(desde: rango.$1, hasta: rango.$2, turnos: lista, alPulsarTurno: abrirTurno, mostrarDiasVacios: true),
            // Semana en pantalla estrecha y agenda: lista vertical por días
            _ => _VistaAgenda(
                desde: rango.$1, hasta: rango.$2, turnos: lista, alPulsarTurno: abrirTurno, mostrarDiasVacios: vista == VistaCalendario.semana),
          },
        ),
      ),
    ]);
  }
}

class _Cabecera extends StatelessWidget {
  const _Cabecera({required this.titulo, required this.vista, required this.alCambiarVista, required this.alMover, required this.alHoy, required this.ancho});

  final String titulo;
  final VistaCalendario vista;
  final ValueChanged<VistaCalendario> alCambiarVista;
  final ValueChanged<int> alMover;
  final VoidCallback alHoy;
  final double ancho;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final etiquetas = {
      VistaCalendario.dia: t.vistaDia,
      VistaCalendario.semana: t.vistaSemana,
      VistaCalendario.mes: t.vistaMes,
      VistaCalendario.agenda: t.vistaAgenda,
    };
    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
      child: Column(children: [
        Row(children: [
          IconButton(tooltip: t.anterior, icon: const Icon(Icons.chevron_left), onPressed: () => alMover(-1)),
          Expanded(child: Text(titulo, textAlign: TextAlign.center, style: Theme.of(context).textTheme.titleMedium, overflow: TextOverflow.ellipsis)),
          IconButton(tooltip: t.siguiente, icon: const Icon(Icons.chevron_right), onPressed: () => alMover(1)),
          TextButton(onPressed: alHoy, child: Text(t.hoy)),
        ]),
        Wrap(spacing: 8, children: [
          for (final e in etiquetas.entries)
            ChoiceChip(label: Text(e.value), selected: vista == e.key, onSelected: (_) => alCambiarVista(e.key)),
        ]),
      ]),
    );
  }
}

class _Filtros extends ConsumerWidget {
  const _Filtros({required this.centroId, required this.servicioId, required this.soloSinCubrir, required this.alCambiar});

  final String? centroId;
  final String? servicioId;
  final bool soloSinCubrir;
  final void Function(String? centro, String? servicio, bool soloSinCubrir) alCambiar;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final centros = ref.watch(centrosProvider).value ?? const <CentroDto>[];
    final servicios = (ref.watch(serviciosActivosProvider).value ?? const <ServicioDto>[]).where((s) => centroId == null || s.centroId == centroId).toList();

    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 4, 12, 0),
      child: Wrap(spacing: 8, runSpacing: 4, crossAxisAlignment: WrapCrossAlignment.center, children: [
        SizedBox(
          width: 190,
          child: DropdownButtonFormField<String?>(
            key: ValueKey('centro-$centroId'),
            initialValue: centros.any((c) => c.id == centroId) ? centroId : null,
            isExpanded: true,
            decoration: InputDecoration(labelText: t.filtroCentro, isDense: true),
            items: [
              DropdownMenuItem(value: null, child: Text(t.todos)),
              for (final c in centros) DropdownMenuItem(value: c.id, child: Text(c.nombre, overflow: TextOverflow.ellipsis)),
            ],
            onChanged: (v) => alCambiar(v, null, soloSinCubrir),
          ),
        ),
        SizedBox(
          width: 190,
          child: DropdownButtonFormField<String?>(
            key: ValueKey('servicio-$servicioId-$centroId'),
            initialValue: servicios.any((s) => s.id == servicioId) ? servicioId : null,
            isExpanded: true,
            decoration: InputDecoration(labelText: t.filtroServicio, isDense: true),
            items: [
              DropdownMenuItem(value: null, child: Text(t.todos)),
              for (final s in servicios) DropdownMenuItem(value: s.id, child: Text(s.nombre, overflow: TextOverflow.ellipsis)),
            ],
            onChanged: (v) => alCambiar(centroId, v, soloSinCubrir),
          ),
        ),
        FilterChip(label: Text(t.soloSinCubrir), selected: soloSinCubrir, onSelected: (v) => alCambiar(centroId, servicioId, v)),
      ]),
    );
  }
}

String _nombreDia(DateTime d) => toBeginningOfSentenceCase(DateFormat('EEEE d MMMM', 'es_ES').format(d));

/// Lista vertical agrupada por día (agenda, día y semana en móvil).
class _VistaAgenda extends StatelessWidget {
  const _VistaAgenda({required this.desde, required this.hasta, required this.turnos, required this.alPulsarTurno, required this.mostrarDiasVacios});

  final DateTime desde;
  final DateTime hasta;
  final List<TurnoDto> turnos;
  final ValueChanged<TurnoDto> alPulsarTurno;
  final bool mostrarDiasVacios;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final dias = <DateTime>[for (var d = desde; !d.isAfter(hasta); d = d.add(const Duration(days: 1))) d];
    final porDia = {for (final d in dias) d: turnos.where((x) => soloFecha(x.dia) == d).toList()};
    final visibles = dias.where((d) => mostrarDiasVacios || porDia[d]!.isNotEmpty).toList();

    if (visibles.isEmpty) return Center(child: Text(t.sinTurnos));
    return ContenidoCentrado(
      child: ListView(padding: const EdgeInsets.only(bottom: 24), children: [
        for (final d in visibles) ...[
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
            child: Text(_nombreDia(d),
                style: Theme.of(context).textTheme.titleSmall?.copyWith(color: soloFecha(DateTime.now()) == d ? Theme.of(context).colorScheme.primary : null)),
          ),
          if (porDia[d]!.isEmpty)
            Padding(padding: const EdgeInsets.symmetric(horizontal: 16), child: Text(t.sinTurnos, style: Theme.of(context).textTheme.bodySmall)),
          for (final turno in porDia[d]!) TarjetaTurno(turno: turno, alPulsar: () => alPulsarTurno(turno), mostrarNombres: true),
        ],
      ]),
    );
  }
}

/// Cuadrícula semanal de 7 columnas (tablet y escritorio).
class _VistaSemana extends StatelessWidget {
  const _VistaSemana({required this.lunes, required this.turnos, required this.alPulsarTurno, required this.alPulsarDia});

  final DateTime lunes;
  final List<TurnoDto> turnos;
  final ValueChanged<TurnoDto> alPulsarTurno;
  final ValueChanged<DateTime> alPulsarDia;

  @override
  Widget build(BuildContext context) {
    final hoy = soloFecha(DateTime.now());
    return SingleChildScrollView(
      padding: const EdgeInsets.all(8),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        for (var i = 0; i < 7; i++)
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 2),
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                InkWell(
                  onTap: () => alPulsarDia(lunes.add(Duration(days: i))),
                  child: Container(
                    padding: const EdgeInsets.symmetric(vertical: 6),
                    decoration: BoxDecoration(
                      color: lunes.add(Duration(days: i)) == hoy ? Theme.of(context).colorScheme.primaryContainer : Theme.of(context).colorScheme.surfaceContainerHighest,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(toBeginningOfSentenceCase(DateFormat('EEE d', 'es_ES').format(lunes.add(Duration(days: i)))), textAlign: TextAlign.center),
                  ),
                ),
                for (final turno in turnos.where((x) => soloFecha(x.dia) == lunes.add(Duration(days: i))))
                  TarjetaTurno(turno: turno, alPulsar: () => alPulsarTurno(turno), compacta: true),
              ]),
            ),
          ),
      ]),
    );
  }
}

/// Vista mensual: una celda por día con el número de turnos y las plazas sin cubrir.
class _VistaMes extends StatelessWidget {
  const _VistaMes({required this.ancla, required this.rango, required this.turnos, required this.alPulsarDia});

  final DateTime ancla;
  final (DateTime, DateTime) rango;
  final List<TurnoDto> turnos;
  final ValueChanged<DateTime> alPulsarDia;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final esquema = Theme.of(context).colorScheme;
    final hoy = soloFecha(DateTime.now());
    final semanas = (rango.$2.difference(rango.$1).inDays + 1) ~/ 7;
    final iniciales = ['L', 'M', 'X', 'J', 'V', 'S', 'D'];

    return SingleChildScrollView(
      padding: const EdgeInsets.all(8),
      child: Column(children: [
        Row(children: [for (final i in iniciales) Expanded(child: Center(child: Padding(padding: const EdgeInsets.all(4), child: Text(i, style: Theme.of(context).textTheme.labelMedium))))]),
        for (var s = 0; s < semanas; s++)
          Row(children: [
            for (var i = 0; i < 7; i++)
              Builder(builder: (context) {
                final dia = rango.$1.add(Duration(days: s * 7 + i));
                final delDia = turnos.where((x) => soloFecha(x.dia) == dia).toList();
                final sinCubrir = delDia.where((x) => x.sinCubrir).length;
                final delMes = dia.month == ancla.month;
                return Expanded(
                  child: Semantics(
                    label: '${DateFormat('d MMMM', 'es_ES').format(dia)}: ${t.resumenDia(delDia.length, sinCubrir)}',
                    button: true,
                    child: InkWell(
                      onTap: () => alPulsarDia(dia),
                      child: Container(
                        height: 72,
                        margin: const EdgeInsets.all(1),
                        padding: const EdgeInsets.all(4),
                        decoration: BoxDecoration(
                          color: dia == hoy ? esquema.primaryContainer : esquema.surfaceContainerLow,
                          borderRadius: BorderRadius.circular(6),
                          border: sinCubrir > 0 ? Border.all(color: esquema.error.withValues(alpha: 0.6)) : null,
                        ),
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text('${dia.day}', style: TextStyle(fontWeight: FontWeight.w600, color: delMes ? null : esquema.outline)),
                          const Spacer(),
                          if (delDia.isNotEmpty)
                            Text('${delDia.length}', style: Theme.of(context).textTheme.labelSmall),
                          if (sinCubrir > 0) Text('!$sinCubrir', style: TextStyle(color: esquema.error, fontSize: 11, fontWeight: FontWeight.bold)),
                        ]),
                      ),
                    ),
                  ),
                );
              }),
          ]),
      ]),
    );
  }
}
