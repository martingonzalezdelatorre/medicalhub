import '../../l10n/app_localizations.dart';

/// Validadores de formulario reutilizables (las reglas de negocio definitivas siempre se validan en el backend).
class Validadores {
  const Validadores(this.t);
  final AppLocalizations t;

  String? obligatorio(String? v) => (v == null || v.trim().isEmpty) ? t.validaObligatorio : null;

  String? correo(String? v) {
    if (obligatorio(v) != null) return t.validaObligatorio;
    final texto = v!.trim();
    final ok = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(texto);
    return ok ? null : t.validaCorreo;
  }

  String? Function(String?) longitudMinima(int n) => (v) {
        if (v == null || v.isEmpty) return t.validaObligatorio;
        return v.length < n ? t.validaLongitudMinima(n) : null;
      };

  String? codigoMfa(String? v) => RegExp(r'^\d{6}$').hasMatch((v ?? '').replaceAll(' ', '')) ? null : t.validaCodigo;
}
