import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'ayudas_prueba.dart';

String _iso(DateTime d) => '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

final _hoy = DateTime.now();
final _hoyIso = _iso(DateTime(_hoy.year, _hoy.month, _hoy.day));

const _tipos = [
  {
    'tipo': 'DescansoMinimo', 'nombre': 'Descanso mínimo entre turnos', 'descripcion': 'Horas mínimas entre turnos.', 'nivel': 'Asignación',
    'comportamientoPorDefecto': 'No se aplica hasta que la sociedad la configure',
    'parametros': [{'clave': 'horas', 'etiqueta': 'Horas de descanso', 'tipo': 'entero', 'minimo': 1, 'maximo': 72}],
  },
  {
    'tipo': 'CompetenciasRequeridas', 'nombre': 'Competencias requeridas', 'descripcion': 'Competencias vigentes.', 'nivel': 'Asignación',
    'comportamientoPorDefecto': 'Bloqueante, con excepción posible', 'parametros': <dynamic>[],
  },
];

Map<String, dynamic> _regla({bool pendiente = true}) => {
      'id': 'r1', 'tipo': 'DescansoMinimo', 'nombre': 'Descanso 12 h', 'descripcion': null, 'servicioId': null, 'servicioNombre': null,
      'severidad': 'Bloqueante', 'activa': true, 'permiteExcepcion': true, 'excepcionRequiereAdministrador': false,
      'parametros': {'horas': 12}, 'fundamento': 'Convenio de la sociedad', 'revisadaEl': null, 'revisadaPor': null, 'pendienteDeRevision': pendiente,
    };

final _excepciones = [
  {
    'id': 'e1', 'fecha': '2026-10-22T10:00:00+00:00', 'profesionalId': 'p1', 'profesional': 'Ana García', 'turnoId': 't1',
    'turno': 'Mañana del 03/11/2026 (Quirófano)', 'codigo': 'DescansoMinimo', 'regla': 'Descanso 12 h',
    'mensaje': 'Descanso insuficiente: 0 h', 'justificacion': 'Cobertura urgente por baja del titular.', 'autorizadaPor': 'Admin Sociedad', 'asignacionActiva': false,
  },
];

Map<String, dynamic> _turno() => {
      'id': 't1', 'cuadranteId': 'c1', 'cuadranteEstado': 'Borrador', 'fecha': _hoyIso, 'inicioLocal': '08:00', 'finLocal': '15:00',
      'cruzaMedianoche': false, 'duracionHoras': 7.0, 'centroId': 'cen1', 'centro': 'Hospital Norte', 'servicioId': 's1', 'servicio': 'Quirófano',
      'tipoTurnoId': 'tt1', 'tipoTurno': 'Mañana', 'esGuardia': false,
      'necesidades': [{'id': 'n1', 'numeroPlazas': 1, 'categoriaProfesionalId': null, 'categoria': null, 'asignaciones': <dynamic>[]}],
      'plazasTotales': 1, 'plazasCubiertas': 0,
    };

Map<String, dynamic> _viol(String codigo, String mensaje, {String severidad = 'Bloqueante', bool permite = true, bool admin = false}) =>
    {'codigo': codigo, 'reglaId': null, 'regla': codigo, 'severidad': severidad, 'mensaje': mensaje, 'permiteExcepcion': permite, 'requiereAdministrador': admin};

List<Map<String, dynamic>> _candidatos({bool puedeAutorizar = true}) => [
      {'profesionalId': 'p1', 'nombreCompleto': 'Ana García', 'elegible': true, 'libre': true, 'motivos': <String>[], 'violaciones': <dynamic>[],
        'requiereExcepcion': false, 'puedeAutorizar': false},
      {'profesionalId': 'p2', 'nombreCompleto': 'Luis Pérez', 'elegible': false, 'libre': true, 'motivos': ['Descanso insuficiente: 0 h (mínimo 12 h).'],
        'violaciones': [_viol('DescansoMinimo', 'Descanso insuficiente: 0 h (mínimo 12 h).', admin: !puedeAutorizar)],
        'requiereExcepcion': true, 'puedeAutorizar': puedeAutorizar},
      {'profesionalId': 'p3', 'nombreCompleto': 'Eva Sanz', 'elegible': false, 'libre': false, 'motivos': ['Ya tiene otro turno que se solapa con este horario.'],
        'violaciones': [_viol('Solape', 'Ya tiene otro turno que se solapa con este horario.', permite: false)], 'requiereExcepcion': false, 'puedeAutorizar': false},
      {'profesionalId': 'p4', 'nombreCompleto': 'Marta Gil', 'elegible': true, 'libre': true, 'motivos': ['Máximo de horas superado (aviso).'],
        'violaciones': [_viol('MaxHorasPeriodo', 'Máximo de horas superado (aviso).', severidad: 'Advertencia', permite: false)],
        'requiereExcepcion': false, 'puedeAutorizar': false},
    ];

