import 'dart:typed_data';

import 'descargar_archivo_stub.dart' if (dart.library.js_interop) 'descargar_archivo_web.dart' as impl;

/// Entrega un archivo al usuario. En web inicia la descarga del navegador; devuelve false si la plataforma
/// no lo soporta (en ese caso la interfaz ofrece copiar el contenido).
Future<bool> descargarArchivo(String nombre, Uint8List bytes, String tipoMime) => impl.descargarArchivo(nombre, bytes, tipoMime);
