import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/utilidades_ui.dart';

class PaginaInicio extends ConsumerWidget {
  const PaginaInicio({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final perfil = ref.watch(authProvider).perfil;
    if (perfil == null) return const SizedBox.shrink();

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: ContenidoCentrado(
        anchoMaximo: 560,
        child: Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(t.bienvenida(perfil.nombreCompleto), style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: 4),
              Text(perfil.email),
              const Divider(height: 32),
              if (perfil.organizacion != null) _Dato(etiqueta: t.perfilSociedad, valor: perfil.organizacion!),
              _Dato(etiqueta: t.perfilRoles, valor: perfil.roles.map((r) => etiquetaRol(t, r)).join(', ')),
              _Dato(etiqueta: t.perfilMfa, valor: perfil.mfaActivado ? t.mfaActiva : t.mfaInactiva),
            ]),
          ),
        ),
      ),
    );
  }
}

class _Dato extends StatelessWidget {
  const _Dato({required this.etiqueta, required this.valor});
  final String etiqueta;
  final String valor;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          SizedBox(width: 120, child: Text(etiqueta, style: Theme.of(context).textTheme.labelLarge)),
          Expanded(child: Text(valor)),
        ]),
      );
}
