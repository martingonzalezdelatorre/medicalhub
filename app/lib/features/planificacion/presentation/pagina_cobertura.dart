import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../profesionales/data/profesionales_repositorio.dart';
import '../data/planificacion_repositorio.dart';
import 'hoja_turno.dart';
import 'pagina_planificacion.dart' show soloFecha, lunesDe;
import 'tarjeta_turno.dart';

enum _Periodo { hoy, semana, mes, rango }

enum _Agrupacion { dia, hospital, profesional }

const _paletaTurnos = <Color>[
  Color(0xFF00796B),
  Color(0xFF3949AB),
  Color(0xFF7E57C2),
  Color(0xFFEF6C00),
  Color(0xFFD81B60),
  Color(0xFF00838F),
  Color(0xFF6D4C41),
  Color(0xFF546E7A),
];

/// Resumen de cobertura para administración y RR. HH.: quién cubre cada turno, agrupable por día, hospital o profesional,
/// día, intervalo de días y profesional. Se adapta de móvil a escritorio.
class PaginaCobertura extends ConsumerStatefulWidget {
  const PaginaCobertura({super.key});

  @override
  ConsumerState<PaginaCobertura> createState() => _EstadoPaginaCobertura();
}

class _EstadoPaginaCobertura extends ConsumerState<PaginaCobertura> {
  static const _maxDias = 100;

  _Periodo _periodo = _Periodo.semana;
  _Agrupacion _agrupacion = _Agrupacion.dia;
  DateTime _ancla = soloFecha(DateTime.now());
  (DateTime, DateTime)? _rangoLibre;
  String? _profesionalId;
  String? _profesionalNombre;
  bool _soloSinCubrir = false;

  (DateTime, DateTime) get _rango => switch (_periodo) {
        _Periodo.hoy => (_ancla, _ancla),
        _Periodo.semana => (lunesDe(_ancla), lunesDe(_ancla).add(const Duration(days: 6))),
        _Periodo.mes => (DateTime(_ancla.year, _ancla.month, 1), DateTime(_ancla.year, _ancla.month + 1, 0)),
        _Periodo.rango => _rangoLibre ?? (_ancla, _ancla.add(const Duration(days: 6))),
      };

  void _mover(int sentido) => setState(() {
        switch (_periodo) {
          case _Periodo.hoy:
            _ancla = _ancla.add(Duration(days: sentido));
          case _Periodo.semana:
            _ancla = _ancla.add(Duration(days: 7 * sentido));
          case _Periodo.mes:
            _ancla = DateTime(_ancla.year, _ancla.month + sentido, 1);
          case _Periodo.rango:
            final r = _rango;
            final dias = r.$2.difference(r.$1).inDays + 1;
            _rangoLibre = (r.$1.add(Duration(days: dias * sentido)), r.$2.add(Duration(days: dias * sentido)));
            _ancla = _rangoLibre!.$1;
        }
      });

  void _elegirPeriodo(_Periodo p) {
    if (p == _Periodo.hoy) _ancla = soloFecha(DateTime.now());
    if (p == _Periodo.rango) {
      _elegirRango();
      return;
    }
    setState(() => _periodo = p);
  }

  Future<void> _elegirRango() async {
    final t = AppLocalizations.of(context);
    final actual = _rango;
    final hoy = soloFecha(DateTime.now());
    final r = await showDateRangePicker(
      context: context,
      helpText: t.cobElegirRango,
      firstDate: hoy.subtract(const Duration(days: 365)),
      lastDate: hoy.add(const Duration(days: 365 * 2)),
      initialDateRange: DateTimeRange(start: actual.$1, end: actual.$2),
    );
    if (r == null || !mounted) return;
    var fin = soloFecha(r.end);
    final inicio = soloFecha(r.start);
    if (fin.difference(inicio).inDays + 1 > _maxDias) {
      fin = inicio.add(const Duration(days: _maxDias - 1));
      mostrarMensaje(context, t.cobRangoMaximo(_maxDias));
    }
    setState(() {
      _periodo = _Periodo.rango;
      _rangoLibre = (inicio, fin);
      _ancla = inicio;
    });
  }

