import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/auth_controlador.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import 'fondo_login.dart';

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
  bool _ocultar = true;
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

  InputDecoration _decoracion(String etiqueta, IconData icono, {Widget? sufijo}) => InputDecoration(
        labelText: etiqueta,
        prefixIcon: Icon(icono),
        suffixIcon: sufijo,
        filled: true,
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
      );

  Widget _marca(BuildContext context, AppLocalizations t, {required bool grande}) {
    final tema = Theme.of(context);
    final estiloTitulo = grande ? tema.textTheme.displaySmall : tema.textTheme.headlineMedium;
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          padding: EdgeInsets.all(grande ? 22 : 16),
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: Colors.white.withValues(alpha: 0.18),
            border: Border.all(color: Colors.white.withValues(alpha: 0.5), width: 2),
          ),
          child: Icon(Icons.monitor_heart_outlined, size: grande ? 64 : 44, color: Colors.white),
        ),
        SizedBox(height: grande ? 24 : 12),
        Text(t.tituloApp, style: estiloTitulo?.copyWith(color: Colors.white, fontWeight: FontWeight.w700), textAlign: TextAlign.center),
        const SizedBox(height: 4),
        Text(t.loginTitulo, style: tema.textTheme.titleMedium?.copyWith(color: Colors.white.withValues(alpha: 0.9)), textAlign: TextAlign.center),
      ],
    );
  }

  Widget _tarjeta(BuildContext context, AppLocalizations t, {required bool mostrarTitulo}) {
    final v = Validadores(t);
    final esquema = Theme.of(context).colorScheme;
    return Card(
      elevation: 8,
      shadowColor: Colors.black38,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      child: Padding(
        padding: const EdgeInsets.all(28),
        child: Form(
          key: _formulario,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (mostrarTitulo) ...[
                Text(t.loginTitulo, style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700), textAlign: TextAlign.center),
                const SizedBox(height: 24),
              ],
              TextFormField(
                controller: _correo,
                decoration: _decoracion(t.campoCorreo, Icons.alternate_email),
                keyboardType: TextInputType.emailAddress,
                autofillHints: const [AutofillHints.username],
                textInputAction: TextInputAction.next,
                validator: v.correo,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _contrasena,
                decoration: _decoracion(
                  t.campoContrasena,
                  Icons.lock_outline,
                  sufijo: IconButton(
                    icon: Icon(_ocultar ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                    onPressed: () => setState(() => _ocultar = !_ocultar),
                  ),
                ),
                obscureText: _ocultar,
                autofillHints: const [AutofillHints.password],
                textInputAction: _pedirCodigo ? TextInputAction.next : TextInputAction.done,
                onFieldSubmitted: (_) => _pedirCodigo ? null : _entrar(),
                validator: v.obligatorio,
              ),
              AnimatedSize(
                duration: const Duration(milliseconds: 300),
                curve: Curves.easeOutCubic,
                alignment: Alignment.topCenter,
                child: _pedirCodigo
                    ? Padding(
                        padding: const EdgeInsets.only(top: 16),
                        child: TextFormField(
                          controller: _codigo,
                          decoration: _decoracion(t.campoCodigoMfa, Icons.shield_outlined),
                          keyboardType: TextInputType.number,
                          autofillHints: const [AutofillHints.oneTimeCode],
                          autofocus: true,
                          onFieldSubmitted: (_) => _entrar(),
                          validator: v.codigoMfa,
                        ),
                      )
                    : const SizedBox(width: double.infinity),
              ),
              AnimatedSize(
                duration: const Duration(milliseconds: 250),
                alignment: Alignment.topCenter,
                child: _error == null
                    ? const SizedBox(width: double.infinity)
                    : Container(
                        margin: const EdgeInsets.only(top: 16),
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(color: esquema.errorContainer, borderRadius: BorderRadius.circular(12)),
                        child: Row(
                          children: [
                            Icon(Icons.error_outline, color: esquema.onErrorContainer),
                            const SizedBox(width: 8),
                            Expanded(child: Text(_error!, style: TextStyle(color: esquema.onErrorContainer))),
                          ],
                        ),
                      ),
              ),
              const SizedBox(height: 24),
              SizedBox(
                height: 52,
                child: FilledButton(
                  style: FilledButton.styleFrom(shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14))),
                  onPressed: _enviando ? null : _entrar,
                  child: AnimatedSwitcher(
                    duration: const Duration(milliseconds: 200),
                    child: _enviando
                        ? const SizedBox(key: ValueKey('c'), height: 22, width: 22, child: CircularProgressIndicator(strokeWidth: 2.5))
                        : Text(t.botonEntrar, key: const ValueKey('t'), style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Scaffold(
      body: FondoLoginAnimado(
        child: LayoutBuilder(
          builder: (context, c) {
            final ancho = c.maxWidth >= 900;
            final contenido = ancho
                ? Row(
                    children: [
                      Expanded(child: Center(child: _Entrada(retraso: 0, desdeX: -40, child: _marca(context, t, grande: true)))),
                      Expanded(
                        child: Center(
                          child: SingleChildScrollView(
                            padding: const EdgeInsets.all(32),
                            child: ConstrainedBox(
                              constraints: const BoxConstraints(maxWidth: 440),
                              child: _Entrada(retraso: 150, desdeX: 40, child: _tarjeta(context, t, mostrarTitulo: true)),
                            ),
                          ),
                        ),
                      ),
                    ],
                  )
                : Center(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.all(16),
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 440),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            _Entrada(retraso: 0, desdeY: -24, child: _marca(context, t, grande: false)),
                            const SizedBox(height: 24),
                            _Entrada(retraso: 150, desdeY: 40, child: _tarjeta(context, t, mostrarTitulo: false)),
                          ],
                        ),
                      ),
                    ),
                  );
            return SafeArea(child: contenido);
          },
        ),
      ),
    );
  }
}

/// Aparición con desvanecimiento y desplazamiento.
class _Entrada extends StatelessWidget {
  const _Entrada({required this.child, this.retraso = 0, this.desdeX = 0, this.desdeY = 0});

  final Widget child;
  final int retraso;
  final double desdeX;
  final double desdeY;

  @override
  Widget build(BuildContext context) {
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: Duration(milliseconds: 700 + retraso),
      curve: Interval(retraso / (700 + retraso), 1, curve: Curves.easeOutCubic),
      builder: (_, v, hijo) => Opacity(
        opacity: v,
        child: Transform.translate(offset: Offset(desdeX * (1 - v), desdeY * (1 - v)), child: hijo),
      ),
      child: child,
    );
  }
}
