import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';
import '../data/planificacion_repositorio.dart';

/// Color según cobertura: completo, parcial o sin cubrir.
Color colorCobertura(BuildContext context, TurnoDto t) {
  final esquema = Theme.of(context).colorScheme;
  if (t.plazasTotales == 0) return esquema.outline;
  if (t.plazasCubiertas >= t.plazasTotales) return Colors.green.shade600;
  return t.plazasCubiertas == 0 ? esquema.error : Colors.orange.shade700;
}

String franjaTurno(TurnoDto t) => '${t.inicioLocal}–${t.finLocal}${t.cruzaMedianoche ? ' (+1)' : ''}';

class TarjetaTurno extends StatelessWidget {
  const TarjetaTurno({super.key, required this.turno, required this.alPulsar, this.mostrarNombres = false, this.compacta = false});

  final TurnoDto turno;
  final VoidCallback alPulsar;
  final bool mostrarNombres;
  final bool compacta;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final color = colorCobertura(context, turno);
    final nombres = [for (final n in turno.necesidades) ...n.asignaciones.map((a) => a.profesional)];

    return Card(
      margin: EdgeInsets.symmetric(vertical: compacta ? 3 : 5, horizontal: compacta ? 0 : 16),
      clipBehavior: Clip.antiAlias,
      color: Color.alphaBlend(color.withValues(alpha: 0.07), Theme.of(context).colorScheme.surface),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14), side: BorderSide(color: color.withValues(alpha: 0.30))),
      child: InkWell(
        onTap: alPulsar,
        child: Container(
          decoration: BoxDecoration(border: Border(left: BorderSide(color: color, width: 6))),
          padding: EdgeInsets.all(compacta ? 8 : 14),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              if (turno.esGuardia) const Padding(padding: EdgeInsets.only(right: 4), child: Icon(Icons.nightlight_outlined, size: 16)),
              Expanded(
                child: Text(compacta ? turno.tipoTurno : '${turno.tipoTurno} · ${franjaTurno(turno)}',
                    style: Theme.of(context).textTheme.titleSmall, maxLines: 1, overflow: TextOverflow.ellipsis),
              ),
              const SizedBox(width: 4),
              Semantics(
                label: t.plazasCubiertasSemantica(turno.plazasCubiertas, turno.plazasTotales),
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(12)),
                  child: Text('${turno.plazasCubiertas}/${turno.plazasTotales}',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 12)),
                ),
              ),
            ]),
            if (compacta) Text(franjaTurno(turno), style: Theme.of(context).textTheme.bodySmall),
            Text(compacta ? turno.servicio : '${turno.servicio} · ${turno.centro}', style: Theme.of(context).textTheme.bodySmall, maxLines: 1, overflow: TextOverflow.ellipsis),
            if (mostrarNombres && nombres.isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(nombres.join(', '), style: Theme.of(context).textTheme.bodySmall, maxLines: 3, overflow: TextOverflow.ellipsis),
              ),
            if (turno.sinCubrir && turno.plazasTotales > 0)
              Padding(
                padding: const EdgeInsets.only(top: 2),
                child: Text(t.faltanProfesionales(turno.plazasTotales - turno.plazasCubiertas),
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(color: color)),
              ),
          ]),
        ),
      ),
    );
  }
}
