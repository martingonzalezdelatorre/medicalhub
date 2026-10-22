import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/auth/modelos.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';

/// Se mantiene viva mientras la página esté abierta para no regenerar el secreto en cada reconstrucción.
final configuracionMfaProvider = FutureProvider.autoDispose<ConfiguracionMfa>(
  (ref) => ref.read(authProvider.notifier).configurarMfa(),
);

class PaginaMfa extends ConsumerStatefulWidget {
  const PaginaMfa({super.key});

  @override
  ConsumerState<PaginaMfa> createState() => _EstadoPaginaMfa();
}

class _EstadoPaginaMfa extends ConsumerState<PaginaMfa> {
  final _formulario = GlobalKey<FormState>();
  final _codigo = TextEditingController();
  bool _enviando = false;

  @override
  void dispose() {
    _codigo.dispose();
    super.dispose();
  }

  Future<void> _activar() async {
    if (!_formulario.currentState!.validate()) return;
    final t = AppLocalizations.of(context);
    setState(() => _enviando = true);
    try {
      await ref.read(authProvider.notifier).activarMfa(_codigo.text.replaceAll(' ', ''));
    } catch (e) {
      if (mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final config = ref.watch(configuracionMfaProvider);
    return Scaffold(
      appBar: AppBar(
        title: Text(t.mfaTitulo),
        actions: [TextButton(onPressed: () => ref.read(authProvider.notifier).cerrarSesion(), child: Text(t.cerrarSesion))],
      ),
      body: EstadoAsincrono(
        valor: config,
        alReintentar: () => ref.invalidate(configuracionMfaProvider),
        constructor: (cfg) => SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: ContenidoCentrado(
            anchoMaximo: 480,
            child: Form(
              key: _formulario,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(t.mfaInstrucciones),
                  const SizedBox(height: 16),
                  Center(
                    child: Container(
                      color: Colors.white,
                      padding: const EdgeInsets.all(8),
                      child: QrImageView(data: cfg.uriOtpAuth, size: 200),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Text(t.mfaClaveManual, style: Theme.of(context).textTheme.labelLarge),
                  Row(children: [
                    Expanded(child: SelectableText(cfg.secreto, style: const TextStyle(fontFamily: 'monospace'))),
                    IconButton(
                      tooltip: t.botonCopiar,
                      icon: const Icon(Icons.copy),
                      onPressed: () async {
                        await Clipboard.setData(ClipboardData(text: cfg.secreto));
                        if (context.mounted) mostrarMensaje(context, t.copiado);
                      },
                    ),
                  ]),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _codigo,
                    decoration: InputDecoration(labelText: t.campoCodigoMfa, border: const OutlineInputBorder()),
                    keyboardType: TextInputType.number,
                    autofillHints: const [AutofillHints.oneTimeCode],
                    onFieldSubmitted: (_) => _activar(),
                    validator: Validadores(t).codigoMfa,
                  ),
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: _enviando ? null : _activar,
                    child: _enviando
                        ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                        : Text(t.botonActivar),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
