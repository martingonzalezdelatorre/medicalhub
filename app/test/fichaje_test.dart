import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:medicalhub_app/core/auth/almacen_tokens.dart';
import 'package:medicalhub_app/features/fichaje/data/servicio_ubicacion.dart';
import 'package:medicalhub_app/main.dart';

import 'ayudas_prueba.dart';

class _UbicacionFalsa implements ServicioUbicacion {
  _UbicacionFalsa(this.resultado);
  final UbicacionDispositivo? resultado;
  int llamadas = 0;

  @override
  Future<ResultadoUbicacion> obtener() async {
    llamadas++;
    return ResultadoUbicacion(resultado);
  }
}

Map<String, dynamic> _fichaje(String estado, {bool? fuera, bool sinUbicacion = false, List<Map<String, dynamic>> pausas = const []}) {
  final inicio = DateTime.now().toUtc().subtract(const Duration(hours: 2));
  return {
    'id': 'f1', 'asignacionId': 'a1', 'estado': estado, 'turno': 'Mañana 08:00–15:00 · Quirófano', 'centro': 'Hospital Norte', 'centroConGeovalla': true,
    'inicioUtc': inicio.toIso8601String(), 'finUtc': estado == 'Cerrado' ? DateTime.now().toUtc().toIso8601String() : null,
    'minutosPausa': 0, 'minutosTrabajados': 120, 'entradaFueraDeZona': fuera, 'salidaFueraDeZona': null, 'entradaSinUbicacion': sinUbicacion,
    'salidaSinUbicacion': null, 'pausas': pausas,
  };
}

final _fichable = {
  'asignacionId': 'a1', 'turno': 'Mañana 08:00–15:00 · Quirófano', 'centro': 'Hospital Norte', 'centroConGeovalla': true,
  'inicioUtc': DateTime.now().toUtc().toIso8601String(), 'finUtc': DateTime.now().toUtc().add(const Duration(hours: 6)).toIso8601String(),
};

class _Servidor {
  _Servidor({this.fichable = true, this.fuera});
  final bool fichable;
  final bool? fuera;
  Map<String, dynamic>? actual;
  final cuerpos = <String, Object?>{};

  late final ServidorFalso falso = ServidorFalso((p) {
    switch ((p.method, p.path)) {
      case (_, '/api/autenticacion/refrescar'):
        return json(sesionJson(roles: ['ProfesionalSanitario']));
      case ('GET', '/api/fichajes/estado'):
        return json({'actual': actual, 'fichable': actual == null && fichable ? _fichable : null});
      case ('GET', '/api/mis-fichajes'):
        return json(actual == null ? [] : [actual]);
      case ('GET', '/api/dashboard'):
        return json({'gestion': null, 'profesional': {'proximosTurnos': [], 'horasPlanificadasMesMin': 0, 'ofertasPendientes': 0, 'ausenciasSolicitadas': 0}});
      case ('GET', '/api/mis-horas'):
        return json([]);
      case ('POST', '/api/fichajes/entrada'):
        cuerpos['entrada'] = p.data;
        return json(actual = _fichaje('EnCurso', fuera: fuera, sinUbicacion: (p.data as Map)['latitud'] == null));
      case ('POST', '/api/fichajes/pausa'):
        return json(actual = _fichaje('EnPausa', pausas: [
          {'inicioUtc': DateTime.now().toUtc().toIso8601String(), 'finUtc': null}
        ]));
      case ('POST', '/api/fichajes/reanudar'):
        return json(actual = _fichaje('EnCurso'));
      case ('POST', '/api/fichajes/salida'):
        cuerpos['salida'] = p.data;
        final r = _fichaje('Cerrado');
        actual = null;
        return json(r);
      default:
        return json(problema(404, 'No encontrado'), 404);
    }
  });
}

