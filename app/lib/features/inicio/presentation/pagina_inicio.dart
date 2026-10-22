import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../application/info_sistema_provider.dart';

class PaginaInicio extends ConsumerWidget {
  const PaginaInicio({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final info = ref.watch(infoSistemaProvider);
    return Scaffold(
      appBar: AppBar(title: Text(t.tituloApp)),
      body: EstadoAsincrono(
        valor: info,
        alReintentar: () => ref.invalidate(infoSistemaProvider),
        constructor: (datos) => Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: Card(
              margin: const EdgeInsets.all(16),
              child: ListTile(
                leading: const Icon(Icons.cloud_done_outlined),
                title: Text(datos.nombre),
                subtitle: Text(t.versionEntorno(datos.version, datos.entorno)),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
