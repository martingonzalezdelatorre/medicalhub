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

Map<String, dynamic> _oferta({String estado = 'Pendiente'}) => {
      'id': 'o1', 'vacanteId': 'v1', 'profesionalId': 'p2', 'profesional': 'Luis Pérez', 'estado': estado, 'fechaOferta': '2026-10-22T10:00:00+00:00',
      'caduca': null, 'fechaRespuesta': null, 'motivoRespuesta': null, 'fechaDecision': null, 'motivoDecision': null, 'ofrecidaPor': 'Admin Sociedad',
      'necesidadId': 'n1', 'fecha': '2026-11-05', 'turno': 'Mañana 08:00–15:00', 'servicio': 'Quirófano', 'centro': 'Hospital Norte', 'estadoVacante': 'Abierta',
      'profesionalAusente': 'Elena Ruiz',
    };

class _Servidor {
  _Servidor({this.roles = const ['CoordinadorMedico'], this.tipos = const []});
  final List<String> roles;
  final List<Map<String, dynamic>> tipos;
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
        return json(tipos);
      case ('POST', '/api/mi-disponibilidad/lote'):
        return json(<dynamic>[], 201);
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
      case ('GET', '/api/mis-ofertas'):
        return json([_oferta()]);
      case ('POST', '/api/mis-ofertas/o1/aceptar'):
        return json(_oferta(estado: 'Aceptada'));
      case ('GET', '/api/ofertas'):
        return json([_oferta(estado: 'Aceptada')]);
      case ('POST', '/api/ofertas/o1/aprobar'):
        return json({'id': 'a9', 'profesionalId': 'p2', 'profesional': 'Luis Pérez', 'estado': 'Confirmada', 'advertencias': <dynamic>[], 'excepciones': <dynamic>[]});
      case ('POST', '/api/vacantes/v1/ofertas'):
        return json([_oferta()], 201);
      case ('GET', '/api/notificaciones/resumen'):
        return json({'noLeidas': 2});
      case ('GET', '/api/notificaciones'):
        return json([
          {'id': 'n1', 'tipo': 'OfertaRecibida', 'titulo': 'Oferta de sustitución', 'mensaje': 'Se le ofrece cubrir el turno.', 'entidadId': 'o1', 'fecha': '2026-10-22T10:00:00+00:00', 'leida': false},
        ]);
      case ('POST', '/api/notificaciones/n1/leer'):
        return json(<String, dynamic>{}, 204);
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