  String _tituloRango() {
    final r = _rango;
    final largo = DateFormat('EEEE d MMMM yyyy', 'es_ES');
    final corto = DateFormat('d MMM', 'es_ES');
    if (r.$1 == r.$2) return toBeginningOfSentenceCase(largo.format(r.$1));
    if (_periodo == _Periodo.mes) return toBeginningOfSentenceCase(DateFormat('MMMM yyyy', 'es_ES').format(r.$1));
    return '${corto.format(r.$1)} – ${corto.format(r.$2)} ${r.$2.year}';
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final ancho = MediaQuery.sizeOf(context).width;
    final perfil = ref.watch(authProvider).perfil;
    final puedeGestionar = perfil != null && (perfil.tieneRol(Rol.administradorSociedad) || perfil.tieneRol(Rol.coordinadorMedico));
    final rango = _rango;
    final filtro = FiltroCalendario(desde: rango.$1, hasta: rango.$2, soloSinCubrir: _soloSinCubrir);
    final turnos = ref.watch(turnosProvider(filtro));

    return EstadoAsincrono(
      valor: turnos,
      alReintentar: () => ref.invalidate(turnosProvider(filtro)),
      constructor: (todos) {
        final idProfesional = _profesionalId;
        final lista = idProfesional == null
            ? todos
            : todos.where((x) => x.necesidades.any((n) => n.asignaciones.any((a) => a.profesionalId == idProfesional))).toList();
        final colores = _coloresTurno(todos);
        void abrir(TurnoDto turno) => showDialog<void>(
            context: context, builder: (_) => HojaTurno(turnoId: turno.id, filtro: filtro, puedeGestionar: puedeGestionar));

        return CustomScrollView(slivers: [
          SliverToBoxAdapter(
            child: _Encabezado(
              titulo: _tituloRango(),
              periodo: _periodo,
              alPeriodo: _elegirPeriodo,
              alMover: _mover,
              profesionalId: idProfesional,
              profesionalNombre: _profesionalNombre,
              alProfesional: (p) => setState(() {
                _profesionalId = p?.id;
                _profesionalNombre = p?.nombreCompleto;
              }),
              agrupacion: _agrupacion,
              alAgrupacion: (v) => setState(() => _agrupacion = v),
              hayFiltros: idProfesional != null || _soloSinCubrir,
              alLimpiar: () => setState(() {
                _profesionalId = null;
                _profesionalNombre = null;
                _soloSinCubrir = false;
              }),
              ancho: ancho,
            ),
          ),
          SliverToBoxAdapter(
            child: _Resumen(
              turnos: lista,
              ancho: ancho,
              soloSinCubrir: _soloSinCubrir,
              alSoloSinCubrir: (v) => setState(() => _soloSinCubrir = v),
            ),
          ),
          if (lista.isEmpty)
            SliverFillRemaining(hasScrollBody: false, child: EstadoVacio(mensaje: t.cobSinResultados, icono: Icons.event_busy_outlined))
          else
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(12, 0, 12, 32),
                child: switch (_agrupacion) {
                  _Agrupacion.dia => _PorDia(turnos: lista, rango: rango, resaltado: idProfesional, colores: colores, alPulsar: abrir),
                  _Agrupacion.hospital => _PorHospital(turnos: lista, resaltado: idProfesional, colores: colores, alPulsar: abrir),
                  _Agrupacion.profesional =>
                    _PorProfesional(turnos: lista, rango: rango, profesionalId: idProfesional, colores: colores, alPulsar: abrir),
                },
              ),
            ),
        ]);
      },
    );
  }
}

// ── Utilidades ──

Map<String, String> _profesionalesDe(List<TurnoDto> turnos) {
  final m = <String, String>{};
  for (final t in turnos) {
    for (final n in t.necesidades) {
      for (final a in n.asignaciones) {
        m[a.profesionalId] = a.profesional;
      }
    }
  }
  final orden = m.entries.toList()..sort((a, b) => a.value.toLowerCase().compareTo(b.value.toLowerCase()));
  return {for (final e in orden) e.key: e.value};
}

Map<String, Color> _coloresTurno(List<TurnoDto> turnos) {
  final tipos = {for (final t in turnos) t.tipoTurno}.toList()..sort();
  return {for (var i = 0; i < tipos.length; i++) tipos[i]: _paletaTurnos[i % _paletaTurnos.length]};
}

List<AsignacionDto> _asignados(TurnoDto t) => [for (final n in t.necesidades) ...n.asignaciones];

/// Turnos de un día: primero los que tienen huecos o sobran (los que requieren acción) y, dentro de cada bloque, por hora de inicio.
List<TurnoDto> _ordenarPorUrgencia(Iterable<TurnoDto> turnos) {
  int rango(TurnoDto x) => (x.excedido || x.plazasCubiertas < x.plazasTotales) ? 0 : 1;
  return turnos.toList()..sort((a, b) => rango(a) != rango(b) ? rango(a).compareTo(rango(b)) : a.inicioLocal.compareTo(b.inicioLocal));
}

/// Icono que acompaña al semáforo para no depender solo del color.
IconData _iconoEstado(int cubiertas, int total, {int excedidas = 0}) {
  if (total == 0) return Icons.remove_circle_outline;
  if (excedidas > 0) return Icons.error_outline;
  if (cubiertas >= total) return Icons.check_circle_outline;
  return Icons.warning_amber_rounded;
}

/// Pastilla «icono + asignados/plazas» con el color del semáforo.
class _PastillaEstado extends StatelessWidget {
  const _PastillaEstado({required this.texto, required this.color, required this.icono});

  final String texto;
  final Color color;
  final IconData icono;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(12)),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        Icon(icono, size: 13, color: Colors.white),
        const SizedBox(width: 3),
        Text(texto, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 12)),
      ]),
    );
  }
}

/// Una barrita por plaza: rellena si está cubierta. Con muchas plazas se degrada a una barra continua.
class _BarraPlazas extends StatelessWidget {
  const _BarraPlazas({required this.cubiertas, required this.total, required this.color});

  final int cubiertas;
  final int total;
  final Color color;

  @override
  Widget build(BuildContext context) {
    if (total > 12 || total == 0) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(3),
        child: LinearProgressIndicator(value: total == 0 ? 1 : (cubiertas / total).clamp(0, 1).toDouble(), minHeight: 5, color: color, backgroundColor: color.withValues(alpha: 0.15)),
      );
    }
    return Row(children: [
      for (var i = 0; i < total; i++)
        Expanded(
          child: Container(
            height: 5,
            margin: EdgeInsets.only(right: i == total - 1 ? 0 : 3),
            decoration: BoxDecoration(color: i < cubiertas ? color : color.withValues(alpha: 0.18), borderRadius: BorderRadius.circular(3)),
          ),
        ),
    ]);
  }
}

String _etiquetaDia(DateTime d) => toBeginningOfSentenceCase(DateFormat('EEEE d MMMM', 'es_ES').format(d));

String _inicial(String nombre) => nombre.trim().isEmpty ? '?' : nombre.trim()[0].toUpperCase();

