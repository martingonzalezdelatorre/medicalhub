import 'package:flutter/material.dart';

import '../../core/auth/modelos.dart';
import '../../core/errores/fallo_api.dart';
import '../../core/tema/tema.dart';
import '../../l10n/app_localizations.dart';

String etiquetaRol(AppLocalizations t, Rol rol) => switch (rol) {
      Rol.administradorPlataforma => t.rolAdministradorPlataforma,
      Rol.administradorSociedad => t.rolAdministradorSociedad,
      Rol.coordinadorMedico => t.rolCoordinadorMedico,
      Rol.profesionalSanitario => t.rolProfesionalSanitario,
      Rol.responsableCentro => t.rolResponsableCentro,
      Rol.recursosHumanos => t.rolRecursosHumanos,
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

/// Estado vacío con icono, para listas sin elementos.
class EstadoVacio extends StatelessWidget {
  const EstadoVacio({super.key, required this.mensaje, this.icono = Icons.inbox_outlined});
  final String mensaje;
  final IconData icono;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Container(
            width: 72,
            height: 72,
            decoration: BoxDecoration(color: cs.primary.withAlpha(24), shape: BoxShape.circle),
            child: Icon(icono, size: 34, color: cs.primary),
          ),
          const SizedBox(height: 16),
          Text(mensaje, textAlign: TextAlign.center, style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
        ]),
      ),
    );
  }
}

/// Tarjeta de elemento de lista: avatar con inicial, título, subtítulo y flecha.
class TarjetaElemento extends StatelessWidget {
  const TarjetaElemento({super.key, required this.titulo, this.subtitulo, this.inicial, this.icono, this.trailing, this.onTap, this.tachado = false});
  final String titulo;
  final String? subtitulo;
  final String? inicial;
  final IconData? icono;
  final Widget? trailing;
  final VoidCallback? onTap;
  final bool tachado;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final cs = tema.colorScheme;
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4),
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          child: Row(children: [
            Container(
              width: 42,
              height: 42,
              alignment: Alignment.center,
              decoration: BoxDecoration(color: cs.primary.withAlpha(24), borderRadius: BorderRadius.circular(12)),
              child: inicial != null
                  ? Text(inicial!, style: tema.textTheme.titleMedium?.copyWith(color: cs.primary, fontWeight: FontWeight.w700))
                  : Icon(icono ?? Icons.circle_outlined, color: cs.primary, size: 22),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(titulo,
                    style: tema.textTheme.titleSmall?.copyWith(decoration: tachado ? TextDecoration.lineThrough : null)),
                if (subtitulo != null && subtitulo!.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(subtitulo!, style: tema.textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                  ),
              ]),
            ),
            trailing ?? (onTap != null ? Icon(Icons.chevron_right, color: cs.onSurfaceVariant) : const SizedBox.shrink()),
          ]),
        ),
      ),
    );
  }
}

/// Contenedor centrado con ancho máximo, para formularios y listas en pantallas anchas.
class ContenidoCentrado extends StatelessWidget {
  const ContenidoCentrado({super.key, required this.child, this.anchoMaximo = 720});
  final Widget child;
  final double anchoMaximo;

  @override
  Widget build(BuildContext context) {
    final ancho = MediaQuery.sizeOf(context).width;
    // En pantallas grandes se amplía el máximo para aprovechar el espacio sin perder legibilidad.
    final maximo = ancho >= Pantalla.escritorio ? anchoMaximo * 1.25 : anchoMaximo;
    return Align(
      alignment: Alignment.topCenter,
      child: ConstrainedBox(constraints: BoxConstraints(maxWidth: maximo), child: child),
    );
  }
}
