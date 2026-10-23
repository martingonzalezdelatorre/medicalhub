import 'package:flutter/material.dart';

import '../../l10n/app_localizations.dart';

/// Longitud mínima de la justificación (la API aplica la misma regla).
const minimoJustificacion = 10;

/// Pide la justificación de una excepción mostrando antes las infracciones que se van a saltar.
/// Devuelve el texto o null si se cancela.
Future<String?> pedirJustificacion(BuildContext context, {required String titulo, required List<String> infracciones}) {
  return showDialog<String>(context: context, builder: (_) => _DialogoJustificacion(titulo: titulo, infracciones: infracciones));
}

class _DialogoJustificacion extends StatefulWidget {
  const _DialogoJustificacion({required this.titulo, required this.infracciones});
  final String titulo;
  final List<String> infracciones;

  @override
  State<_DialogoJustificacion> createState() => _EstadoDialogoJustificacion();
}

class _EstadoDialogoJustificacion extends State<_DialogoJustificacion> {
  final _formulario = GlobalKey<FormState>();
  final _texto = TextEditingController();

  @override
  void dispose() {
    _texto.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return AlertDialog(
      title: Text(widget.titulo),
      content: SizedBox(
        width: 460,
        child: SingleChildScrollView(
          child: Form(
            key: _formulario,
            child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(t.excepcionAviso),
              const SizedBox(height: 8),
              for (final i in widget.infracciones)
                Padding(
                  padding: const EdgeInsets.only(bottom: 4),
                  child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Icon(Icons.warning_amber_outlined, size: 18, color: Colors.orange.shade800),
                    const SizedBox(width: 6),
                    Expanded(child: Text(i)),
                  ]),
                ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _texto,
                autofocus: true,
                minLines: 3,
                maxLines: 5,
                maxLength: 1000,
                decoration: InputDecoration(labelText: t.justificacion, border: const OutlineInputBorder()),
                validator: (v) => (v ?? '').trim().length < minimoJustificacion ? t.justificacionMinimo(minimoJustificacion) : null,
              ),
            ]),
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(
          onPressed: () {
            if (_formulario.currentState!.validate()) Navigator.pop(context, _texto.text.trim());
          },
          child: Text(t.autorizarExcepcion),
        ),
      ],
    );
  }
}
