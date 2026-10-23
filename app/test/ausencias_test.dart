import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'ayudas_prueba.dart';

Map<String, dynamic> _ausencia({String estado = 'Solicitada'}) => {
      'id': 'a1', 'profesionalId': 'p1', 'profesional': 'Ana García', 'tipo': 'Vacaciones', 'desde': '2026-11-03', 'hasta': '2026-11-05', 'dias': 3,
      'comentario': null, 'estado': estado, 'fechaSolicitud': '2026-10-22T10:00:00+00:00', 'solicitadaPor': 'Ana García', 'decisiones': <dynamic>[],
    };

Map<String, dynamic> _disp({String estado = 'Declarada'}) => {
      'id': 'd1', 'profesionalId': 'p1', 'profesional': 'Ana García', 'tipo': 'Bloqueo', 'desde': '2026-11-02', 'hasta': '2026-11-30', 'dias': 1,
      'tipoTurnoId': null, 'tipoTurno': null, 'comentario': null, 'estado': estado, 'fechaDeclaracion': '2026-10-22T10:00:00+00:00', 'motivoDecision': null,
    };

final _vacante = {
  'id': 'v1', 'estado': 'Abierta', 'motivo': 'Ausencia: Baja temporal', 'fechaCreacion': '2026-10-22T10:00:00+00:00', 'fechaCierre': null,
  'necesidadId': 'n1', 'turnoId': 't1', 'fecha': '2026-11-05', 'turno': 'Mañana', 'servicio': 'Quirófano', 'centro': 'Hospital Norte', 'categoria': null,
  'profesionalAusente': 'Elena Ruiz', 'ausenciaId': 'a1', 'cuadranteEstado': 'Publicado',
};

class _Servidor {
  _Servidor({this.roles = const ['CoordinadorMedico']});
  final List<String> roles;
  final peticiones = <RequestOptions>[];

  late final ServidorFalso falso = ServidorFalso((p) {
    peticiones.add(p);
    switch ((p.method, p.path)) {
      case (_, '/api/autenticacion/refrescar'):
        return json(sesionJson(roles: roles));
      case ('GET', '/api/mis-ausencias'):
        return json([_ausencia()]);
      case ('GET', '/api/mi-disponibilidad'):
        return json([_disp()]);
      case ('GET', '/api/mis-tipos-turno'):
        return json(<dynamic>[]);
      case ('POST', '/api/mis-ausencias'):
        return json(_ausencia(), 201);
      case ('POST', '/api/mis-ausencias/a1/cancelar'):
        return json(_ausencia(estado: 'Cancelada'));
      case ('GET', '/api/ausencias'):
        return json([_ausencia()]);
      case ('POST', '/api/ausencias/a1/aprobar'):
        return json({
          'ausencia': _ausencia(estado: 'Aprobada'),
          'vacantes': [
            {'asignacionId': 'x', 'turnoId': 't1', 'necesidadId': 'n1', 'fecha': '2026-11-05', 'turno': 'Mañana', 'servicio': 'Quirófano', 'centro': 'Hospital Norte', 'cuadranteEstado': 'Publicado'}
          ],
        });
      case ('GET', '/api/vacantes'):
        return json([_vacante]);
      case ('GET', '/api/necesidades/n1/candidatos'):
        return json([
          {
            'profesionalId': 'p2', 'nombreCompleto': 'Luis Pérez', 'elegible': true, 'libre': true, 'motivos': <String>[], 'violaciones': <dynamic>[],
            'requiereExcepcion': false, 'puedeAutorizar': false, 'notas': ['Prefiere este turno.'],
          },
        ]);
      case ('POST', '/api/vacantes/v1/cubrir'):
        return json({'id': 'a9', 'profesionalId': 'p2', 'profesional': 'Luis Pérez', 'estado': 'Confirmada', 'advertencias': <dynamic>[], 'excepciones': <dynamic>[]}, 201);
      case ('GET', '/api/disponibilidades'):
        return json([_disp()]);
      case ('POST', '/api/disponibilidades/d1/aprobar'):
        return json(_disp(estado: 'Aprobada'));
      default:
        return json(problema(404, 'No encontrado'), 404);
    }
  });
}

