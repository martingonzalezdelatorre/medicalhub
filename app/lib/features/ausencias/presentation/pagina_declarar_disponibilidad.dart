import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../core/tema/tema.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/seleccion.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../planificacion/data/planificacion_repositorio.dart' show fechaParaApi;
import '../data/ausencias_repositorio.dart';
import 'calendario_rango.dart';
import 'etiquetas_ausencias.dart';

const _nombresDias = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'];
const _laborables = 31;
const _finSemana = 96;

/// Días y turnos de un hospital: p. ej. «lunes y miércoles, mañana o tarde».
class _Franja {
  int dias = diasTodos;

  /// Turnos elegidos como `nombre|franja` (agrupan los de igual nombre y horario de los distintos servicios). Vacío = todos.
  final Set<String> turnos = {};
}

/// Un hospital (`centroId` nulo = cualquiera) con una o varias franjas de días y turnos.
class _Hospital {
  _Hospital(this.centroId);
  String? centroId;

  /// Cambia al rechazar una selección para reconstruir el selector con el valor vigente.
  int version = 0;
  final List<_Franja> franjas = [_Franja()];
}

String _claveTurno(TipoTurnoPropioDto x) => '${x.nombre}|${x.franja}';

String _etiquetaTurno(String clave) => clave.replaceFirst('|', ' · ');

String _textoDias(AppLocalizations t, int mascara) =>
    mascara == diasTodos ? t.todosLosDias : [for (var i = 0; i < 7; i++) if (mascara & (1 << i) != 0) _nombresDias[i]].join(', ');

/// Declaración de disponibilidad del propio profesional, en pantalla completa: tipo, periodo y, por cada hospital,
/// los días de la semana con los turnos que se quieren en ellos. En pantallas anchas el resumen queda a un lado.
class PaginaDeclararDisponibilidad extends ConsumerStatefulWidget {
  const PaginaDeclararDisponibilidad({super.key});

  @override
  ConsumerState<PaginaDeclararDisponibilidad> createState() => _EstadoPagina();
}

class _EstadoPagina extends ConsumerState<PaginaDeclararDisponibilidad> {
  static const _anchoDosColumnas = 860.0;

  final _comentario = TextEditingController();
  TipoDisponibilidad _tipo = TipoDisponibilidad.disponible;
  DateTime _desde = DateTime.now().add(const Duration(days: 1));
  late DateTime _hasta = _desde;
  final List<_Hospital> _hospitales = [];
  bool _enviando = false;
  String? _error;

  @override
  void dispose() {
    _comentario.dispose();
    super.dispose();
  }

  void _volver() => context.go('/mi-disponibilidad');

  /// Semana (lun–vie) en curso; si hoy es fin de semana, la siguiente.
  void _periodoSemana() {
    final hoy = DateTime.now();
    var lunes = DateTime(hoy.year, hoy.month, hoy.day - (hoy.weekday - 1));
    if (hoy.weekday > DateTime.friday) lunes = lunes.add(const Duration(days: 7));
    _aplicarPeriodo(lunes, lunes.add(const Duration(days: 4)), soloLaborables: true);
  }

  void _periodoMes() {
    final hoy = DateTime.now();
    _aplicarPeriodo(DateTime(hoy.year, hoy.month), DateTime(hoy.year, hoy.month + 1, 0));
  }

  /// Los próximos 15 días sin pasar del fin de mes, solo días hábiles.
  void _periodo15Dias() {
    final hoy = DateTime.now();
    var desde = DateTime(hoy.year, hoy.month, hoy.day);
    final finMes = DateTime(hoy.year, hoy.month + 1, 0);
    var hasta = desde.add(const Duration(days: 14));
    if (hasta.isAfter(finMes)) hasta = finMes;
    while (desde.weekday > DateTime.friday && !desde.isAfter(hasta)) {
      desde = desde.add(const Duration(days: 1));
    }
    while (hasta.weekday > DateTime.friday && !hasta.isBefore(desde)) {
      hasta = hasta.subtract(const Duration(days: 1));
    }
    if (hasta.isBefore(desde)) hasta = desde;
    _aplicarPeriodo(desde, hasta, soloLaborables: true);
  }

