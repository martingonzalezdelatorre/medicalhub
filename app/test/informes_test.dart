import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'ayudas_prueba.dart';

Map<String, dynamic> _fila(String nombre, {int turnos = 2, int plan = 1020, int noc = 480, int reg = 1, int reales = 630, int ajustes = -30, int efectivas = 600}) => {
      'profesionalId': 'p1', 'profesional': nombre, 'turnos': turnos, 'guardias': 0, 'planificadasMin': plan, 'nocturnasPlanMin': noc, 'turnosRegistrados': reg,
      'realesMin': reales, 'nocturnasRealMin': 480, 'ajustesMin': ajustes, 'efectivasMin': efectivas,
    };

Map<String, dynamic> _linea({bool registrada = false}) => {
      'asignacionId': 'a1', 'profesionalId': 'p1', 'profesional': 'Marta Gil', 'fecha': '2026-10-05', 'turno': 'Noche 22:00–08:00', 'servicio': 'Quirófano',
      'centro': 'Hospital Norte', 'esGuardia': false, 'inicioUtc': '2026-10-05T20:00:00+00:00', 'finUtc': '2026-10-06T06:00:00+00:00', 'planificadasMin': 600,
      'nocturnasPlanMin': 480, 'registroId': registrada ? 'r1' : null, 'inicioRealUtc': null, 'finRealUtc': null, 'realesMin': registrada ? 630 : null,
      'nocturnasRealMin': registrada ? 480 : null, 'ajustesMin': registrada ? -30 : 0, 'efectivasMin': registrada ? 600 : null,
      'ajustes': registrada ? [{'minutos': -30, 'motivo': 'Retraso de la relevista', 'usuario': 'Admin Sociedad', 'fecha': '2026-10-06T08:00:00+00:00'}] : <dynamic>[],
    };

Map<String, dynamic> _dashGestion({bool pendientes = true}) => {
      'gestion': {
        'desde': '2026-10-23', 'hasta': '2026-11-05', 'plazasTotales': 40, 'plazasCubiertas': 30, 'cuadrantesBorrador': 2,
        'proximosSinCubrir': [
          {'turnoId': 't1', 'fecha': '2026-10-25', 'turno': 'Noche 22:00–08:00', 'servicio': 'Quirófano', 'centro': 'Hospital Norte', 'plazasLibres': 1}
        ],
        'pendientes': pendientes
            ? {'vacantesAbiertas': 3, 'ausenciasSolicitadas': 2, 'ofertasPorAprobar': 1, 'disponibilidadesPendientes': 0, 'excepcionesUltimos30Dias': 4}
            : null,
        'semaforo': {'verde': 11, 'amarillo': 12, 'rojo': 13},
        'atencion': [
          {
            'turnoId': 't9', 'fecha': '2026-10-27', 'turno': 'Mañana 08:00–15:00', 'servicio': 'Urgencias', 'centro': 'Hospital Sur',
            'estado': 'Rojo', 'detalle': 'Anestesiología 5/4 · Enfermería 3/3',
          },
          {
            'turnoId': 't8', 'fecha': '2026-10-28', 'turno': 'Tarde 15:00–22:00', 'servicio': 'Urgencias', 'centro': 'Hospital Sur',
            'estado': 'Amarillo', 'detalle': 'Anestesiología 3/4 · Enfermería 3/3',
          },
        ],
      },
      'profesional': null,
    };

final _dashProfesional = {
  'gestion': null,
  'profesional': {
    'proximosTurnos': [
      {'asignacionId': 'a1', 'fecha': '2026-10-26', 'turno': 'Mañana 08:00–15:00', 'servicio': 'Quirófano', 'centro': 'Hospital Norte', 'cuadranteEstado': 'Publicado'}
    ],
    'horasPlanificadasMesMin': 1260, 'ofertasPendientes': 1, 'ausenciasSolicitadas': 0,
  },
};

class _Servidor {
  _Servidor({this.roles = const ['AdministradorSociedad'], Map<String, dynamic>? dashboard}) : dashboard = dashboard ?? _dashGestion();
  final List<String> roles;
  final Map<String, dynamic> dashboard;
  final peticiones = <RequestOptions>[];
  bool registrada = false;

