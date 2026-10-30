import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../core/tema/tema.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../ausencias/data/ausencias_repositorio.dart';
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
    // En «Mi planificación» también se ven los días que el profesional ofreció y el coordinador aprobó (no son turnos asignados).
    final ofrecidas = widget.propia ? ofrecidasEnRango(ref.watch(miDisponibilidadProvider).value ?? const [], rango.$1, rango.$2) : const <DiaOfrecido>[];

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
            VistaCalendario.mes => _VistaMes(ancla: _ancla, rango: rango, turnos: lista, ofrecidas: ofrecidas, alPulsarDia: irADia),
            VistaCalendario.semana when ancho >= _estrecho =>
              _VistaSemana(lunes: rango.$1, turnos: lista, ofrecidas: ofrecidas, alPulsarTurno: abrirTurno, alPulsarDia: irADia),
            VistaCalendario.dia => _VistaAgenda(desde: rango.$1, hasta: rango.$2, turnos: lista, ofrecidas: ofrecidas, alPulsarTurno: abrirTurno, mostrarDiasVacios: true),
            // Semana en pantalla estrecha y agenda: lista vertical por días
            _ => _VistaAgenda(
                desde: rango.$1, hasta: rango.$2, turnos: lista, ofrecidas: ofrecidas, alPulsarTurno: abrirTurno, mostrarDiasVacios: vista == VistaCalendario.semana),
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
    final navegacion = Row(mainAxisSize: MainAxisSize.min, children: [
      IconButton.outlined(tooltip: t.anterior, icon: const Icon(Icons.chevron_left), onPressed: () => alMover(-1)),
      const SizedBox(width: 6),
      IconButton.outlined(tooltip: t.siguiente, icon: const Icon(Icons.chevron_right), onPressed: () => alMover(1)),
      const SizedBox(width: 12),
      OutlinedButton(onPressed: alHoy, child: Text(t.hoy)),
    ]);
    final tituloW = Text(titulo,
        style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800, letterSpacing: -0.3), overflow: TextOverflow.ellipsis);
    final selector = SegmentedButton<VistaCalendario>(
      showSelectedIcon: false,
      style: SegmentedButton.styleFrom(visualDensity: VisualDensity.compact),
      segments: [for (final e in etiquetas.entries) ButtonSegment(value: e.key, label: Text(e.value))],
      selected: {vista},
      onSelectionChanged: (v) => alCambiarVista(v.first),
    );
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 8),
      child: ancho < 600
          ? Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              Row(children: [Expanded(child: tituloW), navegacion]),
              const SizedBox(height: 14),
              selector,
            ])
          : Row(children: [
              Expanded(child: tituloW),
              navegacion,
              const SizedBox(width: 20),
              selector,
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
      padding: const EdgeInsets.fromLTRB(20, 4, 20, 8),
      child: Wrap(spacing: 12, runSpacing: 10, crossAxisAlignment: WrapCrossAlignment.center, children: [
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
  const _VistaAgenda({required this.desde, required this.hasta, required this.turnos, required this.ofrecidas, required this.alPulsarTurno, required this.mostrarDiasVacios});

  final DateTime desde;
  final DateTime hasta;
  final List<TurnoDto> turnos;
  final List<DiaOfrecido> ofrecidas;
  final ValueChanged<TurnoDto> alPulsarTurno;
  final bool mostrarDiasVacios;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final dias = <DateTime>[for (var d = desde; !d.isAfter(hasta); d = d.add(const Duration(days: 1))) d];
    final porDia = {for (final d in dias) d: turnos.where((x) => soloFecha(x.dia) == d).toList()};
    final visibles = dias.where((d) => mostrarDiasVacios || porDia[d]!.isNotEmpty || ofrecidas.any((o) => o.dia == d)).toList();

    if (visibles.isEmpty) return EstadoVacio(mensaje: t.sinTurnos, icono: Icons.event_busy_outlined);
    final cs = Theme.of(context).colorScheme;
    final hoy = soloFecha(DateTime.now());
    return ContenidoCentrado(
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
        itemCount: visibles.length,
        separatorBuilder: (_, _) => const SizedBox(height: 20),
        itemBuilder: (context, idx) {
          final d = visibles[idx];
          final esHoy = d == hoy;
          final vacio = porDia[d]!.isEmpty && !ofrecidas.any((o) => o.dia == d);
          return Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            SizedBox(
              width: 56,
              child: Container(
                padding: const EdgeInsets.symmetric(vertical: 8),
                decoration: BoxDecoration(
                  gradient: esHoy ? TemaApp.degradadoMarca : null,
                  color: esHoy ? null : cs.surfaceContainerLow,
                  borderRadius: BorderRadius.circular(16),
                ),
                child: Column(children: [
                  Text(DateFormat('EEE', 'es_ES').format(d).toUpperCase().replaceAll('.', ''),
                      style: Theme.of(context).textTheme.labelSmall?.copyWith(
                          letterSpacing: 0.8, fontWeight: FontWeight.w700, color: esHoy ? Colors.white70 : cs.onSurfaceVariant)),
                  Text('${d.day}',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800, color: esHoy ? Colors.white : null)),
                ]),
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                Padding(
                  padding: const EdgeInsets.only(top: 2, bottom: 8),
                  child: Text(_nombreDia(d), style: Theme.of(context).textTheme.labelLarge?.copyWith(color: esHoy ? cs.primary : cs.onSurfaceVariant)),
                ),
                for (final o in ofrecidas.where((o) => o.dia == d)) _TarjetaOfrecida(o),
                if (vacio) Text(t.sinTurnos, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.outline)),
                for (final turno in porDia[d]!) TarjetaTurno(turno: turno, alPulsar: () => alPulsarTurno(turno), mostrarNombres: true),
              ]),
            ),
          ]);
        },
      ),
    );
  }
}

