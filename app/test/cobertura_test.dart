import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:medicalhub_app/features/planificacion/presentation/pagina_cobertura.dart';

import 'ayudas_prueba.dart';

Finder _enTarjetas(String texto) => find.descendant(of: find.byType(TarjetaCobertura), matching: find.text(texto));

String _iso(DateTime d) => '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

final _hoyIso = _iso(DateTime.now());

Map<String, dynamic> _turno(String id, String tipo, String inicio, String fin, String centroId, String centro, int plazas, List<Map<String, dynamic>> asignados) => {
      'id': id, 'cuadranteId': 'c1', 'cuadranteEstado': 'Publicado', 'fecha': _hoyIso,
      'inicioLocal': inicio, 'finLocal': fin, 'cruzaMedianoche': false, 'duracionHoras': 7.0,
      'centroId': centroId, 'centro': centro, 'servicioId': 's1', 'servicio': 'Urgencias',
      'tipoTurnoId': 'tt-$tipo', 'tipoTurno': tipo, 'esGuardia': false,
      'necesidades': [
        {'id': 'n-$id', 'numeroPlazas': plazas, 'categoriaProfesionalId': null, 'categoria': null, 'asignaciones': asignados},
      ],
      'plazasTotales': plazas, 'plazasCubiertas': asignados.length,
    };

Map<String, dynamic> _asig(String id, String nombre) => {'id': 'a-$id', 'profesionalId': id, 'profesional': nombre, 'estado': 'Confirmada'};

