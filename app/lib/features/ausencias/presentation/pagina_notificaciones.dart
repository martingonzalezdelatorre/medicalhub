import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../data/ausencias_repositorio.dart';
import 'etiquetas_ausencias.dart';

/// Pantalla a la que lleva cada tipo de aviso: los dirigidos al profesional van a «Mi disponibilidad», el resto a la gestión.
String rutaDeNotificacion(String tipo) => switch (tipo) {
      'OfertaRecibida' || 'OfertaRetirada' || 'SustitucionAprobada' || 'SustitucionDenegada' || 'AusenciaAprobada' || 'AusenciaRechazada' => '/mi-disponibilidad',
      _ => '/ausencias',
    };

IconData iconoDeNotificacion(String tipo) => switch (tipo) {
      'OfertaRecibida' => Icons.mark_email_unread_outlined,
      'OfertaAceptada' || 'SustitucionAprobada' || 'AusenciaAprobada' => Icons.check_circle_outline,
      'OfertaRechazada' || 'SustitucionDenegada' || 'AusenciaRechazada' => Icons.cancel_outlined,
      'VacanteAbierta' => Icons.person_off_outlined,
      _ => Icons.notifications_outlined,
    };

class PaginaNotificaciones extends ConsumerWidget {
  const PaginaNotificaciones({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final datos = ref.watch(notificacionesProvider);
    final repo = ref.read(ausenciasRepositorioProvider);

    void refrescar() {
      ref.invalidate(notificacionesProvider);
      ref.invalidate(noLeidasProvider);
    }

    Future<void> abrir(NotificacionDto n) async {
      try {
        if (!n.leida) await repo.leer(n.id);
        refrescar();
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
      if (context.mounted) context.go(rutaDeNotificacion(n.tipo));
    }

    Future<void> todasLeidas() async {
      try {
        await repo.leerTodas();
        refrescar();
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
    }

    return EstadoAsincrono(
      valor: datos,
      alReintentar: refrescar,
      constructor: (lista) => lista.isEmpty
          ? EstadoVacio(mensaje: t.sinNotificaciones)
          : ContenidoCentrado(
              child: ListView(padding: const EdgeInsets.symmetric(vertical: 8), children: [
                Align(alignment: Alignment.centerRight, child: TextButton.icon(onPressed: todasLeidas, icon: const Icon(Icons.done_all), label: Text(t.marcarTodasLeidas))),
                for (final n in lista)
                  Card(
                    margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    color: n.leida ? null : Theme.of(context).colorScheme.primaryContainer.withValues(alpha: 0.4),
                    child: ListTile(
                      leading: CircleAvatar(
                        backgroundColor: Theme.of(context).colorScheme.primary.withValues(alpha: n.leida ? 0.08 : 0.18),
                        child: Icon(iconoDeNotificacion(n.tipo), color: Theme.of(context).colorScheme.primary),
                      ),
                      title: Text(n.titulo, style: TextStyle(fontWeight: n.leida ? FontWeight.normal : FontWeight.w700)),
                      subtitle: Text('${n.mensaje}\n${fechaHoraLocal(n.fecha)}'),
                      isThreeLine: true,
                      onTap: () => abrir(n),
                    ),
                  ),
              ]),
            ),
    );
  }
}

/// Campana de la barra superior con el número de avisos sin leer. Se actualiza al navegar y al actuar sobre los avisos.
class CampanaNotificaciones extends ConsumerStatefulWidget {
  const CampanaNotificaciones({super.key, required this.ubicacion});
  final String ubicacion;

  @override
  ConsumerState<CampanaNotificaciones> createState() => _EstadoCampana();
}

class _EstadoCampana extends ConsumerState<CampanaNotificaciones> {
  @override
  void didUpdateWidget(covariant CampanaNotificaciones anterior) {
    super.didUpdateWidget(anterior);
    if (anterior.ubicacion != widget.ubicacion) {
      // No se puede invalidar durante la construcción: se hace al terminar el fotograma.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) ref.invalidate(noLeidasProvider);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final n = ref.watch(noLeidasProvider).value ?? 0;
    return IconButton(
      tooltip: t.notificaciones,
      icon: Badge(isLabelVisible: n > 0, label: Text('$n'), child: const Icon(Icons.notifications_outlined)),
      onPressed: () => context.go('/notificaciones'),
    );
  }
}