  late final ServidorFalso falso = ServidorFalso((p) {
    peticiones.add(p);
    switch ((p.method, p.path)) {
      case (_, '/api/autenticacion/refrescar'):
        return json(sesionJson(roles: roles));
      case ('GET', '/api/dashboard'):
        return json(dashboard);
      case ('GET', '/api/servicios'):
        return json([]);
      case ('GET', '/api/informes/horas'):
        return json({'filas': [_fila('Marta Gil')], 'total': _fila('Total')});
      case ('GET', '/api/informes/cobertura'):
        return json({
          'filas': [
            {'servicioId': 's1', 'servicio': 'Quirófano', 'centro': 'Hospital Norte', 'plazas': 21, 'cubiertas': 19, 'libres': 2, 'porcentajeCobertura': 90.5, 'vacantesAbiertas': 1, 'excepciones': 2}
          ],
          'total': {'servicioId': '00000000-0000-0000-0000-000000000000', 'servicio': 'Total', 'centro': '', 'plazas': 21, 'cubiertas': 19, 'libres': 2, 'porcentajeCobertura': 90.5, 'vacantesAbiertas': 1, 'excepciones': 2},
        });
      case ('GET', '/api/horas'):
        return json([_linea(registrada: registrada)]);
      case ('PUT', '/api/asignaciones/a1/horas'):
        registrada = true;
        return json(_linea(registrada: true));
      case ('POST', '/api/asignaciones/a1/horas/ajustes'):
        return json(_linea(registrada: true), 201);
      case ('GET', '/api/informes/horas.csv'):
        return ResponseBody.fromBytes([0xEF, 0xBB, 0xBF, 0x41], 200, headers: {Headers.contentTypeHeader: ['text/csv']});
      default:
        return json(problema(404, 'No encontrado'), 404);
    }
  });
}

