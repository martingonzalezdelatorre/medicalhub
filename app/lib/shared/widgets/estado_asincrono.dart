import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/errores/fallo_api.dart';
import '../../l10n/app_localizations.dart';

/// Gestión uniforme de carga / error / datos para cualquier `AsyncValue`.
class EstadoAsincrono<T> extends StatelessWidget {
  const EstadoAsincrono({
    super.key,
    required this.valor,
    required this.constructor,
    this.alReintentar,
  });

  final AsyncValue<T> valor;
  final Widget Function(T datos) constructor;
  final VoidCallback? alReintentar;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return valor.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) {
        final fallo = FalloApi.desde(e);
        final mensaje = fallo.tipo == TipoFallo.conexion ? t.errorConexion : (fallo.detalle ?? t.errorInesperado);
        return Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Icon(Icons.error_outline, size: 48, color: Theme.of(context).colorScheme.error),
              const SizedBox(height: 12),
              Text(mensaje, textAlign: TextAlign.center),
              if (alReintentar != null) ...[
                const SizedBox(height: 16),
                FilledButton.tonal(onPressed: alReintentar, child: Text(t.reintentar)),
              ],
            ]),
          ),
        );
      },
      data: constructor,
    );
  }
}