/// Semáforo: verde si llega justo al tope, amarillo si faltan profesionales y rojo si hay más de los permitidos.
Color _colorProgreso(BuildContext context, int cubiertas, int total, {int excedidas = 0}) {
  final cs = Theme.of(context).colorScheme;
  if (total == 0) return cs.outline;
  if (excedidas > 0) return cs.error;
  if (cubiertas >= total) return Colors.green.shade600;
  return Colors.amber.shade700;
}

/// Reparte los hijos en 1, 2 o 3 columnas según el ancho disponible.
class _Rejilla extends StatelessWidget {
  const _Rejilla({required this.hijos, this.maxColumnas = 4, this.anchoMinimo = 260, this.espacio = 10});

  final List<Widget> hijos;
  final int maxColumnas;
  final double anchoMinimo;
  final double espacio;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(builder: (context, c) {
      final columnas = (c.maxWidth / anchoMinimo).floor().clamp(1, maxColumnas);
      final ancho = (c.maxWidth - espacio * (columnas - 1)) / columnas;
      return Wrap(spacing: espacio, runSpacing: espacio, children: [for (final h in hijos) SizedBox(width: ancho, child: h)]);
    });
  }
}

// ── Encabezado y filtros ──

/// Una sola barra con periodo, navegación, agrupación y buscador; en escritorio todo cabe en dos filas como mucho.
class _Encabezado extends StatelessWidget {
  const _Encabezado({
    required this.titulo,
    required this.periodo,
    required this.alPeriodo,
    required this.alMover,
    required this.profesionalId,
    required this.profesionalNombre,
    required this.alProfesional,
    required this.agrupacion,
    required this.alAgrupacion,
    required this.hayFiltros,
    required this.alLimpiar,
    required this.ancho,
  });

  final String titulo;
  final _Periodo periodo;
  final ValueChanged<_Periodo> alPeriodo;
  final ValueChanged<int> alMover;
  final String? profesionalId;
  final String? profesionalNombre;
  final ValueChanged<ProfesionalDto?> alProfesional;
  final _Agrupacion agrupacion;
  final ValueChanged<_Agrupacion> alAgrupacion;
  final bool hayFiltros;
  final VoidCallback alLimpiar;
  final double ancho;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;
    final etiquetas = {
      _Periodo.hoy: t.hoy,
      _Periodo.semana: t.vistaSemana,
      _Periodo.mes: t.vistaMes,
      _Periodo.rango: t.cobRango,
    };
    final estrecho = ancho < 600;
    final ancha = ancho >= 1000;

    final navegacion = Row(mainAxisSize: MainAxisSize.min, children: [
      IconButton.filledTonal(tooltip: t.anterior, visualDensity: VisualDensity.compact, icon: const Icon(Icons.chevron_left), onPressed: () => alMover(-1)),
      Flexible(
        child: ConstrainedBox(
          constraints: BoxConstraints(minWidth: estrecho ? 0 : 190),
          child: Text(titulo,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
              overflow: TextOverflow.ellipsis),
        ),
      ),
      IconButton.filledTonal(tooltip: t.siguiente, visualDensity: VisualDensity.compact, icon: const Icon(Icons.chevron_right), onPressed: () => alMover(1)),
    ]);

    final periodos = Wrap(spacing: 6, runSpacing: 4, children: [
      for (final e in etiquetas.entries)
        ChoiceChip(
          avatar: e.key == _Periodo.rango ? Icon(Icons.date_range_outlined, size: 16, color: periodo == e.key ? cs.onPrimary : cs.onSurface) : null,
          label: Text(e.value),
          visualDensity: VisualDensity.compact,
          selected: periodo == e.key,
          showCheckmark: false,
          selectedColor: cs.primary,
          labelStyle: TextStyle(fontWeight: FontWeight.w600, color: periodo == e.key ? cs.onPrimary : cs.onSurface),
          side: BorderSide(color: periodo == e.key ? cs.primary : cs.outlineVariant),
          onSelected: (_) => alPeriodo(e.key),
        ),
    ]);

    final agrupar = SegmentedButton<_Agrupacion>(
      showSelectedIcon: false,
      style: const ButtonStyle(visualDensity: VisualDensity.compact),
      segments: [
        ButtonSegment(value: _Agrupacion.dia, icon: estrecho ? null : const Icon(Icons.today_outlined, size: 18), label: Text(t.cobPorDia)),
        ButtonSegment(value: _Agrupacion.hospital, icon: estrecho ? null : const Icon(Icons.local_hospital_outlined, size: 18), label: Text(t.cobPorHospital)),
        ButtonSegment(value: _Agrupacion.profesional, icon: estrecho ? null : const Icon(Icons.person_outline, size: 18), label: Text(t.cobPorProfesional)),
      ],
      selected: {agrupacion},
      onSelectionChanged: (s) => alAgrupacion(s.first),
    );

    final buscador = _BuscadorProfesional(profesionalId: profesionalId, nombre: profesionalNombre, alElegir: alProfesional);
    final limpiar = hayFiltros
        ? TextButton.icon(onPressed: alLimpiar, icon: const Icon(Icons.filter_alt_off_outlined, size: 18), label: Text(t.cobLimpiar))
        : null;

