import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class OpcionSeleccion {
  const OpcionSeleccion(this.id, this.etiqueta);
  final String id;
  final String etiqueta;
}

/// Selección múltiple con chips (válida en móvil y escritorio).
class ChipsSeleccion extends StatelessWidget {
  const ChipsSeleccion({
    super.key,
    required this.etiqueta,
    required this.opciones,
    required this.seleccion,
    required this.alCambiar,
  });

  final String etiqueta;
  final List<OpcionSeleccion> opciones;
  final Set<String> seleccion;
  final ValueChanged<Set<String>> alCambiar;

  @override
  Widget build(BuildContext context) {
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Padding(
        padding: const EdgeInsets.only(top: 12, bottom: 4),
        child: Text(etiqueta, style: Theme.of(context).textTheme.labelLarge),
      ),
      Wrap(spacing: 8, runSpacing: 4, children: [
        for (final o in opciones)
          FilterChip(
            label: Text(o.etiqueta),
            selected: seleccion.contains(o.id),
            onSelected: (marcado) {
              final nueva = {...seleccion};
              marcado ? nueva.add(o.id) : nueva.remove(o.id);
              alCambiar(nueva);
            },
          ),
      ]),
    ]);
  }
}

/// Fecha ISO (yyyy-MM-dd) → dd/MM/yyyy.
String formatearFecha(String iso) => DateFormat('dd/MM/yyyy').format(DateTime.parse(iso));

String aHoraApi(TimeOfDay t) => '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

TimeOfDay desdeHoraApi(String h) => TimeOfDay(hour: int.parse(h.substring(0, 2)), minute: int.parse(h.substring(3, 5)));

/// Selector de hora en formato de 24 horas.
Future<TimeOfDay?> elegirHora(BuildContext context, TimeOfDay inicial) => showTimePicker(
      context: context,
      initialTime: inicial,
      builder: (c, child) => MediaQuery(data: MediaQuery.of(c).copyWith(alwaysUse24HourFormat: true), child: child!),
    );

const zonasHorarias = ['Europe/Madrid', 'Atlantic/Canary', 'Africa/Ceuta'];

/// Cabecera de sección con acción opcional.
class CabeceraSeccion extends StatelessWidget {
  const CabeceraSeccion({super.key, required this.titulo, this.accion});
  final String titulo;
  final Widget? accion;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(16, 20, 16, 4),
        child: Row(children: [
          Expanded(child: Text(titulo, style: Theme.of(context).textTheme.titleMedium)),
          ?accion,
        ]),
      );
}
