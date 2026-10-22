import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'ayudas_prueba.dart';

String _iso(DateTime d) => '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

final _hoy = DateTime.now();
final _hoyIso = _iso(DateTime(_hoy.year, _hoy.month, _hoy.day));

Map<String, dynamic> _turno({
  String id = 't1',
  String tipo = 'Mañana',
  String inicio = '08:00',
  String fin = '15:00',
  bool cruza = false,
  String estado = 'Borrador',
  int plazas = 2,
  List<Map<String, dynamic>> asignados = const [],
}) =>
    {
      'id': id, 'cuadranteId': 'c1', 'cuadranteEstado': estado, 'fecha': _hoyIso,
      'inicioLocal': inicio, 'finLocal': fin, 'cruzaMedianoche': cruza, 'duracionHoras': 7.0,
      'centroId': 'cen1', 'centro': 'Hospital Norte', 'servicioId': 's1', 'servicio': 'Quirófano',
      'tipoTurnoId': 'tt1', 'tipoTurno': tipo, 'esGuardia': false,
      'necesidades': [
        {'id': 'n-$id', 'numeroPlazas': plazas, 'categoriaProfesionalId': null, 'categoria': null, 'asignaciones': asignados},
      ],
      'plazasTotales': plazas, 'plazasCubiertas': asignados.length,
    };

Map<String, dynamic> _cuadrante({String estado = 'Borrador', int version = 0}) => {
      'id': 'c1', 'servicioId': 's1', 'servicioNombre': 'Quirófano', 'centroId': 'cen1', 'centroNombre': 'Hospital Norte',
      'nombre': 'Quirófano semana 1', 'desde': _hoyIso, 'hasta': _hoyIso, 'estado': estado, 'version': version,
      'fechaPublicacion': null, 'totalTurnos': 2, 'plazasTotales': 3, 'plazasCubiertas': 1,
    };

final _ana = {'id': 'a1', 'profesionalId': 'p1', 'profesional': 'Ana García', 'estado': 'Confirmada'};

class _Servidor {
  _Servidor({this.roles = const ['AdministradorSociedad'], this.turnos, this.estado = 'Borrador', this.validacion, this.errorAsignar});

  final List<String> roles;
  final List<Map<String, dynamic>>? turnos;
  final String estado;
  final Map<String, dynamic>? validacion;
  final Map<String, dynamic>? errorAsignar;
  final peticiones = <RequestOptions>[];

