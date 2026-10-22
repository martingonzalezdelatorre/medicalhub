import 'dart:js_interop';
import 'dart:typed_data';

import 'package:web/web.dart' as web;

Future<bool> descargarArchivo(String nombre, Uint8List bytes, String tipoMime) async {
  final blob = web.Blob([bytes.toJS].toJS, web.BlobPropertyBag(type: tipoMime));
  final url = web.URL.createObjectURL(blob);
  final enlace = web.HTMLAnchorElement()
    ..href = url
    ..download = nombre;
  web.document.body!.append(enlace);
  enlace.click();
  enlace.remove();
  web.URL.revokeObjectURL(url);
  return true;
}