  void _aplicarPeriodo(DateTime desde, DateTime hasta, {bool soloLaborables = false}) => setState(() {
        _desde = desde;
        _hasta = hasta;
        if (soloLaborables) {
          for (final h in _hospitales) {
            for (final f in h.franjas) {
              f.dias = _laborables;
            }
          }
        }
      });

  /// Hospitales donde el profesional tiene turnos, por id → nombre.
  Map<String, String> _hospitalesDisponibles(List<TipoTurnoPropioDto> tipos) =>
      {for (final x in tipos) if (x.centroId != null) x.centroId!: x.centro};

  /// Ids de tipo de turno de un hospital agrupados por nombre y horario.
  Map<String, List<String>> _turnosDe(List<TipoTurnoPropioDto> tipos, String? centroId) {
    final grupos = <String, List<String>>{};
    if (centroId == null) return grupos;
    for (final x in tipos.where((x) => x.centroId == centroId)) {
      grupos.putIfAbsent(_claveTurno(x), () => []).add(x.id);
    }
    return grupos;
  }

  List<BloqueDisponibilidad> _bloques(List<TipoTurnoPropioDto> tipos) {
    final bloques = <BloqueDisponibilidad>[];
    for (final h in _hospitales) {
      final grupos = _turnosDe(tipos, h.centroId);
      for (final f in h.franjas) {
        final claves = f.turnos.where(grupos.containsKey).toList();
        if (claves.isEmpty) {
          bloques.add(BloqueDisponibilidad(dias: f.dias, centroId: h.centroId));
        } else {
          for (final c in claves) {
            for (final id in grupos[c]!) {
              bloques.add(BloqueDisponibilidad(dias: f.dias, centroId: h.centroId, tipoTurnoId: id));
            }
          }
        }
      }
    }
    return bloques;
  }