  late final ServidorFalso falso = ServidorFalso((p) {
    peticiones.add(p);
    switch ((p.method, p.path)) {
      case (_, '/api/autenticacion/refrescar'):
        return json(sesionJson(roles: roles));
      case ('GET', '/api/turnos' || '/api/mi-planificacion'):
        return json(turnos ?? [_turno(asignados: [_ana]), _turno(id: 't2', tipo: 'Noche', inicio: '22:00', fin: '08:00', cruza: true, plazas: 1)]);
      case ('GET', '/api/cuadrantes'):
        return json([_cuadrante(estado: estado)]);
      case ('GET', '/api/cuadrantes/c1'):
        return json(_cuadrante(estado: estado, version: estado == 'Borrador' ? 0 : 1));
      case ('GET', '/api/cuadrantes/c1/validacion'):
        return json(validacion ?? {
          'puedePublicarse': true,
          'incidencias': [{'severidad': 'Advertencia', 'codigo': 'SIN_CUBRIR', 'mensaje': 'Noche: faltan 1 de 1 plazas.'}],
        });
      case ('GET', '/api/cuadrantes/c1/versiones'):
        return json(<dynamic>[]);
      case ('POST', '/api/cuadrantes/c1/publicar'):
        return json(_cuadrante(estado: 'Publicado', version: 1));
      case ('GET', '/api/necesidades/n-t1/candidatos'):
        return json([
          {'profesionalId': 'p2', 'nombreCompleto': 'Luis Pérez', 'elegible': true, 'libre': true, 'motivos': <String>[]},
          {'profesionalId': 'p3', 'nombreCompleto': 'Eva Sanz', 'elegible': false, 'libre': false, 'motivos': ['Ya tiene un turno que se solapa con este horario.']},
        ]);
      case ('POST', '/api/necesidades/n-t1/asignaciones'):
        return errorAsignar == null ? json({'id': 'a2', 'profesionalId': 'p2', 'profesional': 'Luis Pérez', 'estado': 'Confirmada'}, 201) : json(errorAsignar!, 422);
      case ('GET', '/api/centros'):
        return json([{'id': 'cen1', 'nombre': 'Hospital Norte', 'tipo': 'Hospital', 'direccion': null, 'zonaHoraria': 'Europe/Madrid', 'activo': true, 'responsablesIds': <String>[]}]);
      case ('GET', '/api/servicios'):
        return json([]);
      case ('GET', '/api/plantillas-cuadrante'):
        return json(<dynamic>[]);
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
  group('Calendario', () {
    testWidgets('la vista semanal muestra turnos con cobertura, y el nocturno indica que termina al día siguiente', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await _ir(t, 'Planificación');

      expect(find.textContaining('08:00–15:00'), findsOneWidget);
      expect(find.textContaining('22:00–08:00 (+1)'), findsOneWidget);
      expect(find.text('1/2'), findsOneWidget);
      expect(find.text('0/1'), findsOneWidget);
    });

    testWidgets('se puede cambiar entre día, mes y agenda', (t) async {
      await montarApp(t, _Servidor().falso);
      await _ir(t, 'Planificación');

      for (final vista in ['Día', 'Mes', 'Agenda', 'Semana']) {
        await t.tap(find.widgetWithText(ChoiceChip, vista));
        await t.pumpAndSettle();
        expect(t.takeException(), isNull, reason: vista);
      }
      // Mes: la celda de hoy resume los turnos y las plazas sin cubrir en su etiqueta de accesibilidad
      await t.tap(find.widgetWithText(ChoiceChip, 'Mes'));
      await t.pumpAndSettle();
      expect(find.bySemanticsLabel(RegExp('2 turnos, 2 sin cubrir')), findsOneWidget);
    });

    testWidgets('en móvil la agenda es la vista por defecto y no desborda', (t) async {
      await montarApp(t, _Servidor().falso, tamano: const Size(375, 812));
      await t.tap(find.text('Planificación').first);
      await t.pumpAndSettle();

      expect(find.textContaining('08:00–15:00'), findsOneWidget);
      expect(find.text('Agenda'), findsOneWidget);
      expect(t.takeException(), isNull);

      // La cuadrícula semanal se degrada a lista vertical
      await t.tap(find.widgetWithText(ChoiceChip, 'Semana'));
      await t.pumpAndSettle();
      expect(find.textContaining('08:00–15:00'), findsOneWidget);
      expect(t.takeException(), isNull);
    });

    testWidgets('el filtro «solo sin cubrir» se envía a la API', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await _ir(t, 'Planificación');

      await t.tap(find.widgetWithText(FilterChip, 'Solo sin cubrir'));
      await t.pumpAndSettle();
      expect(s.peticiones.where((p) => p.path == '/api/turnos').last.queryParameters['soloSinCubrir'], true);
    });
  });

  group('Asignación', () {
    testWidgets('el coordinador asigna un candidato elegible y los no elegibles aparecen con su motivo', (t) async {
      final s = _Servidor(roles: ['CoordinadorMedico']);
      await montarApp(t, s.falso);
      await _ir(t, 'Planificación');

      await t.tap(find.textContaining('08:00–15:00'));
      await t.pumpAndSettle();
      expect(find.text('Ana García'), findsWidgets);
      await t.tap(find.textContaining('Asignar profesional'));
      await t.pumpAndSettle();

      expect(find.text('Luis Pérez'), findsOneWidget);
      expect(find.text('Eva Sanz'), findsOneWidget);
      expect(find.textContaining('se solapa'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'Asignar'), findsOneWidget); // solo el elegible

      await t.tap(find.widgetWithText(FilledButton, 'Asignar'));
      await t.pumpAndSettle();
      final post = s.peticiones.singleWhere((p) => p.method == 'POST' && p.path == '/api/necesidades/n-t1/asignaciones');
      expect(post.data, {'profesionalId': 'p2'});
    });

    testWidgets('si el servidor rechaza la asignación se muestra el motivo y el diálogo sigue abierto', (t) async {
      final s = _Servidor(errorAsignar: problema(422, 'No se puede asignar: el profesional ya tiene otro turno que se solapa con este horario.'));
      await montarApp(t, s.falso);
      await _ir(t, 'Planificación');
      await t.tap(find.textContaining('08:00–15:00'));
      await t.pumpAndSettle();
      await t.tap(find.textContaining('Asignar profesional'));
      await t.pumpAndSettle();
      await t.tap(find.widgetWithText(FilledButton, 'Asignar'));
      await t.pumpAndSettle();

      expect(find.textContaining('No se puede asignar: el profesional ya tiene otro turno'), findsOneWidget);
      expect(find.text('Candidatos'), findsOneWidget);
    });

    testWidgets('en un cuadrante publicado no se puede modificar', (t) async {
      final publicado = [_turno(estado: 'Publicado', asignados: [_ana])];
      await montarApp(t, _Servidor(turnos: publicado, estado: 'Publicado').falso);
      await _ir(t, 'Planificación');
      await t.tap(find.textContaining('08:00–15:00'));
      await t.pumpAndSettle();

      expect(find.textContaining('reábralo para modificarlo'), findsOneWidget);
      expect(find.textContaining('Asignar profesional'), findsNothing);
      expect(find.byIcon(Icons.person_remove_outlined), findsNothing);
      expect(find.text('Eliminar turno'), findsNothing);
    });

    testWidgets('el responsable de centro consulta pero no puede asignar', (t) async {
      await montarApp(t, _Servidor(roles: ['ResponsableCentro']).falso);
      await _ir(t, 'Planificación');
      await t.tap(find.textContaining('08:00–15:00'));
      await t.pumpAndSettle();

      expect(find.text('Ana García'), findsWidgets);
      expect(find.textContaining('Asignar profesional'), findsNothing);
      expect(find.text('Eliminar turno'), findsNothing);
      expect(find.text('1 plaza libre'), findsOneWidget);
    });
  });

  group('Cuadrantes', () {
    testWidgets('la lista muestra estado y cobertura, y el administrador puede crear', (t) async {
      await montarApp(t, _Servidor().falso);
      await _ir(t, 'Cuadrantes');

      expect(find.text('Quirófano semana 1'), findsOneWidget);
      expect(find.text('Borrador'), findsOneWidget);
      expect(find.textContaining('1/3 plazas cubiertas'), findsOneWidget);
      expect(find.text('Nuevo cuadrante'), findsOneWidget);
    });

    testWidgets('el responsable de centro no ve botones de creación', (t) async {
      await montarApp(t, _Servidor(roles: ['ResponsableCentro']).falso);
      await _ir(t, 'Cuadrantes');
      expect(find.text('Quirófano semana 1'), findsOneWidget);
      expect(find.text('Nuevo cuadrante'), findsNothing);
    });

    testWidgets('validar y publicar: las advertencias son revisables y la publicación se envía', (t) async {
      final s = _Servidor();
      await montarApp(t, s.falso);
      await _ir(t, 'Cuadrantes');
      await t.tap(find.text('Quirófano semana 1'));
      await t.pumpAndSettle();

      await t.tap(find.text('Validar y publicar'));
      await t.pumpAndSettle();
      expect(find.textContaining('Advertencias'), findsOneWidget);
      expect(find.text('Noche: faltan 1 de 1 plazas.'), findsOneWidget);

      await t.tap(find.widgetWithText(FilledButton, 'Publicar'));
      await t.pumpAndSettle();
      expect(s.peticiones.any((p) => p.method == 'POST' && p.path == '/api/cuadrantes/c1/publicar'), isTrue);
    });

    testWidgets('con errores bloqueantes el botón Publicar está desactivado', (t) async {
      final s = _Servidor(validacion: {
        'puedePublicarse': false,
        'incidencias': [{'severidad': 'Bloqueante', 'codigo': 'ASIGNACION_NO_ELEGIBLE', 'mensaje': 'Ana García en Mañana: El profesional está inactivo.'}],
      });
      await montarApp(t, s.falso);
      await _ir(t, 'Cuadrantes');
      await t.tap(find.text('Quirófano semana 1'));
      await t.pumpAndSettle();
      await t.tap(find.text('Validar y publicar'));
      await t.pumpAndSettle();

      expect(find.textContaining('impiden publicar'), findsOneWidget);
      expect(find.textContaining('está inactivo'), findsOneWidget);
      expect(t.widget<FilledButton>(find.widgetWithText(FilledButton, 'Publicar')).onPressed, isNull);
    });

    testWidgets('un cuadrante publicado ofrece reabrir y archivar, no publicar', (t) async {
      await montarApp(t, _Servidor(estado: 'Publicado').falso);
      await _ir(t, 'Cuadrantes');
      await t.tap(find.text('Quirófano semana 1'));
      await t.pumpAndSettle();

      expect(find.text('Reabrir'), findsOneWidget);
      expect(find.text('Archivar'), findsOneWidget);
      expect(find.text('Validar y publicar'), findsNothing);
      expect(find.text('Exportar CSV'), findsOneWidget);
    });
  });

  group('Mi planificación', () {
    testWidgets('el profesional ve sus turnos sin filtros de gestión', (t) async {
      final s = _Servidor(roles: ['ProfesionalSanitario'], turnos: [_turno(estado: 'Publicado', asignados: [_ana])]);
      await montarApp(t, s.falso);
      await _ir(t, 'Mi planificación');

      expect(find.textContaining('08:00–15:00'), findsOneWidget);
      expect(find.text('Solo sin cubrir'), findsNothing);
      expect(s.peticiones.any((p) => p.path == '/api/mi-planificacion'), isTrue);
      expect(s.peticiones.any((p) => p.path == '/api/turnos'), isFalse);
    });
  });
}