    final Widget contenido;
    if (estrecho) {
      contenido = Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Center(child: navegacion),
        const SizedBox(height: 8),
        Center(child: periodos),
        const SizedBox(height: 10),
        SizedBox(width: double.infinity, child: agrupar),
        const SizedBox(height: 10),
        buscador,
        if (limpiar != null) Align(alignment: Alignment.centerRight, child: limpiar),
      ]);
    } else {
      contenido = Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Wrap(spacing: 16, runSpacing: 8, alignment: WrapAlignment.spaceBetween, crossAxisAlignment: WrapCrossAlignment.center, children: [
          navegacion,
          periodos,
          if (ancha) agrupar,
        ]),
        const SizedBox(height: 10),
        Wrap(spacing: 12, runSpacing: 8, crossAxisAlignment: WrapCrossAlignment.center, children: [
          SizedBox(width: 260, child: buscador),
          if (!ancha) agrupar,
          ?limpiar,
        ]),
      ]);
    }

    return Container(
      margin: const EdgeInsets.fromLTRB(12, 12, 12, 4),
      padding: const EdgeInsets.fromLTRB(10, 8, 10, 10),
      decoration: BoxDecoration(color: cs.surface, borderRadius: BorderRadius.circular(20), border: Border.all(color: cs.outlineVariant)),
      child: contenido,
    );
  }
}

// ── Resumen ──

/// Franja de métricas en una línea. «Sin cubrir» es también un filtro: al pulsarla solo se ven los turnos con plazas libres.
class _Resumen extends StatelessWidget {
  const _Resumen({required this.turnos, required this.ancho, required this.soloSinCubrir, required this.alSoloSinCubrir});

  final List<TurnoDto> turnos;
  final double ancho;
  final bool soloSinCubrir;
  final ValueChanged<bool> alSoloSinCubrir;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;
    final totales = turnos.fold<int>(0, (s, x) => s + x.plazasTotales);
    final excedidas = turnos.fold<int>(0, (s, x) => s + x.plazasExcedidas);
    final cubiertas = turnos.fold<int>(0, (s, x) => s + x.plazasCubiertas);
    final sin = totales - cubiertas;
    final pct = totales == 0 ? 100 : (cubiertas * 100 / totales).round();
    final personas = _profesionalesDe(turnos).length;

    final tiles = [
      _Metrica(icono: Icons.event_note_outlined, valor: '${turnos.length}', etiqueta: t.cobTurnos, color: cs.primary),
      _Metrica(icono: _iconoEstado(cubiertas, totales, excedidas: excedidas), valor: '$pct %', etiqueta: t.cobCobertura, color: _colorProgreso(context, cubiertas, totales, excedidas: excedidas)),
      _Metrica(
        icono: Icons.warning_amber_rounded,
        valor: '$sin',
        etiqueta: t.cobPlazasSinCubrir,
        color: sin > 0 ? cs.error : Colors.green.shade600,
        tooltip: t.soloSinCubrir,
        seleccionada: soloSinCubrir,
        alPulsar: () => alSoloSinCubrir(!soloSinCubrir),
      ),
      _Metrica(icono: Icons.groups_2_outlined, valor: '$personas', etiqueta: t.cobProfesionales, color: cs.tertiary),
    ];
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 6, 12, 0),
      child: _Rejilla(hijos: tiles, maxColumnas: 4, anchoMinimo: ancho < 600 ? 150 : 180, espacio: 8),
    );
  }
}

class _Metrica extends StatelessWidget {
  const _Metrica({required this.icono, required this.valor, required this.etiqueta, required this.color, this.alPulsar, this.tooltip, this.seleccionada = false});

  final IconData icono;
  final String valor;
  final String etiqueta;
  final Color color;
  final VoidCallback? alPulsar;
  final String? tooltip;
  final bool seleccionada;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final contenido = Padding(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
      child: Row(children: [
        Icon(icono, color: color, size: 22),
        const SizedBox(width: 8),
        Text(valor, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800, color: color, height: 1.1)),
        const SizedBox(width: 8),
        Expanded(child: Text(etiqueta, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant), maxLines: 1, overflow: TextOverflow.ellipsis)),
        if (alPulsar != null) Icon(seleccionada ? Icons.filter_alt : Icons.filter_alt_outlined, size: 16, color: color),
      ]),
    );
    final tarjeta = Material(
      color: color.withValues(alpha: seleccionada ? 0.22 : 0.09),
      shape:RoundedRectangleBorder(borderRadius: BorderRadius.circular(14), side: BorderSide(color: color.withValues(alpha: seleccionada ? 0.8 : 0.25), width: seleccionada ? 1.5 : 1)),
      clipBehavior: Clip.antiAlias,
      child: alPulsar == null ? contenido : InkWell(onTap: alPulsar, child: contenido),
    );
    return tooltip == null ? tarjeta : Tooltip(message: tooltip!, child: tarjeta);
  }
}

// ── Cabecera de grupo ──

class _CabeceraGrupo extends StatelessWidget {
  const _CabeceraGrupo({required this.titulo, required this.icono, required this.turnos, this.destacado = false});

  final String titulo;
  final IconData icono;
  final List<TurnoDto> turnos;
  final bool destacado;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final totales = turnos.fold<int>(0, (s, x) => s + x.plazasTotales);
    final cubiertas = turnos.fold<int>(0, (s, x) => s + x.plazasCubiertas);
    final excedidas = turnos.fold<int>(0, (s, x) => s + x.plazasExcedidas);
    final color = _colorProgreso(context, cubiertas, totales, excedidas: excedidas);
    return Padding(
      padding: const EdgeInsets.only(top: 18, bottom: 8),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(children: [
          Icon(icono, size: 20, color: destacado ? cs.primary : cs.onSurfaceVariant),
          const SizedBox(width: 8),
          Expanded(
            child: Text(titulo,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700, color: destacado ? cs.primary : null),
                overflow: TextOverflow.ellipsis),
          ),
          _PastillaEstado(texto: '${cubiertas + excedidas}/$totales', color: color, icono: _iconoEstado(cubiertas, totales, excedidas: excedidas)),
        ]),
        const SizedBox(height: 6),
        _BarraPlazas(cubiertas: cubiertas, total: totales, color: color),
      ]),
    );
  }
}

