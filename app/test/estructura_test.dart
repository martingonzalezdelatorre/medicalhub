import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:medicalhub_app/core/auth/auth_controlador.dart';
import 'package:medicalhub_app/core/auth/modelos.dart';
import 'package:medicalhub_app/core/router/enrutador.dart';

import 'ayudas_prueba.dart';

const _noche = {
  'id': 'tt-noche', 'nombre': 'Noche', 'horaInicio': '22:00:00', 'horaFin': '08:00:00',
  'esGuardia': false, 'activo': true, 'cruzaMedianoche': true, 'duracionHoras': 10.0,
};

final _servicio = {
  'id': 'serv1', 'centroId': 'cen1', 'centroNombre': 'Hospital Norte', 'nombre': 'Reanimación', 'descripcion': 'URPA',
  'coordinadorUsuarioId': null, 'configuracionReglas': null, 'activo': true,
  'categoriasAdmitidasIds': ['cat1'], 'competenciasRequeridasIds': ['comp1'],
  'tiposTurno': [_noche],
  'requisitos': [{'id': 'r1', 'tipoTurnoId': 'tt-noche', 'numeroProfesionales': 2, 'categoriaProfesionalId': 'cat1'}],
};

final _centro = {
  'id': 'cen1', 'nombre': 'Hospital Norte', 'tipo': 'Hospital', 'direccion': 'Calle 1', 'zonaHoraria': 'Europe/Madrid',
  'activo': true, 'responsablesIds': <String>[],
};

final _profesional = {
  'id': 'p1', 'usuarioId': null, 'nombre': 'Ana', 'apellidos': 'García', 'email': 'ana@x.es', 'telefono': null,
  'numeroColegiado': 'COL-1', 'categoriaId': 'cat1', 'especialidadesIds': <String>[],
  'competencias': [{'competenciaId': 'comp1', 'vigenteHasta': '2027-06-30'}], 'serviciosIds': ['serv1'], 'activo': true,
};

ServidorFalso _servidor({List<String> roles = const ['AdministradorSociedad'], List<RequestOptions>? escrituras}) => ServidorFalso((p) {
      if (p.method != 'GET' && !p.path.contains('refrescar')) escrituras?.add(p);
      switch (p.path) {
        case '/api/autenticacion/refrescar':
          return json(sesionJson(roles: roles));
        case '/api/categorias-profesionales':
          return json([{'id': 'cat1', 'nombre': 'Adjunto', 'descripcion': null, 'activa': true}]);
        case '/api/especialidades':
          return json([{'id': 'esp1', 'nombre': 'Anestesiología', 'descripcion': null, 'activa': true}]);
        case '/api/competencias':
          return json([{'id': 'comp1', 'nombre': 'Vía aérea difícil', 'descripcion': 'Intubación', 'activa': true}]);
        case '/api/centros':
          return json([_centro]);
        case '/api/centros/cen1':
          return json(_centro);
        case '/api/servicios':
          return p.method == 'GET' ? json([_servicio]) : json(_servicio, 201);
        case '/api/servicios/serv1':
          return json(_servicio);
        case '/api/profesionales':
          return p.method == 'GET' ? json([_profesional]) : json(_profesional, 201);
        case '/api/profesionales/p1':
          return json(_profesional);
        case '/api/usuarios':
          return json(<dynamic>[]);
        default:
          return json(problema(404, 'No encontrado'), 404);
      }
    });

