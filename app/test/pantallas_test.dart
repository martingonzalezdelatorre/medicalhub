
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:medicalhub_app/core/auth/almacen_tokens.dart';
import 'package:medicalhub_app/main.dart';

import 'ayudas_prueba.dart';

Future<void> _montar(WidgetTester t, ServidorFalso servidor, {Size tamano = const Size(1200, 900), String? tokenGuardado}) async {
  t.view.physicalSize = tamano;
  t.view.devicePixelRatio = 1;
  addTearDown(t.view.reset);
  final almacen = AlmacenTokensMemoria()..valor = tokenGuardado;
  await t.pumpWidget(ProviderScope(overrides: servidor.sobrescrituras(almacen: almacen), child: const MedicalHubApp()));
  await t.pumpAndSettle();
}

void main() {
  group('Login', () {
    testWidgets('sin sesión muestra el formulario y valida los campos', (t) async {
      await _montar(t, ServidorFalso((_) => json(sesionJson())));
      expect(find.text('Iniciar sesión'), findsOneWidget);

      await t.tap(find.text('Entrar'));
      await t.pumpAndSettle();
      expect(find.text('Campo obligatorio'), findsWidgets);
    });

    testWidgets('credenciales incorrectas muestran el mensaje de la API', (t) async {
      final servidor = ServidorFalso((_) => json(problema(401, 'Credenciales no válidas o cuenta bloqueada temporalmente.'), 401));
      await _montar(t, servidor, tamano: const Size(375, 812));

      await t.enterText(find.byType(TextFormField).at(0), 'ana@sociedad.es');
      await t.enterText(find.byType(TextFormField).at(1), 'mala');
      await t.tap(find.text('Entrar'));
      await t.pumpAndSettle();

      expect(find.textContaining('Credenciales no válidas'), findsOneWidget);
      expect(t.takeException(), isNull);
    });

    testWidgets('si la cuenta exige MFA aparece el campo de código y se envía', (t) async {
      final servidor = ServidorFalso((p) {
        final codigo = (p.data as Map)['codigoMfa'];
        return codigo == null ? json(problema(401, 'Introduzca el código', codigo: 'mfa_requerido'), 401) : json(sesionJson());
      });
      await _montar(t, servidor);

      await t.enterText(find.byType(TextFormField).at(0), 'ana@sociedad.es');
      await t.enterText(find.byType(TextFormField).at(1), 'Contrasena-Segura-1');
      await t.tap(find.text('Entrar'));
      await t.pumpAndSettle();
      expect(find.text('Introduzca el código de su aplicación autenticadora.'), findsOneWidget);
      expect(find.byType(TextFormField), findsNWidgets(3));

      await t.enterText(find.byType(TextFormField).at(2), '123456');
      await t.tap(find.text('Entrar'));
      await t.pumpAndSettle();

      expect(find.text('Hola, Ana García'), findsOneWidget);
    });
  });

  group('Sesión restaurada', () {
    testWidgets('un profesional ve solo Inicio (sin navegación de gestión)', (t) async {
      final servidor = ServidorFalso((_) => json(sesionJson(roles: ['ProfesionalSanitario'])));
      await _montar(t, servidor, tokenGuardado: 'R');

      expect(find.text('Hola, Ana García'), findsOneWidget);
      expect(find.byType(NavigationRail), findsNothing);
      expect(find.byType(NavigationBar), findsNothing);
      expect(find.text('Profesional sanitario'), findsOneWidget);
    });

    testWidgets('el administrador de sociedad ve Usuarios; en escritorio usa raíl lateral', (t) async {
      final servidor = ServidorFalso((p) => p.path.contains('refrescar') ? json(sesionJson()) : json(<dynamic>[]));
      await _montar(t, servidor, tokenGuardado: 'R');

      expect(find.byType(NavigationRail), findsOneWidget);
      expect(find.byType(NavigationBar), findsNothing);
      expect(find.text('Usuarios'), findsOneWidget);
      expect(find.text('Sociedades'), findsNothing);
    });

    testWidgets('en móvil la navegación pasa a barra inferior', (t) async {
      final servidor = ServidorFalso((_) => json(sesionJson()));
      await _montar(t, servidor, tamano: const Size(375, 812), tokenGuardado: 'R');

      expect(find.byType(NavigationBar), findsOneWidget);
      expect(find.byType(NavigationRail), findsNothing);
      expect(t.takeException(), isNull);
    });

    testWidgets('el administrador de plataforma ve Sociedades', (t) async {
      final servidor = ServidorFalso((_) => json(sesionJson(roles: ['AdministradorPlataforma'])));
      await _montar(t, servidor, tokenGuardado: 'R');
      expect(find.text('Sociedades'), findsOneWidget);
      expect(find.text('Usuarios'), findsNothing);
    });

    testWidgets('con MFA pendiente se obliga a configurar la verificación en dos pasos', (t) async {
      final servidor = ServidorFalso((p) {
        if (p.path.contains('mfa/configurar')) {
          return json({'secreto': 'JBSWY3DPEHPK3PXP', 'uriOtpAuth': 'otpauth://totp/MedicalHub:ana?secret=JBSWY3DPEHPK3PXP'});
        }
        return json(sesionJson(mfaPendiente: true));
      });
      await _montar(t, servidor, tokenGuardado: 'R');

      expect(find.text('Verificación en dos pasos'), findsOneWidget);
      expect(find.text('JBSWY3DPEHPK3PXP'), findsOneWidget);
      expect(find.byType(NavigationRail), findsNothing);
    });

    testWidgets('si el token guardado ya no vale se vuelve al login', (t) async {
      final servidor = ServidorFalso((_) => json(problema(401, 'Sesión no válida.'), 401));
      await _montar(t, servidor, tokenGuardado: 'CADUCADO');
      expect(find.text('Iniciar sesión'), findsOneWidget);
    });
  });
}