Future<void> _montar(WidgetTester t, _Servidor s, _UbicacionFalsa ubicacion) async {
  await initializeDateFormatting('es_ES');
  t.view.physicalSize = const Size(500, 1000);
  t.view.devicePixelRatio = 1;
  addTearDown(t.view.reset);
  final almacen = AlmacenTokensMemoria()..valor = 'R';
  await t.pumpWidget(ProviderScope(
    overrides: [...s.falso.sobrescrituras(almacen: almacen), servicioUbicacionProvider.overrideWithValue(ubicacion)],
    child: const MedicalHubApp(),
  ));
  await t.pumpAndSettle();
  // Navega a la pantalla de fichaje desde el menú
  await t.tap(find.byIcon(Icons.fingerprint).first);
  await t.pumpAndSettle();
}

void main() {
  testWidgets('el profesional ficha entrada con su ubicación, hace una pausa y ficha la salida', (t) async {
    final s = _Servidor();
    final ubi = _UbicacionFalsa(const UbicacionDispositivo(latitud: 40.4168, longitud: -3.7038, precisionMetros: 8));
    await _montar(t, s, ubi);

    expect(find.text('Fichar entrada'), findsOneWidget);
    await t.tap(find.text('Fichar entrada'));
    await t.pumpAndSettle();

    final cuerpo = s.cuerpos['entrada'] as Map;
    expect(cuerpo['latitud'], 40.4168);
    expect(cuerpo['longitud'], -3.7038);
    expect(cuerpo['precisionMetros'], 8);
    expect(find.text('Jornada en curso'), findsOneWidget);
    expect(find.text('Fichar entrada'), findsNothing);

    await t.tap(find.text('Pausa'));
    await t.pumpAndSettle();
    expect(find.text('En pausa'), findsOneWidget);
    expect(find.text('Reanudar'), findsOneWidget);

    await t.tap(find.text('Reanudar'));
    await t.pumpAndSettle();
    expect(find.text('Jornada en curso'), findsOneWidget);

    await t.tap(find.text('Fichar salida'));
    await t.pumpAndSettle();
    expect((s.cuerpos['salida'] as Map)['latitud'], 40.4168);
    expect(find.text('Jornada en curso'), findsNothing);   // la jornada ya está cerrada (el servidor simulado vuelve a ofrecer el turno)
    expect(ubi.llamadas, 2);                             // solo entrada y salida piden la ubicación
  });

  testWidgets('fuera de zona se avisa al profesional sin impedir el fichaje', (t) async {
    final s = _Servidor(fuera: true);
    await _montar(t, s, _UbicacionFalsa(const UbicacionDispositivo(latitud: 41, longitud: 2)));
    await t.tap(find.text('Fichar entrada'));
    await t.pumpAndSettle();
    expect(find.text('Se ha registrado fuera de la zona del centro. La coordinación podrá revisarlo.'), findsOneWidget);
    expect(find.text('Jornada en curso'), findsOneWidget);
  });

  testWidgets('sin ubicación hay que confirmar y se ficha sin coordenadas; al cancelar no se ficha', (t) async {
    final s = _Servidor();
    final ubi = _UbicacionFalsa(null);
    await _montar(t, s, ubi);

    await t.tap(find.text('Fichar entrada'));
    await t.pumpAndSettle();
    expect(find.textContaining('No se ha podido obtener su ubicación'), findsOneWidget);
    await t.tap(find.text('Cancelar'));
    await t.pumpAndSettle();
    expect(s.cuerpos.containsKey('entrada'), isFalse);
    expect(find.text('Fichar entrada'), findsOneWidget);

    await t.tap(find.text('Fichar entrada'));
    await t.pumpAndSettle();
    await t.tap(find.text('Fichar sin ubicación'));
    await t.pumpAndSettle();
    expect((s.cuerpos['entrada'] as Map)['latitud'], isNull);
    expect(find.text('Registrado sin ubicación.'), findsOneWidget);
  });

  testWidgets('sin turno asignado se explica y no hay botón de entrada', (t) async {
    final s = _Servidor(fichable: false);
    await _montar(t, s, _UbicacionFalsa(null));
    expect(find.text('No tiene ningún turno asignado en este momento sobre el que fichar.'), findsOneWidget);
    expect(find.text('Fichar entrada'), findsNothing);
  });
}
