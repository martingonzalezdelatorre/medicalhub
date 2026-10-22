import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/utilidades_ui.dart';

class PaginaLogin extends ConsumerStatefulWidget {
  const PaginaLogin({super.key});

  @override
  ConsumerState<PaginaLogin> createState() => _EstadoPaginaLogin();
}

class _EstadoPaginaLogin extends ConsumerState<PaginaLogin> {
  final _formulario = GlobalKey<FormState>();
  final _correo = TextEditingController();
  final _contrasena = TextEditingController();
  final _codigo = TextEditingController();
  bool _pedirCodigo = false;
  bool _enviando = false;
  String? _error;

  @override
  void dispose() {
    _correo.dispose();
    _contrasena.dispose();
    _codigo.dispose();
    super.dispose();
  }

  Future<void> _entrar() async {
    if (!_formulario.currentState!.validate()) return;
    final t = AppLocalizations.of(context);
    setState(() {
      _enviando = true;
      _error = null;
    });
    try {
      await ref.read(authProvider.notifier).login(
            _correo.text.trim(),
            _contrasena.text,
            codigoMfa: _pedirCodigo ? _codigo.text.replaceAll(' ', '') : null,
          );
    } on FalloApi catch (f) {
      if (!mounted) return;
      setState(() {
        if (f.codigo == 'mfa_requerido') {
          _pedirCodigo = true;
          _error = t.mfaRequeridoAviso;
        } else {
          _error = mensajeDeFallo(t, f);
        }
      });
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final v = Validadores(t);
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Form(
                    key: _formulario,
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(t.tituloApp, style: Theme.of(context).textTheme.headlineMedium, textAlign: TextAlign.center),
                        const SizedBox(height: 4),
                        Text(t.loginTitulo, style: Theme.of(context).textTheme.titleMedium, textAlign: TextAlign.center),
                        const SizedBox(height: 24),
                        TextFormField(
                          controller: _correo,
                          decoration: InputDecoration(labelText: t.campoCorreo, border: const OutlineInputBorder()),
                          keyboardType: TextInputType.emailAddress,
                          autofillHints: const [AutofillHints.username],
                          textInputAction: TextInputAction.next,
                          validator: v.correo,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller: _contrasena,
                          decoration: InputDecoration(labelText: t.campoContrasena, border: const OutlineInputBorder()),
                          obscureText: true,
                          autofillHints: const [AutofillHints.password],
                          textInputAction: _pedirCodigo ? TextInputAction.next : TextInputAction.done,
                          onFieldSubmitted: (_) => _pedirCodigo ? null : _entrar(),
                          validator: v.obligatorio,
                        ),
                        if (_pedirCodigo) ...[
                          const SizedBox(height: 16),
                          TextFormField(
                            controller: _codigo,
                            decoration: InputDecoration(labelText: t.campoCodigoMfa, border: const OutlineInputBorder()),
                            keyboardType: TextInputType.number,
                            autofillHints: const [AutofillHints.oneTimeCode],
                            autofocus: true,
                            onFieldSubmitted: (_) => _entrar(),
                            validator: v.codigoMfa,
                          ),
                        ],
                        if (_error != null) ...[
                          const SizedBox(height: 16),
                          Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                        ],
                        const SizedBox(height: 24),
                        FilledButton(
                          onPressed: _enviando ? null : _entrar,
                          child: _enviando
                              ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                              : Text(t.botonEntrar),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
