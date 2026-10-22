import 'package:flutter/material.dart';

import '../../core/auth/modelos.dart';
import '../../core/errores/fallo_api.dart';
import '../../l10n/app_localizations.dart';

String etiquetaRol(AppLocalizations t, Rol rol) => switch (rol) {
      Rol.administradorPlataforma => t.rolAdministradorPlataforma,
      Rol.administradorSociedad => t.rolAdministradorSociedad,
      Rol.coordinadorMedico => t.rolCoordinadorMedico,
      Rol.profesionalSanitario => t.rolProfesionalSanitario,
      Rol.responsableCentro => t.rolResponsableCentro,
    };

/// Mensaje comprensible para el usuario a partir de un fallo de la API.
String mensajeDeFallo(AppLocalizations t, Object error) {
  final f = FalloApi.desde(error);
  return f.tipo == TipoFallo.conexion ? t.errorConexion : (f.detalle ?? t.errorInesperado);
}

void mostrarMensaje(BuildContext context, String texto) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(texto)));
}

/// Contenedor centrado con ancho máximo, para formularios y listas en pantallas anchas.
class ContenidoCentrado extends StatelessWidget {
  const ContenidoCentrado({super.key, required this.child, this.anchoMaximo = 720});
  final Widget child;
  final double anchoMaximo;

  @override
  Widget build(BuildContext context) => Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(constraints: BoxConstraints(maxWidth: anchoMaximo), child: child),
      );
}