// ── Vistas ──

class _PorDia extends StatelessWidget {
  const _PorDia({required this.turnos, required this.rango, required this.resaltado, required this.colores, required this.alPulsar});

  final List<TurnoDto> turnos;
  final (DateTime, DateTime) rango;
  final String? resaltado;
  final Map<String, Color> colores;
  final ValueChanged<TurnoDto> alPulsar;

  @override
  Widget build(BuildContext context) {
    final hoy = soloFecha(DateTime.now());
    final dias = <DateTime>{for (final x in turnos) soloFecha(x.dia)}.toList()..sort();
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      for (final d in dias) ...[
        Builder(builder: (context) {
          final delDia = _ordenarPorUrgencia(turnos.where((x) => soloFecha(x.dia) == d));
          return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            _CabeceraGrupo(titulo: _etiquetaDia(d), icono: Icons.calendar_today_outlined, turnos: delDia, destacado: d == hoy),
            _Rejilla(hijos: [
              for (final x in delDia)
                TarjetaCobertura(turno: x, resaltado: resaltado, colorTipo: colores[x.tipoTurno]!, mostrarCentro: true, alPulsar: () => alPulsar(x)),
            ]),
          ]);
        }),
      ],
    ]);
  }
}

class _PorHospital extends StatelessWidget {
  const _PorHospital({required this.turnos, required this.resaltado, required this.colores, required this.alPulsar});

  final List<TurnoDto> turnos;
  final String? resaltado;
  final Map<String, Color> colores;
  final ValueChanged<TurnoDto> alPulsar;

  @override
  Widget build(BuildContext context) {
    final centros = <String, String>{for (final x in turnos) x.centroId: x.centro};
    final orden = centros.entries.toList()..sort((a, b) => a.value.toLowerCase().compareTo(b.value.toLowerCase()));
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      for (final c in orden)
        Builder(builder: (context) {
          final delCentro = turnos.where((x) => x.centroId == c.key).toList();
          final dias = <DateTime>{for (final x in delCentro) soloFecha(x.dia)}.toList()..sort();
          return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            _CabeceraGrupo(titulo: c.value, icono: Icons.local_hospital_outlined, turnos: delCentro, destacado: true),
            for (final d in dias) _FilaDia(
              dia: d,
              turnos: _ordenarPorUrgencia(delCentro.where((x) => soloFecha(x.dia) == d)),
              resaltado: resaltado,
              colores: colores,
              alPulsar: alPulsar,
            ),
          ]);
        }),
    ]);
  }
}

/// Fila «día + sus turnos»: en pantallas anchas la fecha queda a la izquierda.
class _FilaDia extends StatelessWidget {
  const _FilaDia({required this.dia, required this.turnos, required this.resaltado, required this.colores, required this.alPulsar});

  final DateTime dia;
  final List<TurnoDto> turnos;
  final String? resaltado;
  final Map<String, Color> colores;
  final ValueChanged<TurnoDto> alPulsar;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final esHoy = soloFecha(DateTime.now()) == dia;
    final tarjetas = _Rejilla(
      hijos: [
        for (final x in turnos)
          TarjetaCobertura(turno: x, resaltado: resaltado, colorTipo: colores[x.tipoTurno]!, mostrarCentro: false, alPulsar: () => alPulsar(x)),
      ],
    );
    final fecha = Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: esHoy ? cs.primary : cs.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        Text(toBeginningOfSentenceCase(DateFormat('EEE', 'es_ES').format(dia)),
            style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: esHoy ? cs.onPrimary : cs.onSurfaceVariant)),
        Text('${dia.day}', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: esHoy ? cs.onPrimary : cs.onSurface, height: 1.1)),
        Text(toBeginningOfSentenceCase(DateFormat('MMM', 'es_ES').format(dia)),
            style: TextStyle(fontSize: 11, color: esHoy ? cs.onPrimary : cs.onSurfaceVariant)),
      ]),
    );
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: LayoutBuilder(builder: (context, c) {
        if (c.maxWidth < 520) {
          return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Align(alignment: Alignment.centerLeft, child: Padding(padding: const EdgeInsets.only(bottom: 6), child: Text(_etiquetaDia(dia), style: Theme.of(context).textTheme.labelLarge?.copyWith(color: esHoy ? cs.primary : cs.onSurfaceVariant, fontWeight: FontWeight.w700)))),
            tarjetas,
          ]);
        }
        return Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          SizedBox(width: 56, child: fecha),
          const SizedBox(width: 10),
          Expanded(child: tarjetas),
        ]);
      }),
    );
  }
}

class _PorProfesional extends StatelessWidget {
  const _PorProfesional({required this.turnos, required this.rango, required this.profesionalId, required this.colores, required this.alPulsar});

