import 'package:flutter/material.dart';

import '../../../shared/widgets/seleccion.dart';
import 'etiquetas_ausencias.dart' show elegirFecha;
import '../../../l10n/app_localizations.dart';
import '../../planificacion/data/planificacion_repositorio.dart' show fechaParaApi;
import 'calendario_rango.dart';

/// Tarjeta con icono y título para agrupar los campos de un formulario de pantalla completa.
class TarjetaFormulario extends StatelessWidget {
  const TarjetaFormulario({super.key, required this.icono, required this.titulo, required this.child, this.accion});

  final IconData icono;
  final String titulo;
  final Widget child;
  final Widget? accion;

  @override
  Widget build(BuildContext context) {
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
          child,
        ]),
      ),
    );
  }
}

/// Elección de un periodo: calendario y, debajo, las dos fechas con su selector.
class SelectorPeriodo extends StatelessWidget {
  const SelectorPeriodo({super.key, required this.desde, required this.hasta, required this.alCambiar});

  final DateTime desde;
  final DateTime hasta;
  final void Function(DateTime desde, DateTime hasta) alCambiar;

  Widget _fecha(BuildContext context, String etiqueta, DateTime valor, ValueChanged<DateTime> alElegir) => OutlinedButton.icon(
        icon: const Icon(Icons.calendar_today_outlined, size: 18),
        style: OutlinedButton.styleFrom(alignment: Alignment.centerLeft, padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 16)),
        onPressed: () async {
          final f = await elegirFecha(context, valor);
          if (f != null) alElegir(f);
        },
        label: Text('$etiqueta: ${formatearFecha(fechaParaApi(valor))}', overflow: TextOverflow.ellipsis),
      );

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final d = _fecha(context, t.campoDesde, desde, (f) => alCambiar(f, hasta.isBefore(f) ? f : hasta));
    final h = _fecha(context, t.campoHasta, hasta, (f) => alCambiar(desde, f.isBefore(desde) ? desde : f));
    return LayoutBuilder(builder: (context, c) {
      final fechas = c.maxWidth >= 420
          ? Row(children: [Expanded(child: d), const SizedBox(width: 12), Expanded(child: h)])
          : Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [d, const SizedBox(height: 10), h]);
      return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        CalendarioRango(desde: desde, hasta: hasta, alCambiar: alCambiar),
        const SizedBox(height: 12),
        fechas,
      ]);
    });
  }
}