  Future<void> _enviar(List<TipoTurnoPropioDto> tipos) async {
    final t = AppLocalizations.of(context);
    if (_hospitales.map((h) => h.centroId).toSet().length != _hospitales.length) {
      setState(() => _error = t.hospitalYaElegido);
      return;
    }
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(ausenciasRepositorioProvider).declararLote(
            tipo: _tipo,
            desde: _desde,
            hasta: _hasta,
            bloques: _bloques(tipos),
            comentario: _comentario.text.trim().isEmpty ? null : _comentario.text.trim(),
          );
      ref.invalidate(miDisponibilidadProvider);
      if (!mounted) return;
      mostrarMensaje(context, t.disponibilidadDeclarada);
      _volver();
    } on FalloApi catch (f) {
      if (mounted) setState(() => _error = f.errores.isEmpty ? mensajeDeFallo(t, f) : f.errores.values.expand((e) => e).join(' '));
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  Widget _tarjeta(IconData icono, String titulo, Widget hijo, {Widget? accion}) {
    final tema = Theme.of(context);
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(color: tema.colorScheme.primary.withAlpha(24), borderRadius: BorderRadius.circular(10)),
              child: Icon(icono, size: 18, color: tema.colorScheme.primary),
            ),
            const SizedBox(width: 10),
            Expanded(child: Text(titulo, style: tema.textTheme.titleSmall)),
            ?accion,
          ]),
          const SizedBox(height: 14),
          hijo,
        ]),
      ),
    );
  }

  Widget _selectorFecha(String etiqueta, DateTime valor, ValueChanged<DateTime> alElegir) => OutlinedButton.icon(
        icon: const Icon(Icons.calendar_today_outlined, size: 18),
        style: OutlinedButton.styleFrom(alignment: Alignment.centerLeft, padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 16)),
        onPressed: () async {
          final f = await elegirFecha(context, valor);
          if (f != null) alElegir(f);
        },
        label: Text('$etiqueta: ${formatearFecha(fechaParaApi(valor))}', overflow: TextOverflow.ellipsis),
      );

  /// Días de la semana y turnos de una franja de un hospital.
  Widget _franja(AppLocalizations t, _Hospital h, _Franja f, Map<String, List<String>> grupos, {required bool quitable}) {
    final tema = Theme.of(context);
    final cs = tema.colorScheme;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: cs.surfaceContainerLow, borderRadius: BorderRadius.circular(12), border: Border.all(color: cs.outlineVariant)),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Expanded(child: Text(t.campoDiasSemana, style: tema.textTheme.labelLarge)),
          if (quitable)
            IconButton(
              tooltip: t.quitarBloque,
              visualDensity: VisualDensity.compact,
              icon: const Icon(Icons.close, size: 18),
              onPressed: () => setState(() => h.franjas.remove(f)),
            ),
        ]),
        Wrap(spacing: 6, runSpacing: 6, children: [
          for (var i = 0; i < 7; i++)
            FilterChip(
              label: Text(_nombresDias[i]),
              selected: f.dias & (1 << i) != 0,
              onSelected: (v) => setState(() {
                final nuevo = v ? f.dias | (1 << i) : f.dias & ~(1 << i);
                if (nuevo != 0) f.dias = nuevo; // siempre al menos un día
              }),
            ),
        ]),
        const SizedBox(height: 4),
        Wrap(spacing: 4, children: [
          ActionChip(visualDensity: VisualDensity.compact, label: Text(t.diasLaborables), onPressed: () => setState(() => f.dias = _laborables)),
          ActionChip(visualDensity: VisualDensity.compact, label: Text(t.diasFinSemana), onPressed: () => setState(() => f.dias = _finSemana)),
          ActionChip(visualDensity: VisualDensity.compact, label: Text(t.diasTodosAtajo), onPressed: () => setState(() => f.dias = diasTodos)),
        ]),
        const SizedBox(height: 14),
        Text(t.turnosEnLosDias, style: tema.textTheme.labelLarge),
        const SizedBox(height: 8),
        Wrap(spacing: 8, runSpacing: 8, children: [
          ChoiceChip(label: Text(t.todosLosTurnos), selected: f.turnos.isEmpty, onSelected: (_) => setState(f.turnos.clear)),
          for (final clave in grupos.keys)
            FilterChip(
              label: Text(_etiquetaTurno(clave)),
              selected: f.turnos.contains(clave),
              onSelected: (v) => setState(() => v ? f.turnos.add(clave) : f.turnos.remove(clave)),
            ),
        ]),
      ]),
    );
  }

  Widget _tarjetaHospital(AppLocalizations t, List<TipoTurnoPropioDto> tipos, _Hospital h, int indice) {
    final nombres = _hospitalesDisponibles(tipos);
    final usados = {for (final o in _hospitales) if (o != h) o.centroId};
    final grupos = _turnosDe(tipos, h.centroId);
    return _tarjeta(
      Icons.local_hospital_outlined,
      '${t.campoHospital} ${indice + 1}',
      Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        DropdownButtonFormField<String?>(
          key: ValueKey('${h.hashCode}-${h.centroId}-${h.version}'),
          initialValue: h.centroId,
          isExpanded: true,
          decoration: const InputDecoration(prefixIcon: Icon(Icons.apartment_outlined)),
          items: [
            DropdownMenuItem(value: null, child: Text(t.cualquierHospital)),
            for (final e in nombres.entries) DropdownMenuItem(value: e.key, child: Text(e.value, overflow: TextOverflow.ellipsis)),
          ],
          onChanged: (v) => setState(() {
            if (usados.contains(v)) {
              // Se restaura el valor anterior en el selector y se avisa.
              h.version++;
              mostrarMensaje(context, t.hospitalYaElegido);
              return;
            }
            h.centroId = v;
            // Los turnos elegidos solo valen para el hospital seleccionado.
            final validos = _turnosDe(tipos, v).keys.toSet();
            for (final f in h.franjas) {
              f.turnos.removeWhere((c) => !validos.contains(c));
            }
          }),
        ),
        for (final f in h.franjas) ...[
          const SizedBox(height: 12),
          _franja(t, h, f, grupos, quitable: h.franjas.length > 1),
        ],
        const SizedBox(height: 8),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(icon: const Icon(Icons.add), label: Text(t.anadirFranja), onPressed: () => setState(() => h.franjas.add(_Franja()))),
        ),
      ]),
      accion: _hospitales.length > 1
          ? IconButton(tooltip: t.quitarBloque, icon: const Icon(Icons.delete_outline), onPressed: () => setState(() => _hospitales.remove(h)))
          : null,
    );
  }

  Widget _formulario(AppLocalizations t, List<TipoTurnoPropioDto> tipos) {
    final nombres = _hospitalesDisponibles(tipos);
    final usados = {for (final h in _hospitales) h.centroId};
    final quedanHospitales = nombres.keys.any((id) => !usados.contains(id)) || !usados.contains(null);

    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      _tarjeta(
        Icons.tune_outlined,
        t.campoTipoDisponibilidad,
        Wrap(spacing: 8, runSpacing: 8, children: [
          for (final x in TipoDisponibilidad.values)
            ChoiceChip(label: Text(etiquetaTipoDisponibilidad(t, x)), selected: _tipo == x, onSelected: (_) => setState(() => _tipo = x)),
        ]),
      ),
      const SizedBox(height: 12),
      _tarjeta(
        Icons.date_range_outlined,
        t.seccionPeriodo,
        LayoutBuilder(builder: (context, c) {
          final desde = _selectorFecha(t.campoDesde, _desde, (f) => setState(() {
                _desde = f;
                if (_hasta.isBefore(f)) _hasta = f;
              }));
          final hasta = _selectorFecha(t.campoHasta, _hasta, (f) => setState(() => _hasta = f.isBefore(_desde) ? _desde : f));
          final calendario = CalendarioRango(desde: _desde, hasta: _hasta, alCambiar: (d, h) => setState(() {
                _desde = d;
                _hasta = h;
              }));
          final fechas = c.maxWidth >= 420
              ? Row(children: [Expanded(child: desde), const SizedBox(width: 12), Expanded(child: hasta)])
              : Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [desde, const SizedBox(height: 10), hasta]);
          final atajos = Wrap(spacing: 8, runSpacing: 4, children: [
            ActionChip(avatar: const Icon(Icons.view_week_outlined, size: 18), label: const Text('Semana (lun–vie)'), onPressed: _periodoSemana),
            ActionChip(avatar: const Icon(Icons.calendar_month_outlined, size: 18), label: const Text('Mes entero'), onPressed: _periodoMes),
            ActionChip(avatar: const Icon(Icons.date_range_outlined, size: 18), label: const Text('Próximos 15 días'), onPressed: _periodo15Dias),
          ]);
          return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [atajos, const SizedBox(height: 12), calendario, const SizedBox(height: 12), fechas]);
        }),
      ),
      if (tipos.isEmpty) ...[
        const SizedBox(height: 12),
        Text(t.sinTurnosPropios, style: Theme.of(context).textTheme.bodySmall),
      ],
      for (var i = 0; i < _hospitales.length; i++) ...[
        const SizedBox(height: 12),
        _tarjetaHospital(t, tipos, _hospitales[i], i),
      ],
      if (tipos.isNotEmpty && quedanHospitales) ...[
        const SizedBox(height: 8),
        Align(
          alignment: Alignment.centerLeft,
          child: OutlinedButton.icon(
            icon: const Icon(Icons.add_business_outlined),
            label: Text(t.anadirHospital),
            onPressed: () => setState(() {
              final libre = nombres.keys.where((id) => !usados.contains(id)).firstOrNull;
              _hospitales.add(_Hospital(libre));
            }),
          ),
        ),
      ],
      const SizedBox(height: 12),
      _tarjeta(
        Icons.notes_outlined,
        t.campoComentarioOpcional,
        TextField(controller: _comentario, maxLength: 300, maxLines: 3, decoration: const InputDecoration(alignLabelWithHint: true)),
      ),
    ]);
  }

  Widget _resumen(AppLocalizations t, List<TipoTurnoPropioDto> tipos) {
    final tema = Theme.of(context);
    final cs = tema.colorScheme;
    final nombres = _hospitalesDisponibles(tipos);
    Widget fila(IconData icono, String texto, {bool negrita = false}) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Icon(icono, size: 18, color: cs.onSurfaceVariant),
            const SizedBox(width: 10),
            Expanded(child: Text(texto, style: negrita ? tema.textTheme.titleSmall : tema.textTheme.bodyMedium)),
          ]),
        );
    return _tarjeta(
      Icons.fact_check_outlined,
      t.resumenTitulo,
      Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        fila(Icons.tune_outlined, etiquetaTipoDisponibilidad(t, _tipo), negrita: true),
        fila(Icons.date_range_outlined, rangoFechasApi(fechaParaApi(_desde), fechaParaApi(_hasta))),
        for (final h in _hospitales) ...[
          const Divider(height: 20),
          fila(Icons.local_hospital_outlined, h.centroId == null ? t.cualquierHospital : nombres[h.centroId] ?? '', negrita: true),
          for (final f in h.franjas)
            fila(Icons.schedule_outlined,
                '${_textoDias(t, f.dias)} · ${f.turnos.isEmpty ? t.todosLosTurnos : f.turnos.map(_etiquetaTurno).join(', ')}'),
        ],
        const SizedBox(height: 10),
        Text(t.avisoDisponibilidad, style: tema.textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
      ]),
    );
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tema = Theme.of(context);
    final tipos = ref.watch(misTiposTurnoProvider).value ?? const <TipoTurnoPropioDto>[];
    final ancho = MediaQuery.sizeOf(context).width;
    final dosColumnas = ancho >= _anchoDosColumnas;

    // Al llegar los turnos propios se parte de su primer hospital; sin ellos, de «cualquier hospital».
    if (_hospitales.isEmpty && ref.read(misTiposTurnoProvider).hasValue) {
      _hospitales.add(_Hospital(_hospitalesDisponibles(tipos).keys.firstOrNull));
    }

    final contenido = dosColumnas
        ? Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Expanded(flex: 3, child: _formulario(t, tipos)),
            const SizedBox(width: 16),
            Expanded(flex: 2, child: _resumen(t, tipos)),
          ])
        : Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [_formulario(t, tipos), const SizedBox(height: 12), _resumen(t, tipos)]);

    return Column(children: [
      Expanded(
        child: ContenidoCentrado(
          anchoMaximo: 960,
          child: ListView(padding: EdgeInsets.all(ancho < Pantalla.movil ? 12 : 20), children: [
            Text(t.declararDisponibilidad, style: tema.textTheme.headlineSmall),
            const SizedBox(height: 16),
            contenido,
            if (_error != null) Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: TextStyle(color: tema.colorScheme.error))),
          ]),
        ),
      ),
      Material(
        elevation: 6,
        color: tema.colorScheme.surface,
        child: SafeArea(
          top: false,
          child: ContenidoCentrado(
            anchoMaximo: 960,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              child: Row(mainAxisAlignment: MainAxisAlignment.end, children: [
                TextButton(onPressed: _enviando ? null : _volver, child: Text(t.botonCancelar)),
                const SizedBox(width: 8),
                FilledButton.icon(
                  icon: const Icon(Icons.check),
                  onPressed: _enviando || _hospitales.isEmpty ? null : () => _enviar(tipos),
                  label: Text(t.declarar),
                ),
              ]),
            ),
          ),
        ),
      ),
    ]);
  }
}