  final List<TurnoDto> turnos;
  final (DateTime, DateTime) rango;
  final String? profesionalId;
  final Map<String, Color> colores;
  final ValueChanged<TurnoDto> alPulsar;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;
    // profesional → turnos que ocupa
    final porPersona = <String, List<TurnoDto>>{};
    final nombres = <String, String>{};
    for (final x in turnos) {
      for (final a in _asignados(x)) {
        if (profesionalId != null && a.profesionalId != profesionalId) continue;
        (porPersona[a.profesionalId] ??= []).add(x);
        nombres[a.profesionalId] = a.profesional;
      }
    }
    final ids = porPersona.keys.toList()..sort((a, b) => nombres[a]!.toLowerCase().compareTo(nombres[b]!.toLowerCase()));
    if (ids.isEmpty) return EstadoVacio(mensaje: t.cobSinResultados, icono: Icons.person_off_outlined);
    final dias = <DateTime>[for (var d = rango.$1; !d.isAfter(rango.$2); d = d.add(const Duration(days: 1))) d];

    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      const SizedBox(height: 4),
      _Matriz(ids: ids, nombres: nombres, porPersona: porPersona, dias: dias, colores: colores, alPulsar: alPulsar),
      const SizedBox(height: 10),
      _Leyenda(colores: colores),
      for (final id in ids)
        Builder(builder: (context) {
          final suyos = porPersona[id]!..sort((a, b) => a.dia != b.dia ? a.dia.compareTo(b.dia) : a.inicioLocal.compareTo(b.inicioLocal));
          final horas = suyos.fold<double>(0, (s, x) => s + x.duracionHoras);
          final hospitales = {for (final x in suyos) x.centro}.join(' · ');
          return Padding(
            padding: const EdgeInsets.only(top: 14),
            child: Card(
              margin: EdgeInsets.zero,
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                  Row(children: [
                    CircleAvatar(backgroundColor: cs.primary.withValues(alpha: 0.14), child: Text(_inicial(nombres[id]!), style: TextStyle(color: cs.primary, fontWeight: FontWeight.w700))),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(nombres[id]!, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
                        Text(hospitales, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant), maxLines: 2, overflow: TextOverflow.ellipsis),
                      ]),
                    ),
                    Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                      Text(t.cobNTurnos(suyos.length), style: Theme.of(context).textTheme.labelLarge?.copyWith(fontWeight: FontWeight.w700)),
                      Text(t.cobHoras(horas.toStringAsFixed(horas == horas.roundToDouble() ? 0 : 1)), style: Theme.of(context).textTheme.bodySmall),
                    ]),
                  ]),
                  const SizedBox(height: 12),
                  _Rejilla(anchoMinimo: 250, espacio: 8, hijos: [for (final x in suyos) _MiniTurno(turno: x, color: colores[x.tipoTurno]!, alPulsar: () => alPulsar(x))]),
                ]),
              ),
            ),
          );
        }),
    ]);
  }
}

class _MiniTurno extends StatelessWidget {
  const _MiniTurno({required this.turno, required this.color, required this.alPulsar});

  final TurnoDto turno;
  final Color color;
  final VoidCallback alPulsar;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Material(
      color: color.withValues(alpha: 0.10),
      borderRadius: BorderRadius.circular(12),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: alPulsar,
        child: Padding(
          padding: const EdgeInsets.all(10),
          child: Row(children: [
            Container(
              width: 44,
              padding: const EdgeInsets.symmetric(vertical: 4),
              decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(8)),
              child: Column(mainAxisSize: MainAxisSize.min, children: [
                Text(toBeginningOfSentenceCase(DateFormat('EEE', 'es_ES').format(turno.dia)), style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w600)),
                Text('${turno.dia.day}', style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.w800, height: 1.1)),
              ]),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text('${turno.tipoTurno} · ${franjaTurno(turno)}', style: Theme.of(context).textTheme.labelLarge?.copyWith(fontWeight: FontWeight.w700), maxLines: 1, overflow: TextOverflow.ellipsis),
                Text('${turno.servicio} · ${turno.centro}', style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant), maxLines: 1, overflow: TextOverflow.ellipsis),
              ]),
            ),
          ]),
        ),
      ),
    );
  }
}

/// Matriz profesional × día: de un vistazo, quién trabaja qué turno y qué días.
class _Matriz extends StatelessWidget {
  const _Matriz({required this.ids, required this.nombres, required this.porPersona, required this.dias, required this.colores, required this.alPulsar});

  final List<String> ids;
  final Map<String, String> nombres;
  final Map<String, List<TurnoDto>> porPersona;
  final List<DateTime> dias;
  final Map<String, Color> colores;
  final ValueChanged<TurnoDto> alPulsar;

  static const _celda = 36.0;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final hoy = soloFecha(DateTime.now());
    final anchoNombre = MediaQuery.sizeOf(context).width < 600 ? 104.0 : 170.0;

    Widget cabecera(DateTime d) {
      final finde = d.weekday >= DateTime.saturday;
      return Container(
        width: _celda,
        height: 44,
        margin: const EdgeInsets.all(1),
        decoration: BoxDecoration(
          color: d == hoy ? cs.primary : (finde ? cs.surfaceContainerHighest : Colors.transparent),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
          Text(DateFormat('E', 'es_ES').format(d).substring(0, 1).toUpperCase(), style: TextStyle(fontSize: 10, color: d == hoy ? cs.onPrimary : cs.onSurfaceVariant)),
          Text('${d.day}', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: d == hoy ? cs.onPrimary : cs.onSurface)),
        ]),
      );
    }

    Widget celda(String id, DateTime d) {
      final finde = d.weekday >= DateTime.saturday;
      final delDia = porPersona[id]!.where((x) => soloFecha(x.dia) == d).toList();
      final fondo = finde ? cs.surfaceContainerHighest.withValues(alpha: 0.6) : cs.surfaceContainerLow;
      if (delDia.isEmpty) {
        return Container(width: _celda, height: _celda, margin: const EdgeInsets.all(1), decoration: BoxDecoration(color: fondo, borderRadius: BorderRadius.circular(8)));
      }
      final primero = delDia.first;
      final color = colores[primero.tipoTurno]!;
      final descripcion = delDia.map((x) => '${x.tipoTurno} ${franjaTurno(x)} · ${x.centro}').join('\n');
      return Tooltip(
        message: '${nombres[id]}\n$descripcion',
        child: Semantics(
          button: true,
          label: '${nombres[id]}: $descripcion',
          child: InkWell(
            borderRadius: BorderRadius.circular(8),
            onTap: () => alPulsar(primero),
            child: Container(
              width: _celda,
              height: _celda,
              margin: const EdgeInsets.all(1),
              alignment: Alignment.center,
              decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(8)),
              child: Text(delDia.length > 1 ? '${delDia.length}×' : primero.tipoTurno.substring(0, 1).toUpperCase(),
                  style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w800, fontSize: 13)),
            ),
          ),
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 6),
      decoration: BoxDecoration(color: cs.surface, borderRadius: BorderRadius.circular(16), border: Border.all(color: cs.outlineVariant)),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        SizedBox(
          width: anchoNombre,
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            const SizedBox(height: 46),
            for (final id in ids)
              Container(
                height: _celda,
                margin: const EdgeInsets.symmetric(vertical: 1),
                padding: const EdgeInsets.only(left: 6, right: 4),
                alignment: Alignment.centerLeft,
                child: Text(nombres[id]!, style: Theme.of(context).textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600), maxLines: 2, overflow: TextOverflow.ellipsis),
              ),
          ]),
        ),
        Expanded(
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Column(children: [
              Row(children: [for (final d in dias) cabecera(d)]),
              for (final id in ids) Row(children: [for (final d in dias) celda(id, d)]),
            ]),
          ),
        ),
      ]),
    );
  }
}