class _Servidor {
  _Servidor({this.roles = const ['AdministradorSociedad'], this.puedeAutorizar = true, this.validacion});

  final List<String> roles;
  final bool puedeAutorizar;
  final Map<String, dynamic>? validacion;
  final peticiones = <RequestOptions>[];

  late final ServidorFalso falso = ServidorFalso((p) {
    peticiones.add(p);
    switch ((p.method, p.path)) {
      case (_, '/api/autenticacion/refrescar'):
        return json(sesionJson(roles: roles));
      case ('GET', '/api/reglas-planificacion/tipos'):
        return json(_tipos);
      case ('GET', '/api/reglas-planificacion'):
        return json([_regla()]);
      case ('POST', '/api/reglas-planificacion'):
        return json(_regla(), 201);
      case ('POST', '/api/reglas-planificacion/r1/revisar'):
        return json(_regla(pendiente: false));
      case ('GET', '/api/excepciones-reglas'):
        return json(_excepciones);
      case ('GET', '/api/servicios'):
        return json([]);
      case ('GET', '/api/centros'):
        return json([]);
      case ('GET', '/api/turnos'):
        return json([_turno()]);
      case ('GET', '/api/necesidades/n1/candidatos'):
        return json(_candidatos(puedeAutorizar: puedeAutorizar));
      case ('POST', '/api/necesidades/n1/asignaciones'):
        return json({'id': 'a9', 'profesionalId': 'p2', 'profesional': 'Luis Pérez', 'estado': 'Confirmada', 'advertencias': <dynamic>[], 'excepciones': [_viol('DescansoMinimo', 'x')]}, 201);
      case ('GET', '/api/cuadrantes'):
        return json([_cuadrante()]);
      case ('GET', '/api/cuadrantes/c1'):
        return json({
          'id': 'c1', 'servicioId': 's1', 'servicioNombre': 'Quirófano', 'centroId': 'cen1', 'centroNombre': 'Hospital Norte', 'nombre': 'Semana 1',
          'desde': _hoyIso, 'hasta': _hoyIso, 'estado': 'Borrador', 'version': 0, 'fechaPublicacion': null, 'totalTurnos': 1, 'plazasTotales': 1, 'plazasCubiertas': 1,
        });
      case ('GET', '/api/cuadrantes/c1/versiones'):
        return json(<dynamic>[]);
      case ('GET', '/api/cuadrantes/c1/validacion'):
        return json(validacion ?? {'puedePublicarse': true, 'incidencias': <dynamic>[]});
      case ('POST', '/api/asignaciones/a1/excepciones'):
        return json([_viol('DescansoMinimo', 'x')]);
      default:
        return json(problema(404, 'No encontrado'), 404);
    }
  });
}

Future<void> _ir(WidgetTester t, String seccion) async {
  await t.tap(find.text(seccion).first);
  await t.pumpAndSettle();
}