void main() {
  group('Panel de inicio', () {
    testWidgets('la coordinación ve cobertura, pendientes y turnos sin cubrir', (t) async {
      await montarApp(t, _Servidor().falso);
      expect(find.text('Cobertura de los próximos 14 días'), findsOneWidget);
      expect(find.text('30 de 40 plazas cubiertas'), findsOneWidget);
      expect(find.text('Vacantes abiertas'), findsOneWidget);
      expect(find.text('3'), findsOneWidget);
      expect(find.text('Próximos turnos sin cubrir'), findsOneWidget);
      expect(find.textContaining('25/10/2026 · Noche 22:00–08:00'), findsOneWidget);
      expect(find.text('1 libre(s)'), findsOneWidget);
    });

    testWidgets('el semáforo muestra cuántos turnos están en su tope, faltos de gente o pasados, y cuáles revisar', (t) async {
      await t.binding.setSurfaceSize(const Size(1000, 2400));
      addTearDown(() => t.binding.setSurfaceSize(null));
      await montarApp(t, _Servidor().falso);
      expect(find.text('Cobertura por turno'), findsOneWidget);
      expect(find.text('En su tope'), findsOneWidget);
      expect(find.text('Faltan profesionales'), findsOneWidget);
      expect(find.text('Pasados del tope'), findsOneWidget);
      expect(find.text('Turnos que requieren atención'), findsOneWidget);
      expect(find.textContaining('Anestesiología 5/4'), findsOneWidget);
      expect(find.textContaining('Anestesiología 3/4'), findsOneWidget);
    });

    testWidgets('el responsable de centro ve la cobertura pero no los pendientes de gestión', (t) async {
      await montarApp(t, _Servidor(roles: ['ResponsableCentro'], dashboard: _dashGestion(pendientes: false)).falso);
      expect(find.text('Cobertura de los próximos 14 días'), findsOneWidget);
      expect(find.text('Vacantes abiertas'), findsNothing);
    });

    testWidgets('el profesional ve sus próximos turnos y sus horas del mes', (t) async {
      await montarApp(t, _Servidor(roles: ['ProfesionalSanitario'], dashboard: _dashProfesional).falso);
      expect(find.text('Mis próximos turnos'), findsOneWidget);
      expect(find.textContaining('26/10/2026 · Mañana 08:00–15:00'), findsOneWidget);
      expect(find.text('21 h'), findsOneWidget);     // 1260 min
      expect(find.text('Cobertura de los próximos 14 días'), findsNothing);
    });

    testWidgets('si el panel falla la pantalla de inicio sigue mostrando el perfil', (t) async {
      final s = ServidorFalso((p) => p.path == '/api/autenticacion/refrescar' ? json(sesionJson()) : json(problema(500, 'Error'), 500));
      await montarApp(t, s);
      expect(find.textContaining('Ana García'), findsWidgets);
      expect(find.text('Cobertura de los próximos 14 días'), findsNothing);
    });
  });

  group('Informes', () {
    Future<void> abrir(WidgetTester t, _Servidor s) async {
      await montarApp(t, s.falso);
      await t.tap(find.text('Informes').first);
      await t.pumpAndSettle();
    }

    testWidgets('el informe de horas muestra planificadas, registradas, ajustes y total, y envía el periodo', (t) async {
      final s = _Servidor();
      await abrir(t, s);
      expect(find.text('Marta Gil'), findsOneWidget);
      expect(find.text('17 h'), findsWidgets);          // planificadas 1020 min
      expect(find.text('8 h'), findsWidgets);           // nocturnas 480 min
      expect(find.text('10 h'), findsWidgets);          // efectivas 600 min
      expect(find.text('Total'), findsOneWidget);
      final peticion = s.peticiones.firstWhere((p) => p.path == '/api/informes/horas');
      expect(peticion.queryParameters['desde'], matches(RegExp(r'^\d{4}-\d{2}-01$')));
      expect(peticion.queryParameters.containsKey('servicioId'), isFalse);
    });

    testWidgets('el informe de cobertura muestra el porcentaje con coma decimal', (t) async {
      await abrir(t, _Servidor());
      await t.tap(find.text('Cobertura').first);
      await t.pumpAndSettle();
      expect(find.text('Quirófano'), findsOneWidget);
      expect(find.text('90,5 %'), findsWidgets);
    });

    testWidgets('el gestor registra horas desde el registro y la API recibe instantes UTC', (t) async {
      final s = _Servidor();
      await abrir(t, s);
      await t.tap(find.text('Registro de horas'));
      await t.pumpAndSettle();
      expect(find.text('Sin registrar'), findsOneWidget);

      await t.tap(find.text('Registrar horas').first);
      await t.pumpAndSettle();
      await t.tap(find.widgetWithText(FilledButton, 'Guardar'));
      await t.pumpAndSettle();
      final put = s.peticiones.singleWhere((p) => p.method == 'PUT' && p.path == '/api/asignaciones/a1/horas');
      expect((put.data as Map)['inicioUtc'], '2026-10-05T20:00:00.000Z');
      expect((put.data as Map)['finUtc'], '2026-10-06T06:00:00.000Z');
    });

    testWidgets('el ajuste exige minutos y un motivo de al menos 10 caracteres', (t) async {
      final s = _Servidor()..registrada = true;
      await abrir(t, s);
      await t.tap(find.text('Registro de horas'));
      await t.pumpAndSettle();
      expect(find.textContaining('Retraso de la relevista'), findsOneWidget);

      await t.tap(find.text('Ajustar').first);
      await t.pumpAndSettle();
      await t.enterText(find.widgetWithText(TextField, 'Minutos (+/−)'), '-30');
      await t.enterText(find.widgetWithText(TextField, 'Motivo (mínimo 10 caracteres)'), 'corto');
      await t.tap(find.widgetWithText(FilledButton, 'Guardar'));
      await t.pumpAndSettle();
      expect(find.textContaining('un motivo de al menos 10 caracteres'), findsOneWidget);
      expect(s.peticiones.any((p) => p.path.endsWith('/ajustes')), isFalse);

      await t.enterText(find.widgetWithText(TextField, 'Motivo (mínimo 10 caracteres)'), 'Se descuenta el retraso de la relevista');
      await t.tap(find.widgetWithText(FilledButton, 'Guardar'));
      await t.pumpAndSettle();
      final post = s.peticiones.singleWhere((p) => p.path == '/api/asignaciones/a1/horas/ajustes');
      expect((post.data as Map)['minutos'], -30);
    });

    testWidgets('el responsable de centro consulta pero no exporta ni registra; el profesional no ve la sección', (t) async {
      final s = _Servidor(roles: ['ResponsableCentro']);
      await abrir(t, s);
      expect(find.text('Exportar CSV'), findsNothing);
      await t.tap(find.text('Registro de horas'));
      await t.pumpAndSettle();
      expect(find.text('Registrar horas'), findsNothing);
    });

    testWidgets('el profesional no ve el menú de informes', (t) async {
      await montarApp(t, _Servidor(roles: ['ProfesionalSanitario'], dashboard: _dashProfesional).falso);
      expect(find.text('Informes'), findsNothing);
    });

    testWidgets('el gestor exporta el CSV de horas', (t) async {
      final s = _Servidor();
      await abrir(t, s);
      await t.tap(find.text('Exportar CSV'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.path == '/api/informes/horas.csv'), isTrue);
    });
  });
}