class _Leyenda extends StatelessWidget {
  const _Leyenda({required this.colores});
  final Map<String, Color> colores;

  @override
  Widget build(BuildContext context) {
    return Wrap(spacing: 14, runSpacing: 6, children: [
      for (final e in colores.entries)
        Row(mainAxisSize: MainAxisSize.min, children: [
          Container(
            width: 20,
            height: 20,
            alignment: Alignment.center,
            decoration: BoxDecoration(color: e.value, borderRadius: BorderRadius.circular(6)),
            child: Text(e.key.substring(0, 1).toUpperCase(), style: const TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.w800)),
          ),
          const SizedBox(width: 6),
          Text(e.key, style: Theme.of(context).textTheme.bodySmall),
        ]),
    ]);
  }
}

// ── Tarjeta de turno con los profesionales que lo cubren ──

class TarjetaCobertura extends StatelessWidget {
  const TarjetaCobertura({super.key, required this.turno, required this.colorTipo, required this.alPulsar, this.resaltado, this.mostrarCentro = true});

  final TurnoDto turno;
  final Color colorTipo;
  final VoidCallback alPulsar;
  final String? resaltado;
  final bool mostrarCentro;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;
    final estado = colorCobertura(context, turno);
    final texto = Theme.of(context).textTheme;

    return Card(
      margin: EdgeInsets.zero,
      clipBehavior: Clip.antiAlias,
      color: Color.alphaBlend(estado.withValues(alpha: 0.05), cs.surface),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16), side: BorderSide(color: estado.withValues(alpha: 0.35))),
      child: InkWell(
        onTap: alPulsar,
        child: Container(
          decoration: BoxDecoration(border: Border(left: BorderSide(color: colorTipo, width: 6))),
          padding: const EdgeInsets.fromLTRB(12, 10, 12, 10),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              if (turno.esGuardia) const Padding(padding: EdgeInsets.only(right: 4), child: Icon(Icons.nightlight_outlined, size: 16)),
              Expanded(
                child: Text(turno.tipoTurno, style: texto.titleSmall?.copyWith(fontWeight: FontWeight.w800, color: colorTipo), maxLines: 1, overflow: TextOverflow.ellipsis),
              ),
              if (turno.editable)
                Padding(
                  padding: const EdgeInsets.only(right: 6),
                  child: Tooltip(message: t.cobBorrador, child: Icon(Icons.edit_note_outlined, size: 18, color: cs.onSurfaceVariant)),
                ),
              Semantics(
                label: t.plazasCubiertasSemantica(turno.asignados, turno.plazasTotales),
                child: _PastillaEstado(
                  texto: '${turno.asignados}/${turno.plazasTotales}',
                  color: estado,
                  icono: _iconoEstado(turno.plazasCubiertas, turno.plazasTotales, excedidas: turno.plazasExcedidas),
                ),
              ),
            ]),
            const SizedBox(height: 2),
            Row(children: [
              Icon(Icons.schedule, size: 14, color: cs.onSurfaceVariant),
              const SizedBox(width: 4),
              Text(franjaTurno(turno), style: texto.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
            ]),
            Text(mostrarCentro ? '${turno.centro} · ${turno.servicio}' : turno.servicio,
                style: texto.bodySmall?.copyWith(color: cs.onSurfaceVariant), maxLines: 1, overflow: TextOverflow.ellipsis),
            const SizedBox(height: 8),
            _BarraPlazas(cubiertas: turno.plazasCubiertas, total: turno.plazasTotales, color: estado),
            const SizedBox(height: 8),
            // Por categoría: «Médico 4/4», «Enfermería 2/3»… Con una sola necesidad sin categoría la pastilla ya lo dice todo.
            for (final n in turno.necesidades) ...[
              Builder(builder: (context) {
                final asignados = n.asignaciones.length;
                final c = asignados > n.numeroPlazas ? cs.error : (asignados == n.numeroPlazas ? Colors.green.shade600 : Colors.amber.shade700);
                final cabecera = turno.necesidades.length > 1 || n.categoria != null;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 6),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    if (cabecera)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Row(children: [
                          Expanded(child: Text(n.categoria ?? t.cobCualquierCategoria, style: texto.labelMedium?.copyWith(fontWeight: FontWeight.w700, color: cs.onSurfaceVariant), overflow: TextOverflow.ellipsis)),
                          Text('$asignados/${n.numeroPlazas}', style: texto.labelMedium?.copyWith(color: c, fontWeight: FontWeight.w800)),
                        ]),
                      ),
                    Wrap(spacing: 4, runSpacing: 4, children: [
                      for (final a in n.asignaciones) _ChipProfesional(nombre: a.profesional, resaltado: a.profesionalId == resaltado),
                      if (n.plazasLibres > 0) _ChipLibre(texto: n.plazasLibres == 1 ? t.cobSinAsignar : '${t.cobSinAsignar} ×${n.plazasLibres}'),
                    ]),
                  ]),
                );
              }),
            ],
            if (turno.excedido)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Row(children: [
                  Icon(Icons.error_outline, size: 16, color: cs.error),
                  const SizedBox(width: 4),
                  Expanded(child: Text(t.sobranProfesionales(turno.plazasExcedidas), style: texto.bodySmall?.copyWith(color: cs.error, fontWeight: FontWeight.w700))),
                ]),
              ),
          ]),
        ),
      ),
    );
  }
}