void main() {
  group('Reglas', () {
    testWidgets('el administrador ve las reglas con su estado de revisión y el comportamiento por defecto', (t) async {
      await montarApp(t, _Servidor().falso);
      await _ir(t, 'Reglas');

      expect(find.text('Descanso 12 h'), findsOneWidget);
      expect(find.text('Pendiente de revisión'), findsOneWidget);
      expect(find.textContaining('Horas de descanso: 12'), findsOneWidget);
      expect(find.textContaining('no existen hasta que los configure'), findsOneWidget); // aviso: sin límites legales por defecto
      expect(find.text('Nueva regla'), findsOneWidget);
      // Los tipos sin regla muestran qué pasa por defecto
      await t.tap(find.text('Comportamiento cuando no hay regla configurada'));
      await t.pumpAndSettle();
      expect(find.text('Competencias requeridas'), findsOneWidget);
      expect(find.textContaining('Bloqueante, con excepción posible'), findsOneWidget);
    });

    testWidgets('el coordinador consulta las reglas pero no puede modificarlas', (t) async {
      await montarApp(t, _Servidor(roles: ['CoordinadorMedico']).falso);
      await _ir(t, 'Reglas');
      expect(find.text('Descanso 12 h'), findsOneWidget);
      expect(find.text('Nueva regla'), findsNothing);
      expect(find.byType(PopupMenuButton<String>), findsNothing);
    });

    testWidgets('el responsable de centro y el profesional no ven la sección', (t) async {
      for (final rol in ['ResponsableCentro', 'ProfesionalSanitario']) {
        await montarApp(t, _Servidor(roles: [rol]).falso);
        expect(find.text('Reglas'), findsNothing, reason: rol);
      }
    });

    testWidgets('crear una regla valida el rango del parámetro y envía los datos tipados', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await _ir(t, 'Reglas');
      await t.tap(find.text('Nueva regla'));
      await t.pumpAndSettle();

      await t.tap(find.byType(DropdownButtonFormField<String>).first);
      await t.pumpAndSettle();
      await t.tap(find.text('Descanso mínimo entre turnos').last);
      await t.pumpAndSettle();
      expect(find.widgetWithText(TextFormField, 'Horas de descanso (1–72)'), findsOneWidget);

      await t.enterText(find.widgetWithText(TextFormField, 'Horas de descanso (1–72)'), '500');
      await t.tap(find.text('Guardar'));
      await t.pumpAndSettle();
      expect(find.text('Número entre 1 y 72'), findsOneWidget);

      await t.enterText(find.widgetWithText(TextFormField, 'Horas de descanso (1–72)'), '11');
      await t.tap(find.text('Guardar'));
      await t.pumpAndSettle();

      final post = s.peticiones.singleWhere((p) => p.method == 'POST' && p.path == '/api/reglas-planificacion');
      final cuerpo = post.data as Map<String, dynamic>;
      expect(cuerpo['tipo'], 'DescansoMinimo');
      expect(cuerpo['parametros'], {'horas': 11});
      expect(cuerpo['severidad'], 'Bloqueante');
      expect(cuerpo['permiteExcepcion'], true);
      expect(cuerpo['servicioId'], isNull);
    });

    testWidgets('marcar una regla como revisada llama a la API', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await _ir(t, 'Reglas');
      await t.tap(find.byType(PopupMenuButton<String>).first);
      await t.pumpAndSettle();
      await t.tap(find.text('Marcar como revisada'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/reglas-planificacion/r1/revisar'), isTrue);
    });

    testWidgets('el registro de excepciones muestra justificación, autor y si la asignación se anuló', (t) async {
      await montarApp(t, _Servidor().falso);
      await _ir(t, 'Reglas');
      await t.tap(find.text('Excepciones'));
      await t.pumpAndSettle();

      expect(find.text('Ana García'), findsOneWidget);
      expect(find.textContaining('Cobertura urgente por baja del titular.'), findsOneWidget);
      expect(find.textContaining('Autorizada por Admin Sociedad'), findsOneWidget);
      expect(find.text('Asignación anulada'), findsOneWidget);
    });
  });

  group('Asignación con excepción', () {
    Future<void> abrirCandidatos(WidgetTester t) async {
      await _ir(t, 'Planificación');
      await t.tap(find.textContaining('08:00–15:00'));
      await t.pumpAndSettle();
      await t.tap(find.textContaining('Asignar profesional'));
      await t.pumpAndSettle();
    }

    testWidgets('cada candidato se presenta como válido, con excepción, con aviso o no permitido', (t) async {
      await montarApp(t, _Servidor().falso);
      await abrirCandidatos(t);

      expect(find.widgetWithText(FilledButton, 'Asignar'), findsNWidgets(2)); // Ana y Marta (con aviso)
      expect(find.widgetWithText(OutlinedButton, 'Con excepción'), findsOneWidget); // Luis
      expect(find.text('Máximo de horas superado (aviso).'), findsOneWidget);
      expect(find.textContaining('se solapa'), findsOneWidget); // Eva: no permitido, sin botón
    });

    testWidgets('autorizar una excepción exige justificación y la envía junto con la asignación', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await abrirCandidatos(t);

      await t.tap(find.widgetWithText(OutlinedButton, 'Con excepción'));
      await t.pumpAndSettle();
      expect(find.textContaining('Descanso insuficiente'), findsWidgets);

      await t.tap(find.widgetWithText(FilledButton, 'Autorizar excepción'));
      await t.pumpAndSettle();
      expect(find.textContaining('mínimo 10 caracteres'), findsOneWidget);

      await t.enterText(find.byType(TextFormField), 'corta');
      await t.tap(find.widgetWithText(FilledButton, 'Autorizar excepción'));
      await t.pumpAndSettle();
      expect(find.textContaining('mínimo 10 caracteres'), findsOneWidget);

      await t.enterText(find.byType(TextFormField), 'Baja imprevista del titular, no hay otro candidato.');
      await t.tap(find.widgetWithText(FilledButton, 'Autorizar excepción'));
      await t.pumpAndSettle();

      final post = s.peticiones.singleWhere((p) => p.method == 'POST' && p.path == '/api/necesidades/n1/asignaciones');
      expect(post.data, {'profesionalId': 'p2', 'excepcion': {'justificacion': 'Baja imprevista del titular, no hay otro candidato.'}});
    });

    testWidgets('si la excepción solo la autoriza el administrador, el coordinador no ve el botón', (t) async {
      await montarApp(t, _Servidor(roles: ['CoordinadorMedico'], puedeAutorizar: false).falso);
      await abrirCandidatos(t);
      expect(find.widgetWithText(OutlinedButton, 'Con excepción'), findsNothing);
      expect(find.text('Requiere al administrador'), findsOneWidget);
    });
  });

  group('Publicación con excepciones', () {
    testWidgets('las excepciones autorizadas se muestran con su justificación y las pendientes se pueden autorizar', (t) async {
      final s = _Servidor(validacion: {
        'puedePublicarse': false,
        'incidencias': [
          {'severidad': 'Bloqueante', 'codigo': 'DescansoMinimo', 'mensaje': 'Luis en Mañana: Descanso insuficiente.', 'excepcionada': false,
            'asignacionId': 'a1', 'permiteExcepcion': true},
          {'severidad': 'Bloqueante', 'codigo': 'CompetenciasRequeridas', 'mensaje': 'Eva en Noche: Falta la competencia «Vía aérea».', 'excepcionada': true,
            'justificacion': 'Habilitación en trámite.', 'autorizadaPor': 'Admin Sociedad', 'asignacionId': 'a2', 'permiteExcepcion': true},
          {'severidad': 'Bloqueante', 'codigo': 'Solape', 'mensaje': 'Ana: solape.', 'excepcionada': false, 'asignacionId': 'a3', 'permiteExcepcion': false},
        ],
      });
      await montarApp(t, s.falso);
      await _ir(t, 'Cuadrantes');
      await t.tap(find.text('Semana 1'));
      await t.pumpAndSettle();
      await t.tap(find.text('Validar y publicar'));
      await t.pumpAndSettle();

      // Excepción ya autorizada: visible, con justificación y autor
      expect(find.text('Excepciones autorizadas'), findsOneWidget);
      expect(find.textContaining('Habilitación en trámite.'), findsOneWidget);
      expect(find.textContaining('Autorizada por Admin Sociedad'), findsOneWidget);
      // Errores pendientes: solo el que admite excepción ofrece el botón
      expect(find.widgetWithText(OutlinedButton, 'Autorizar excepción'), findsOneWidget);
      expect(t.widget<FilledButton>(find.widgetWithText(FilledButton, 'Publicar')).onPressed, isNull);

      await t.tap(find.widgetWithText(OutlinedButton, 'Autorizar excepción'));
      await t.pumpAndSettle();
      await t.enterText(find.byType(TextFormField), 'Acuerdo puntual con la dirección médica.');
      await t.tap(find.widgetWithText(FilledButton, 'Autorizar excepción'));
      await t.pumpAndSettle();

      final post = s.peticiones.singleWhere((p) => p.method == 'POST' && p.path == '/api/asignaciones/a1/excepciones');
      expect(post.data, {'justificacion': 'Acuerdo puntual con la dirección médica.'});
    });

    testWidgets('sin infracciones pendientes se puede publicar', (t) async {
      await montarApp(t, _Servidor().falso);
      await _ir(t, 'Cuadrantes');
      await t.tap(find.text('Semana 1'));
      await t.pumpAndSettle();
      await t.tap(find.text('Validar y publicar'));
      await t.pumpAndSettle();
      expect(find.textContaining('listo para publicarse'), findsOneWidget);
      expect(t.widget<FilledButton>(find.widgetWithText(FilledButton, 'Publicar')).onPressed, isNotNull);
    });
  });
}

Map<String, dynamic> _cuadrante() => {
      'id': 'c1', 'servicioId': 's1', 'servicioNombre': 'Quirófano', 'centroId': 'cen1', 'centroNombre': 'Hospital Norte', 'nombre': 'Semana 1',
      'desde': _hoyIso, 'hasta': _hoyIso, 'estado': 'Borrador', 'version': 0, 'fechaPublicacion': null, 'totalTurnos': 1, 'plazasTotales': 1, 'plazasCubiertas': 1,
    };
