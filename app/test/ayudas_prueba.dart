import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/misc.dart';
import 'package:medicalhub_app/core/api/cliente_api.dart';
import 'package:medicalhub_app/core/auth/almacen_tokens.dart';

typedef Responder = ResponseBody Function(RequestOptions peticion);

/// Servidor HTTP simulado para pruebas de la app.
class ServidorFalso implements HttpClientAdapter {
  ServidorFalso(this.responder);

  final Responder responder;
  final peticiones = <RequestOptions>[];

  int contar(String metodo, String ruta) => peticiones.where((p) => p.method == metodo && p.path == ruta).length;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    peticiones.add(options);
    return responder(options);
  }

  @override
  void close({bool force = false}) {}

  List<Override> sobrescrituras({AlmacenTokens? almacen}) => [
        adaptadorHttpProvider.overrideWithValue(this),
        almacenTokensProvider.overrideWithValue(almacen ?? AlmacenTokensMemoria()),
      ];
}

ResponseBody json(Object datos, [int estado = 200]) =>
    ResponseBody.fromString(jsonEncode(datos), estado, headers: {
      Headers.contentTypeHeader: ['application/json'],
    });

Map<String, dynamic> sesionJson({
  String acceso = 'ACCESO-1',
  String refresco = 'REFRESCO-1',
  List<String> roles = const ['AdministradorSociedad'],
  bool mfaPendiente = false,
  String nombre = 'Ana García',
}) =>
    {
      'tokenAcceso': acceso,
      'accesoExpira': '2026-10-22T10:00:00+00:00',
      'tokenRefresco': refresco,
      'refrescoExpira': '2026-10-29T10:00:00+00:00',
      'mfaPendiente': mfaPendiente,
      'perfil': {
        'id': '11111111-1111-1111-1111-111111111111',
        'organizacionId': '22222222-2222-2222-2222-222222222222',
        'organizacion': 'Anestesia Madrid',
        'email': 'ana@sociedad.es',
        'nombreCompleto': nombre,
        'roles': roles,
        'mfaActivado': !mfaPendiente,
        'mfaPendiente': mfaPendiente,
        'zonaHoraria': 'Europe/Madrid',
      },
    };

Map<String, dynamic> problema(int estado, String detalle, {String? codigo}) =>
    {'status': estado, 'title': 'Error', 'detail': detalle, 'codigo': ?codigo};