/// Cuadrícula semanal de 7 columnas (tablet y escritorio).
class _VistaSemana extends StatelessWidget {
  const _VistaSemana({required this.lunes, required this.turnos, required this.ofrecidas, required this.alPulsarTurno, required this.alPulsarDia});

  final DateTime lunes;
  final List<TurnoDto> turnos;
  final List<DiaOfrecido> ofrecidas;
  final ValueChanged<TurnoDto> alPulsarTurno;
  final ValueChanged<DateTime> alPulsarDia;

  @override
  Widget build(BuildContext context) {
    final hoy = soloFecha(DateTime.now());
    final cs = Theme.of(context).colorScheme;
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        for (var i = 0; i < 7; i++)
          Builder(builder: (context) {
            final dia = lunes.add(Duration(days: i));
            final esHoy = dia == hoy;
            return Expanded(
              child: Container(
                margin: const EdgeInsets.symmetric(horizontal: 4),
                padding: const EdgeInsets.all(8),
                constraints: const BoxConstraints(minHeight: 280),
                decoration: BoxDecoration(
                  color: esHoy ? cs.primary.withValues(alpha: 0.06) : cs.surfaceContainerLow,
                  borderRadius: BorderRadius.circular(18),
                  border: Border.all(color: esHoy ? cs.primary.withValues(alpha: 0.4) : Colors.transparent),
                ),
                child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                  InkWell(
                    onTap: () => alPulsarDia(dia),
                    borderRadius: BorderRadius.circular(12),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(vertical: 8),
                      child: Column(children: [
                        Text(DateFormat('EEE', 'es_ES').format(dia).toUpperCase().replaceAll('.', ''),
                            style: Theme.of(context).textTheme.labelSmall?.copyWith(letterSpacing: 0.8, fontWeight: FontWeight.w700, color: cs.onSurfaceVariant)),
                        const SizedBox(height: 4),
                        Container(
                          width: 34,
                          height: 34,
                          alignment: Alignment.center,
                          decoration: BoxDecoration(gradient: esHoy ? TemaApp.degradadoMarca : null, shape: BoxShape.circle),
                          child: Text('${dia.day}', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15, color: esHoy ? Colors.white : null)),
                        ),
                      ]),
                    ),
                  ),
                  const SizedBox(height: 4),
                  for (final o in ofrecidas.where((o) => o.dia == dia)) _TarjetaOfrecida(o, compacta: true),
                  for (final turno in turnos.where((x) => soloFecha(x.dia) == dia))
                    TarjetaTurno(turno: turno, alPulsar: () => alPulsarTurno(turno), compacta: true),
                ]),
              ),
            );
          }),
      ]),
    );
  }
}

/// Vista mensual: una celda por día con el número de turnos y las plazas sin cubrir.
class _VistaMes extends StatelessWidget {
  const _VistaMes({required this.ancla, required this.rango, required this.turnos, required this.ofrecidas, required this.alPulsarDia});