void main() {
  late List<dynamic> peticionesTurnos;
  final peticionesProfesionales = <Map<String, dynamic>>[];

  ServidorFalso servidor(List<String> roles) {
    peticionesTurnos = [];
    return ServidorFalso((p) {
      switch ((p.method, p.path)) {
        case (_, '/api/autenticacion/refrescar'):
          return json(sesionJson(roles: roles));
        case ('GET', '/api/turnos'):
          peticionesTurnos.add(p.queryParameters);
          return json([
            _turno('t1', 'Mañana', '08:00', '15:00', 'h1', 'Hospital Norte', 2, [_asig('p1', 'Ana García')]),
            _turno('t2', 'Noche', '22:00', '06:00', 'h2', 'Hospital Sur', 1, [_asig('p2', 'Luis Pérez')]),
          ]);
        case ('GET', '/api/profesionales'):
          peticionesProfesionales.add(p.queryParameters);
          final texto = (p.queryParameters['texto'] as String? ?? '').toLowerCase();
          return json([
            for (final (id, n, a) in [('p1', 'Ana', 'García'), ('p2', 'Luis', 'Pérez'), ('p3', 'Lucía', 'Sin Turnos')])
              if ('$n $a'.toLowerCase().contains(texto))
                {'id': id, 'usuarioId': null, 'nombre': n, 'apellidos': a, 'email': '$id@x.es', 'telefono': null, 'numeroColegiado': null, 'categoriaId': 'cat1', 'especialidadesIds': <String>[], 'competencias': <dynamic>[], 'serviciosIds': <String>[], 'activo': true},
          ]);
        case ('GET', '/api/centros'):
          return json([
            {'id': 'h1', 'nombre': 'Hospital Norte', 'tipo': 'Hospital', 'direccion': null, 'zonaHoraria': 'Europe/Madrid', 'activo': true, 'responsablesIds': <String>[]},
            {'id': 'h2', 'nombre': 'Hospital Sur', 'tipo': 'Hospital', 'direccion': null, 'zonaHoraria': 'Europe/Madrid', 'activo': true, 'responsablesIds': <String>[]},
          ]);
        default:
          return json(problema(404, 'No encontrado'), 404);
      }
    });
  }

  testWidgets('el administrador ve quién cubre cada turno, qué falta, y puede agrupar por hospital y profesional', (t) async {
    await montarApp(t, servidor(['AdministradorSociedad']));
    await t.tap(find.text('Cobertura').first);
    await t.pumpAndSettle();

    expect(_enTarjetas('Ana García'), findsOneWidget);
    expect(_enTarjetas('Luis Pérez'), findsOneWidget);
    expect(find.text('Sin asignar'), findsOneWidget); // falta 1 plaza en la mañana
    expect(find.text('1/2'), findsWidgets);

    await t.tap(find.text('Por hospital'));
    await t.pumpAndSettle();
    expect(find.text('Hospital Norte'), findsWidgets);
    expect(find.text('Hospital Sur'), findsWidgets);

    await t.tap(find.text('Por profesional'));
    await t.pumpAndSettle();
    expect(find.text('1 turno'), findsNWidgets(2));
    expect(t.takeException(), isNull);
  });

  testWidgets('filtrar por profesional deja solo sus turnos y no hay desplegable de hospital', (t) async {
    await montarApp(t, servidor(['AdministradorSociedad']));
    await t.tap(find.text('Cobertura').first);
    await t.pumpAndSettle();
    expect(find.text('Todos los hospitales'), findsNothing);

    // Con menos de 3 letras no se consulta; con 3 busca entre todos los profesionales de la sociedad
    await t.enterText(find.widgetWithText(TextField, 'Profesional'), 'lu');
    await t.pump(const Duration(milliseconds: 400));
    expect(peticionesProfesionales, isEmpty);
    await t.enterText(find.widgetWithText(TextField, 'Profesional'), 'luis');
    await t.pump(const Duration(milliseconds: 400));
    await t.pumpAndSettle();
    expect(peticionesProfesionales.single['texto'], 'luis');
    await t.tap(find.descendant(of: find.byType(ListTile), matching: find.text('Luis Pérez')));
    await t.pumpAndSettle();
    expect(_enTarjetas('Ana García'), findsNothing);
    expect(_enTarjetas('Luis Pérez'), findsOneWidget);

    // Un profesional sin turnos en el periodo también se puede elegir: no hay resultados, pero tampoco error
    await t.tap(find.byTooltip('Quitar filtros').first);
    await t.pumpAndSettle();
    await t.enterText(find.widgetWithText(TextField, 'Profesional'), 'sin turnos');
    await t.pump(const Duration(milliseconds: 400));
    await t.pumpAndSettle();
    await t.tap(find.descendant(of: find.byType(ListTile), matching: find.text('Lucía Sin Turnos')));
    await t.pumpAndSettle();
    expect(find.byType(TarjetaCobertura), findsNothing);
    expect(t.takeException(), isNull);
  });

  testWidgets('un turno con más profesionales que plazas avisa en rojo de cuántos sobran', (t) async {
    final turno = _turno('t1', 'Mañana', '08:00', '15:00', 'h2', 'Hospital Sur', 4, [for (var i = 1; i <= 5; i++) _asig('p$i', 'Anestesista $i')])
      ..['plazasCubiertas'] = 4 // el servidor cuenta como cubiertas como máximo las plazas
      ..['plazasExcedidas'] = 1;
    await montarApp(t, ServidorFalso((p) => switch ((p.method, p.path)) {
          (_, '/api/autenticacion/refrescar') => json(sesionJson(roles: ['AdministradorSociedad'])),
          ('GET', '/api/turnos') => json([turno]),
          ('GET', '/api/centros') => json(<dynamic>[]),
          _ => json(problema(404, 'No encontrado'), 404),
        }));
    await t.tap(find.text('Cobertura').first);
    await t.pumpAndSettle();

    expect(find.text('Sobra 1 profesional'), findsOneWidget);
    expect(find.text('5/4'), findsWidgets); // 5 asignados para 4 plazas
    expect(find.text('Sin asignar'), findsNothing);
  });

  testWidgets('en móvil no desborda', (t) async {
    await montarApp(t, servidor(['AdministradorSociedad']), tamano: const Size(375, 812));
    await t.tap(find.byTooltip('Cobertura').first);
    await t.pumpAndSettle();
    expect(_enTarjetas('Ana García'), findsOneWidget);
    await t.tap(find.text('Por profesional'));
    await t.pumpAndSettle();
    expect(t.takeException(), isNull);
  });

  testWidgets('un profesional no ve la sección', (t) async {
    await montarApp(t, servidor(['ProfesionalSanitario']));
    expect(find.text('Cobertura'), findsNothing);
  });
}