class _ChipProfesional extends StatelessWidget {
  const _ChipProfesional({required this.nombre, required this.resaltado});

  final String nombre;
  final bool resaltado;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    // Los asignados quedan en gris discreto; solo el profesional filtrado y los huecos llaman la atención.
    final fondo = resaltado ? cs.primary : cs.surfaceContainerHighest;
    final frente = resaltado ? cs.onPrimary : cs.onSurface;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: fondo, borderRadius: BorderRadius.circular(10)),
      child: Text(nombre, style: TextStyle(fontSize: 12, fontWeight: resaltado ? FontWeight.w700 : FontWeight.w500, color: frente), overflow: TextOverflow.ellipsis),
    );
  }
}

class _ChipLibre extends StatelessWidget {
  const _ChipLibre({required this.texto});
  final String texto;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: Colors.amber.shade800.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(10), border: Border.all(color: Colors.amber.shade800.withValues(alpha: 0.5))),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        Icon(Icons.person_add_alt_outlined, size: 14, color: Colors.amber.shade800),
        const SizedBox(width: 5),
        Flexible(child: Text(texto, style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: Colors.amber.shade800), overflow: TextOverflow.ellipsis)),
      ]),
    );
  }
}

// ── Buscador de profesionales ──

/// Busca entre TODOS los profesionales de la sociedad (no solo los que aparecen en el periodo). Como pueden ser cientos,
/// la búsqueda empieza al escribir [minLetras] letras y la resuelve el servidor.
class _BuscadorProfesional extends ConsumerStatefulWidget {
  const _BuscadorProfesional({required this.profesionalId, required this.nombre, required this.alElegir});

  final String? profesionalId;
  final String? nombre;
  final ValueChanged<ProfesionalDto?> alElegir;

  static const minLetras = 3;

  @override
  ConsumerState<_BuscadorProfesional> createState() => _EstadoBuscador();
}

class _EstadoBuscador extends ConsumerState<_BuscadorProfesional> {
  String _ultimaBusqueda = '';

  Future<Iterable<ProfesionalDto>> _buscar(String texto) async {
    final q = texto.trim();
    _ultimaBusqueda = q;
    if (q.length < _BuscadorProfesional.minLetras) return const [];
    await Future<void>.delayed(const Duration(milliseconds: 300)); // espera a que termine de escribir
    if (!mounted || q != _ultimaBusqueda) return const [];
    try {
      return await ref.read(profesionalesRepositorioProvider).listar(activo: true, texto: q);
    } catch (_) {
      return const [];
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;

    if (widget.profesionalId != null) {
      return InputDecorator(
        decoration: InputDecoration(labelText: t.cobProfesional, isDense: true, prefixIcon: const Icon(Icons.person_search_outlined, size: 20)),
        child: Row(children: [
          Expanded(child: Text(widget.nombre ?? '', overflow: TextOverflow.ellipsis)),
          InkWell(
            onTap: () => widget.alElegir(null),
            borderRadius: BorderRadius.circular(12),
            child: Tooltip(message: t.cobLimpiar, child: Icon(Icons.close, size: 18, color: cs.onSurfaceVariant)),
          ),
        ]),
      );
    }

    return LayoutBuilder(builder: (context, c) {
      return Autocomplete<ProfesionalDto>(
        displayStringForOption: (p) => p.nombreCompleto,
        optionsBuilder: (v) => _buscar(v.text),
        onSelected: widget.alElegir,
        fieldViewBuilder: (context, controlador, foco, _) => TextField(
          controller: controlador,
          focusNode: foco,
          decoration: InputDecoration(
            labelText: t.cobProfesional,
            hintText: t.cobBuscarProfesional,
            isDense: true,
            prefixIcon: const Icon(Icons.person_search_outlined, size: 20),
          ),
        ),
        optionsViewBuilder: (context, alElegir, opciones) => Align(
          alignment: Alignment.topLeft,
          child: Material(
            elevation: 4,
            borderRadius: BorderRadius.circular(12),
            child: ConstrainedBox(
              constraints: BoxConstraints(maxHeight: 280, maxWidth: c.maxWidth.isFinite ? c.maxWidth : 320),
              child: ListView.builder(
                padding: EdgeInsets.zero,
                shrinkWrap: true,
                itemCount: opciones.length,
                itemBuilder: (context, i) {
                  final p = opciones.elementAt(i);
                  return ListTile(dense: true, title: Text(p.nombreCompleto, overflow: TextOverflow.ellipsis), onTap: () => alElegir(p));
                },
              ),
            ),
          ),
        ),
      );
    });
  }
}
