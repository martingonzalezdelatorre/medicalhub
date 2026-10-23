import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/seleccion.dart';
import '../data/ausencias_repositorio.dart';

String etiquetaTipoAusencia(AppLocalizations t, TipoAusencia tipo) => switch (tipo) {
      TipoAusencia.vacaciones => t.ausenciaVacaciones,
      TipoAusencia.asuntosPropios => t.ausenciaAsuntosPropios,
      TipoAusencia.permiso => t.ausenciaPermiso,
      TipoAusencia.formacion => t.ausenciaFormacion,
      TipoAusencia.bajaTemporal => t.ausenciaBajaTemporal,
      TipoAusencia.otro => t.ausenciaOtro,
    };

String etiquetaEstadoAusencia(AppLocalizations t, EstadoAusencia e) => switch (e) {
      EstadoAusencia.solicitada => t.ausenciaSolicitada,
      EstadoAusencia.aprobada => t.ausenciaAprobada,
      EstadoAusencia.rechazada => t.ausenciaRechazada,
      EstadoAusencia.cancelada => t.ausenciaCancelada,
    };

Color colorEstadoAusencia(BuildContext context, EstadoAusencia e) => switch (e) {
      EstadoAusencia.solicitada => Colors.orange.shade800,
      EstadoAusencia.aprobada => Colors.green.shade700,
      EstadoAusencia.rechazada => Theme.of(context).colorScheme.error,
      EstadoAusencia.cancelada => Theme.of(context).colorScheme.outline,
    };

String etiquetaTipoDisponibilidad(AppLocalizations t, TipoDisponibilidad tipo) => switch (tipo) {
      TipoDisponibilidad.disponible => t.dispDisponible,
      TipoDisponibilidad.bloqueo => t.dispBloqueo,
      TipoDisponibilidad.prefiere => t.dispPrefiere,
      TipoDisponibilidad.evita => t.dispEvita,
    };

String etiquetaEstadoDisponibilidad(AppLocalizations t, EstadoDisponibilidad e) => switch (e) {
      EstadoDisponibilidad.declarada => t.dispPendiente,
      EstadoDisponibilidad.aprobada => t.ausenciaAprobada,
      EstadoDisponibilidad.rechazada => t.ausenciaRechazada,
    };

const _inicialesDias = ['L', 'M', 'X', 'J', 'V', 'S', 'D'];

/// «Todos los días» o «L, X, V» según la máscara (lunes = 1 … domingo = 64).
String etiquetaDias(AppLocalizations t, int mascara) {
  if (mascara == diasTodos) return t.todosLosDias;
  return [for (var i = 0; i < 7; i++) if (mascara & (1 << i) != 0) _inicialesDias[i]].join(', ');
}

String rangoFechasApi(String desde, String hasta) => desde == hasta ? formatearFecha(desde) : '${formatearFecha(desde)} – ${formatearFecha(hasta)}';

class ChipEstadoAusencia extends StatelessWidget {
  const ChipEstadoAusencia({super.key, required this.etiqueta, required this.color});
  final String etiqueta;
  final Color color;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(12)),
        child: Text(etiqueta, style: TextStyle(color: color, fontWeight: FontWeight.w600)),
      );
}

Future<DateTime?> elegirFecha(BuildContext context, DateTime inicial) =>
    showDatePicker(context: context, initialDate: inicial, firstDate: DateTime(2020), lastDate: DateTime(2100));