void main() {
  group('Ausencias y disponibilidad', () {
    testWidgets('el profesional ve sus ausencias y declaraciones y puede cancelar una solicitud', (t) async {
      final s = _Servidor(roles: ['ProfesionalSanitario']);
      await montarApp(t, s.falso);
      await t.tap(find.text('Mi disponibilidad').first);
      await t.pumpAndSettle();

      expect(find.textContaining('Vacaciones · 03/11/2026 – 05/11/2026'), findsOneWidget);
      expect(find.text('Solicitada'), findsOneWidget);
      expect(find.textContaining('No disponible (bloqueo) · 02/11/2026 – 30/11/2026'), findsOneWidget);
      expect(find.text('Pendiente de aprobación'), findsOneWidget);
      expect(find.text('Ausencias'), findsNothing); // la gestión no es para el profesional

      await t.tap(find.byTooltip('Cancelar').first);
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/mis-ausencias/a1/cancelar'), isTrue);
    });

    testWidgets('solicitar una ausencia envía el tipo y las fechas y avisa de no incluir datos de salud', (t) async {
      final s = _Servidor(roles: ['ProfesionalSanitario']);
      await montarApp(t, s.falso);
      await t.tap(find.text('Mi disponibilidad').first);
      await t.pumpAndSettle();
      await t.tap(find.text('Solicitar ausencia'));
      await t.pumpAndSettle();
      expect(find.textContaining('No incluya datos de salud'), findsOneWidget);
      await t.tap(find.widgetWithText(FilledButton, 'Solicitar'));
      await t.pumpAndSettle();
      final cuerpo = s.peticiones.singleWhere((p) => p.method == 'POST' && p.path == '/api/mis-ausencias').data as Map<String, dynamic>;
      expect(cuerpo['tipo'], 'Vacaciones');
      expect(cuerpo['desde'], cuerpo['hasta']);
    });

    testWidgets('el coordinador aprueba una ausencia y ve cuántas vacantes se crean', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await t.tap(find.text('Ausencias').first);
      await t.pumpAndSettle();
      expect(find.textContaining('Ana García · Vacaciones'), findsOneWidget);

      await t.tap(find.byType(PopupMenuButton<String>).first);
      await t.pumpAndSettle();
      await t.tap(find.text('Aprobar'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/ausencias/a1/aprobar'), isTrue);
      expect(find.text('Ausencia aprobada. Vacantes creadas: 1'), findsOneWidget);
    });

    testWidgets('el coordinador cubre una vacante eligiendo un candidato', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await t.tap(find.text('Ausencias').first);
      await t.pumpAndSettle();
      await t.tap(find.text('Vacantes'));
      await t.pumpAndSettle();
      expect(find.textContaining('Ausente: Elena Ruiz'), findsOneWidget);

      await t.tap(find.text('Buscar sustituto'));
      await t.pumpAndSettle();
      expect(find.text('Luis Pérez'), findsOneWidget);
      expect(find.text('Prefiere este turno.'), findsOneWidget);
      await t.tap(find.widgetWithText(FilledButton, 'Asignar'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/vacantes/v1/cubrir'), isTrue);
    });

    testWidgets('el coordinador aprueba una disponibilidad declarada', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await t.tap(find.text('Ausencias').first);
      await t.pumpAndSettle();
      await t.tap(find.text('Disponibilidad'));
      await t.pumpAndSettle();
      await t.tap(find.byTooltip('Aprobar'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/disponibilidades/d1/aprobar'), isTrue);
    });

    testWidgets('el profesional ve su sección y no la gestión', (t) async {
      await montarApp(t, _Servidor(roles: ['ProfesionalSanitario']).falso);
      expect(find.text('Ausencias'), findsNothing);
      expect(find.text('Mi disponibilidad'), findsWidgets);
    });

    testWidgets('el coordinador ve la gestión y no «Mi disponibilidad»', (t) async {
      await montarApp(t, _Servidor().falso);
      expect(find.text('Mi disponibilidad'), findsNothing);
      expect(find.text('Ausencias'), findsWidgets);
    });
  });
}
