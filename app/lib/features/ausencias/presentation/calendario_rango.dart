import 'package:flutter/material.dart';

const _meses = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];
const _inicialesDias = ['L', 'M', 'X', 'J', 'V', 'S', 'D'];

DateTime _dia(DateTime f) => DateTime(f.year, f.month, f.day);

/// Calendario mensual para elegir un periodo: el primer toque marca el inicio y el segundo el fin
/// (un solo día si se toca dos veces el mismo).
class CalendarioRango extends StatefulWidget {
  const CalendarioRango({super.key, required this.desde, required this.hasta, required this.alCambiar});

  final DateTime desde;
  final DateTime hasta;
  final void Function(DateTime desde, DateTime hasta) alCambiar;

  @override
  State<CalendarioRango> createState() => _EstadoCalendario();
}

class _EstadoCalendario extends State<CalendarioRango> {
  late DateTime _mes = DateTime(widget.desde.year, widget.desde.month);

  /// Con el inicio ya marcado, el siguiente toque cierra el rango.
  bool _esperandoFin = false;

  @override
  void didUpdateWidget(CalendarioRango anterior) {
    super.didUpdateWidget(anterior);
    // Un atajo de periodo (semana, mes…) mueve el inicio: el calendario muestra ese mes.
    if (widget.desde != anterior.desde) _mes = DateTime(widget.desde.year, widget.desde.month);
  }

  void _tocar(DateTime d) {
    final desde = _dia(widget.desde);
    if (!_esperandoFin) {
      setState(() => _esperandoFin = true);
      widget.alCambiar(d, d);
    } else {
      setState(() => _esperandoFin = false);
      d.isBefore(desde) ? widget.alCambiar(d, desde) : widget.alCambiar(desde, d);
    }
  }

  void _cambiarMes(int delta) => setState(() => _mes = DateTime(_mes.year, _mes.month + delta));

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final desde = _dia(widget.desde);
    final hasta = _dia(widget.hasta);
    final hoy = _dia(DateTime.now());
    final primero = DateTime(_mes.year, _mes.month);
    final huecos = primero.weekday - 1;
    final diasMes = DateTime(_mes.year, _mes.month + 1, 0).day;
    final filas = ((huecos + diasMes) / 7).ceil();

    Widget celda(int i) {
      final n = i - huecos + 1;
      if (n < 1 || n > diasMes) return const SizedBox.shrink();
      final d = DateTime(_mes.year, _mes.month, n);
      final extremo = d == desde || d == hasta;
      final dentro = !d.isBefore(desde) && !d.isAfter(hasta);
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: InkResponse(
          onTap: () => _tocar(d),
          child: Container(
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: extremo ? cs.primary : dentro ? cs.primary.withAlpha(36) : null,
              shape: extremo ? BoxShape.circle : BoxShape.rectangle,
              border: d == hoy && !extremo ? Border.all(color: cs.primary) : null,
            ),
            child: Text(
              '$n',
              style: TextStyle(color: extremo ? cs.onPrimary : null, fontWeight: extremo ? FontWeight.w600 : null),
            ),
          ),
        ),
      );
    }

    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      Row(children: [
        IconButton(visualDensity: VisualDensity.compact, icon: const Icon(Icons.chevron_left), onPressed: () => _cambiarMes(-1)),
        Expanded(
          child: Text('${_meses[_mes.month - 1]} ${_mes.year}', textAlign: TextAlign.center, style: Theme.of(context).textTheme.titleSmall),
        ),
        IconButton(visualDensity: VisualDensity.compact, icon: const Icon(Icons.chevron_right), onPressed: () => _cambiarMes(1)),
      ]),
      Row(children: [
        for (final l in _inicialesDias)
          Expanded(child: Center(child: Text(l, style: Theme.of(context).textTheme.labelSmall?.copyWith(color: cs.onSurfaceVariant)))),
      ]),
      const SizedBox(height: 4),
      for (var f = 0; f < filas; f++)
        SizedBox(
          height: 38,
          child: Row(children: [for (var c = 0; c < 7; c++) Expanded(child: celda(f * 7 + c))]),
        ),
    ]);
  }
}