    testWidgets('declarar disponibilidad por hospital envía un bloque por cada día y turno elegidos', (t) async {
      Map<String, dynamic> tipo(String id, String nombre, String franja, String centro, String centroId) =>
          {'id': id, 'nombre': nombre, 'franja': franja, 'servicio': 'Urgencias', 'centro': centro, 'centroId': centroId};
      final s = _Servidor(roles: ['ProfesionalSanitario'], tipos: [
        tipo('m1', 'Mañana', '08:00–15:00', 'Hospital Norte', 'c1'),
        tipo('t1', 'Tarde', '15:00–22:00', 'Hospital Norte', 'c1'),
        tipo('m2', 'Mañana', '08:00–15:00', 'Hospital Sur', 'c2'),
      ]);
      await t.binding.setSurfaceSize(const Size(1200, 2400));
      addTearDown(() => t.binding.setSurfaceSize(null));
      await montarApp(t, s.falso);
      await t.tap(find.text('Mi disponibilidad').first);
      await t.pumpAndSettle();
      await t.tap(find.text('Declarar disponibilidad').first);
      await t.pumpAndSettle();

      // Hospital Norte: lunes y miércoles por la mañana; viernes por la tarde.
      expect(find.text('Hospital 1'), findsOneWidget);
      await t.tap(find.text('Mañana · 08:00–15:00').first);
      await t.tap(find.text('Lun–Vie').first);
      await t.tap(find.text('Mar').first);
      await t.tap(find.text('Jue').first);
      await t.tap(find.text('Vie').first);
      await t.tap(find.text('Añadir otros días y turnos'));
      await t.pumpAndSettle();
      for (final d in ['Lun', 'Mar', 'Mié', 'Jue', 'Sáb', 'Dom']) {
        await t.tap(find.text(d).last);
      }
      await t.tap(find.text('Tarde · 15:00–22:00').last);
      // Hospital Sur: todos los días, cualquier turno.
      await t.tap(find.text('Añadir otro hospital'));
      await t.pumpAndSettle();
      expect(find.text('Hospital 2'), findsOneWidget);

      await t.tap(find.widgetWithText(FilledButton, 'Declarar'));
      await t.pumpAndSettle();
      final cuerpo = s.peticiones.singleWhere((p) => p.method == 'POST' && p.path == '/api/mi-disponibilidad/lote').data as Map<String, dynamic>;
      final bloques = (cuerpo['bloques'] as List).cast<Map<String, dynamic>>();
      expect(bloques.map((b) => (b['centroId'], b['tipoTurnoId'], b['dias'])), [('c1', 'm1', 5), ('c1', 't1', 16), ('c2', null, 127)]);
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
      expect(find.descendant(of: find.byType(AlertDialog), matching: find.text('Luis Pérez')), findsOneWidget);
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

  group('Ofertas de sustitución y notificaciones', () {
    testWidgets('el profesional ve su oferta pendiente y la acepta', (t) async {
      final s = _Servidor(roles: ['ProfesionalSanitario']);
      await montarApp(t, s.falso);
      await t.tap(find.text('Mi disponibilidad').first);
      await t.pumpAndSettle();

      expect(find.text('Ofertas de sustitución'), findsOneWidget);
      expect(find.textContaining('05/11/2026 · Mañana 08:00–15:00'), findsOneWidget);
      expect(find.text('Pendiente de respuesta'), findsOneWidget);

      await t.tap(find.widgetWithText(FilledButton, 'Aceptar'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/mis-ofertas/o1/aceptar'), isTrue);
    });

    testWidgets('el coordinador ve la oferta aceptada y aprueba la sustitución', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await t.tap(find.text('Ausencias').first);
      await t.pumpAndSettle();
      await t.tap(find.text('Vacantes'));
      await t.pumpAndSettle();

      expect(find.text('Luis Pérez'), findsOneWidget);
      expect(find.text('Aceptada, pendiente de aprobación'), findsOneWidget);
      await t.tap(find.text('Aprobar sustitución'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/ofertas/o1/aprobar'), isTrue);
      expect(find.text('Sustitución aprobada'), findsOneWidget);
    });

    testWidgets('el coordinador ofrece la vacante a varios candidatos', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await t.tap(find.text('Ausencias').first);
      await t.pumpAndSettle();
      await t.tap(find.text('Vacantes'));
      await t.pumpAndSettle();
      await t.tap(find.text('Ofrecer turno'));
      await t.pumpAndSettle();

      expect(find.widgetWithText(FilledButton, 'Enviar oferta'), findsOneWidget);
      expect(t.widget<FilledButton>(find.widgetWithText(FilledButton, 'Enviar oferta')).onPressed, isNull); // nadie elegido
      await t.tap(find.text('Luis Pérez').last);
      await t.pumpAndSettle();
      await t.tap(find.widgetWithText(FilledButton, 'Enviar oferta'));
      await t.pumpAndSettle();
      final cuerpo = s.peticiones.singleWhere((p) => p.method == 'POST' && p.path == '/api/vacantes/v1/ofertas').data as Map<String, dynamic>;
      expect(cuerpo['profesionalIds'], ['p2']);
    });

    testWidgets('la campana muestra los avisos sin leer y la lista los marca como leídos', (t) async {
      final s = _Servidor(roles: ['ProfesionalSanitario']);
      await montarApp(t, s.falso);
      expect(find.text('2'), findsOneWidget); // distintivo de la campana

      await t.tap(find.byTooltip('Notificaciones'));
      await t.pumpAndSettle();
      expect(find.text('Oferta de sustitución'), findsOneWidget);
      expect(find.text('Marcar todas como leídas'), findsOneWidget);

      await t.tap(find.text('Oferta de sustitución'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/notificaciones/n1/leer'), isTrue);
      expect(find.text('Ofertas de sustitución'), findsOneWidget); // navega a «Mi disponibilidad»
    });
  });
}