  final DateTime ancla;
  final (DateTime, DateTime) rango;
  final List<TurnoDto> turnos;
  final List<DiaOfrecido> ofrecidas;
  final ValueChanged<DateTime> alPulsarDia;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final esquema = Theme.of(context).colorScheme;
    final hoy = soloFecha(DateTime.now());
    final semanas = (rango.$2.difference(rango.$1).inDays + 1) ~/ 7;
    final iniciales = ['L', 'M', 'X', 'J', 'V', 'S', 'D'];

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
      child: Column(children: [
        Row(children: [
          for (final i in iniciales)
            Expanded(
                child: Center(
                    child: Padding(
                        padding: const EdgeInsets.symmetric(vertical: 8),
                        child: Text(i, style: Theme.of(context).textTheme.labelMedium?.copyWith(fontWeight: FontWeight.w700, color: esquema.onSurfaceVariant))))),
        ]),
        for (var s = 0; s < semanas; s++)
          Row(children: [
            for (var i = 0; i < 7; i++)
              Builder(builder: (context) {
                final dia = rango.$1.add(Duration(days: s * 7 + i));
                final delDia = turnos.where((x) => soloFecha(x.dia) == dia).toList();
                final sinCubrir = delDia.where((x) => x.sinCubrir).length;
                final ofrecidasDia = ofrecidas.where((o) => o.dia == dia).length;
                final delMes = dia.month == ancla.month;
                return Expanded(
                  child: Semantics(
                    label: '${DateFormat('d MMMM', 'es_ES').format(dia)}: ${t.resumenDia(delDia.length, sinCubrir)}',
                    button: true,
                    child: InkWell(
                      onTap: () => alPulsarDia(dia),
                      borderRadius: BorderRadius.circular(16),
                      child: Container(
                        height: 84,
                        margin: const EdgeInsets.all(3),
                        padding: const EdgeInsets.all(8),
                        decoration: BoxDecoration(
                          color: dia == hoy
                              ? esquema.primary.withValues(alpha: 0.12)
                              : (delMes ? esquema.surfaceContainerLow : Colors.transparent),
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(
                              color: dia == hoy
                                  ? esquema.primary
                                  : (sinCubrir > 0 ? esquema.error.withValues(alpha: 0.6) : Colors.transparent),
                              width: dia == hoy ? 2 : 1),
                        ),
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text('${dia.day}',
                              style: TextStyle(fontWeight: FontWeight.w700, color: dia == hoy ? esquema.primary : (delMes ? null : esquema.outline))),
                          const Spacer(),
                          Row(children: [
                            if (delDia.isNotEmpty)
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                                decoration: BoxDecoration(color: esquema.primary.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(8)),
                                child: Text('${delDia.length}', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: esquema.primary)),
                              ),
                            if (ofrecidasDia > 0)
                              Padding(
                                padding: const EdgeInsets.only(left: 3),
                                child: Tooltip(message: t.dispDisponible, child: Icon(Icons.event_available_outlined, size: 14, color: esquema.tertiary)),
                              ),
                            const Spacer(),
                            if (sinCubrir > 0)
                              Text('!$sinCubrir', style: TextStyle(color: esquema.error, fontSize: 11, fontWeight: FontWeight.bold)),
                          ]),
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

// ── Disponibilidad aprobada (solo «Mi planificación») ──

/// Un día en el que el profesional ofreció disponibilidad y la coordinación la aprobó. No es un turno asignado.
class DiaOfrecido {
  const DiaOfrecido(this.dia, this.centro, this.turno);
  final DateTime dia;
  final String? centro;
  final String? turno;
}

/// Expande las declaraciones APROBADAS de tipo «Disponible» a los días concretos del rango (según su máscara de días semanales).
List<DiaOfrecido> ofrecidasEnRango(List<DisponibilidadDto> declaraciones, DateTime desde, DateTime hasta) {
  final salida = <DiaOfrecido>[];
  for (final d in declaraciones) {
    if (d.estado != EstadoDisponibilidad.aprobada || d.tipo != TipoDisponibilidad.disponible) continue;
    final inicio = soloFecha(DateTime.parse(d.desde));
    final fin = soloFecha(DateTime.parse(d.hasta));
    for (var dia = inicio.isBefore(desde) ? desde : inicio; !dia.isAfter(fin) && !dia.isAfter(hasta); dia = dia.add(const Duration(days: 1))) {
      if (d.dias & (1 << (dia.weekday - 1)) != 0) salida.add(DiaOfrecido(dia, d.centro, d.tipoTurno));
    }
  }
  // Un mismo turno ofrecido en varios servicios del hospital se declara una vez por servicio: se muestra una sola vez.
  final vistos = <String>{};
  salida.removeWhere((o) => !vistos.add('${o.dia}|${o.centro}|${o.turno}'));
  salida.sort((a, b) => a.dia.compareTo(b.dia));
  return salida;
}

class _TarjetaOfrecida extends StatelessWidget {
  const _TarjetaOfrecida(this.o, {this.compacta = false});
  final DiaOfrecido o;
  final bool compacta;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;
    final detalle = [if (o.centro != null) o.centro!, o.turno ?? t.cualquierTurno].join(' · ');
    return Container(
      margin: compacta ? const EdgeInsets.only(top: 6) : const EdgeInsets.only(bottom: 10),
      padding: EdgeInsets.symmetric(horizontal: compacta ? 8 : 14, vertical: compacta ? 8 : 12),
      decoration: BoxDecoration(
        color: cs.tertiary.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: cs.tertiary.withValues(alpha: 0.35)),
      ),
      child: Row(children: [
        Icon(Icons.event_available_outlined, size: compacta ? 14 : 18, color: cs.tertiary),
        SizedBox(width: compacta ? 4 : 10),
        Expanded(
          child: compacta
              ? Text(detalle, style: Theme.of(context).textTheme.labelSmall, maxLines: 3, overflow: TextOverflow.ellipsis)
              : Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(t.dispDisponible, style: Theme.of(context).textTheme.labelLarge?.copyWith(color: cs.tertiary, fontWeight: FontWeight.w700)),
                  Text(detalle, style: Theme.of(context).textTheme.bodySmall),
                ]),
        ),
      ]),
    );
  }
}