void main() {
  group('Navegación por rol', () {
    testWidgets('el administrador ve todas las secciones de gestión', (t) async {
      await montarApp(t, _servidor());
      for (final texto in ['Inicio', 'Usuarios', 'Profesionales', 'Centros', 'Catálogos']) {
        expect(find.text(texto), findsWidgets, reason: texto);
      }
      expect(find.text('Sociedades'), findsNothing);
    });

    testWidgets('el coordinador consulta profesionales y centros pero no gestiona usuarios ni catálogos', (t) async {
      await montarApp(t, _servidor(roles: ['CoordinadorMedico']));
      expect(find.text('Profesionales'), findsWidgets);
      expect(find.text('Centros'), findsWidgets);
      expect(find.text('Usuarios'), findsNothing);
      expect(find.text('Catálogos'), findsNothing);
    });

    testWidgets('en móvil las cinco secciones caben en la barra inferior', (t) async {
      await montarApp(t, _servidor(), tamano: const Size(375, 812));
      expect(find.byType(NavigationBar), findsOneWidget);
      expect(find.descendant(of: find.byType(NavigationBar), matching: find.byType(NavigationDestination)), findsNWidgets(5));
      expect(t.takeException(), isNull);
    });
  });

  group('redireccionar (E2)', () {
    EstadoAuth sesion(String rol) => EstadoAuth(sesion: RespuestaSesion.fromJson(sesionJson(roles: [rol])));

    test('catálogos y alta de profesionales solo para el administrador', () {
      expect(redireccionar(sesion('CoordinadorMedico'), '/catalogos'), '/');
      expect(redireccionar(sesion('CoordinadorMedico'), '/profesionales/nuevo'), '/');
      expect(redireccionar(sesion('AdministradorSociedad'), '/catalogos'), isNull);
      expect(redireccionar(sesion('AdministradorSociedad'), '/profesionales/nuevo'), isNull);
    });

    test('profesionales, centros y servicios exigen un rol de lectura', () {
      for (final ruta in ['/profesionales', '/profesionales/p1', '/centros', '/centros/c1', '/servicios/s1']) {
        expect(redireccionar(sesion('ProfesionalSanitario'), ruta), '/', reason: ruta);
        expect(redireccionar(sesion('ResponsableCentro'), ruta), isNull, reason: ruta);
      }
    });
  });

  group('Catálogos', () {
    testWidgets('muestra los elementos y permite crear uno nuevo', (t) async {
      final escrituras = <RequestOptions>[];
      await montarApp(t, _servidor(escrituras: escrituras));
      await t.tap(find.text('Catálogos').first);
      await t.pumpAndSettle();

      expect(find.text('Anestesiología'), findsOneWidget);
      await t.tap(find.text('Nuevo elemento'));
      await t.pumpAndSettle();
      await t.tap(find.text('Guardar'));
      await t.pumpAndSettle();
      expect(find.text('Campo obligatorio'), findsOneWidget); // validación en cliente

      await t.enterText(find.byType(TextFormField).first, 'Cirugía cardíaca');
      await t.tap(find.text('Guardar'));
      await t.pumpAndSettle();
      expect(escrituras.single.path, '/api/especialidades');
      expect(escrituras.single.data, {'nombre': 'Cirugía cardíaca', 'descripcion': null});
    });
  });

  group('Centros y servicios', () {
    testWidgets('el servicio muestra turnos nocturnos con su duración y los requisitos de cobertura', (t) async {
      await montarApp(t, _servidor());
      await t.tap(find.text('Centros').first);
      await t.pumpAndSettle();
      await t.tap(find.text('Hospital Norte'));
      await t.pumpAndSettle();
      expect(find.text('Reanimación'), findsOneWidget);

      await t.tap(find.text('Reanimación'));
      await t.pumpAndSettle();

      expect(find.textContaining('22:00–08:00'), findsOneWidget);
      expect(find.textContaining('termina al día siguiente'), findsOneWidget);
      expect(find.textContaining('10 h'), findsOneWidget);
      expect(find.text('Noche: 2'), findsOneWidget);
      expect(find.text('Vía aérea difícil'), findsOneWidget);
    });

    testWidgets('un responsable de centro no ve acciones de edición', (t) async {
      await montarApp(t, _servidor(roles: ['ResponsableCentro']));
      await t.tap(find.text('Centros').first);
      await t.pumpAndSettle();
      expect(find.text('Nuevo centro'), findsNothing);

      await t.tap(find.text('Hospital Norte'));
      await t.pumpAndSettle();
      expect(find.text('Nuevo servicio'), findsNothing);
      expect(find.byIcon(Icons.edit_outlined), findsNothing);
    });
  });

  group('Profesionales', () {
    testWidgets('la lista muestra categoría y colegiado, y el administrador puede crear', (t) async {
      await montarApp(t, _servidor());
      await t.tap(find.text('Profesionales').first);
      await t.pumpAndSettle();

      expect(find.text('Ana García'), findsWidgets);
      expect(find.text('Adjunto · COL-1'), findsOneWidget);
      expect(find.text('Nuevo profesional'), findsOneWidget);
    });

    testWidgets('el coordinador ve la lista sin botón de alta', (t) async {
      await montarApp(t, _servidor(roles: ['CoordinadorMedico']));
      await t.tap(find.text('Profesionales').first);
      await t.pumpAndSettle();
      expect(find.text('Ana García'), findsWidgets);
      expect(find.text('Nuevo profesional'), findsNothing);
    });

    testWidgets('alta de un profesional con categoría, competencia con caducidad y servicio', (t) async {
      final escrituras = <RequestOptions>[];
      await montarApp(t, _servidor(escrituras: escrituras));
      await t.tap(find.text('Profesionales').first);
      await t.pumpAndSettle();
      await t.tap(find.text('Nuevo profesional'));
      await t.pumpAndSettle();

      // Sin datos: validación en el cliente
      await t.ensureVisible(find.text('Guardar'));
      await t.tap(find.text('Guardar'));
      await t.pumpAndSettle();
      expect(find.text('Campo obligatorio'), findsNWidgets(2));

      await t.enterText(find.byType(TextFormField).at(0), 'Luis');
      await t.enterText(find.byType(TextFormField).at(1), 'Pérez');
      await t.tap(find.byType(DropdownButtonFormField<String>).first);
      await t.pumpAndSettle();
      await t.tap(find.text('Adjunto').last);
      await t.pumpAndSettle();

      await t.ensureVisible(find.text('Vía aérea difícil'));
      await t.tap(find.text('Vía aérea difícil'));
      await t.pumpAndSettle();
      expect(find.text('Sin caducidad'), findsOneWidget);

      await t.ensureVisible(find.text('Hospital Norte · Reanimación'));
      await t.tap(find.text('Hospital Norte · Reanimación'));
      await t.pumpAndSettle();

      await t.ensureVisible(find.text('Guardar'));
      await t.tap(find.text('Guardar'));
      await t.pumpAndSettle();

      final cuerpo = escrituras.single.data as Map<String, dynamic>;
      expect(escrituras.single.path, '/api/profesionales');
      expect(cuerpo['nombre'], 'Luis');
      expect(cuerpo['apellidos'], 'Pérez');
      expect(cuerpo['categoriaId'], 'cat1');
      expect(cuerpo['competencias'], [{'competenciaId': 'comp1', 'vigenteHasta': null}]);
      expect(cuerpo['serviciosIds'], ['serv1']);
      expect(find.text('Profesionales'), findsWidgets); // vuelve a la lista
    });

    testWidgets('el formulario móvil no desborda', (t) async {
      await montarApp(t, _servidor(), tamano: const Size(375, 812));
      await t.tap(find.byTooltip('Profesionales').last);
      await t.pumpAndSettle();
      await t.tap(find.text('Nuevo profesional'));
      await t.pumpAndSettle();
      expect(t.takeException(), isNull);
    });
  });
}
