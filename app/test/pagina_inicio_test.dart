import 'dart:ui';
import 'package:flutter_riverpod/misc.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:medicalhub_app/core/errores/fallo_api.dart';
import 'package:medicalhub_app/features/inicio/application/info_sistema_provider.dart';
import 'package:medicalhub_app/features/inicio/data/informacion_sistema.dart';
import 'package:medicalhub_app/main.dart';

Future<void> _montar(WidgetTester t, Size tamano, Override sobrescritura) async {
  t.view.physicalSize = tamano;
  t.view.devicePixelRatio = 1;
  addTearDown(t.view.reset);
  await t.pumpWidget(ProviderScope(overrides: [sobrescritura], child: const MedicalHubApp()));
  await t.pumpAndSettle();
}

void main() {
  final correcto = infoSistemaProvider.overrideWith(
    (ref) async => const InformacionSistema(nombre: 'MedicalHub API', version: '1.0.0', entorno: 'Test'),
  );

  for (final (nombre, tamano) in [
    ('móvil', const Size(375, 812)),
    ('tablet', const Size(768, 1024)),
    ('escritorio', const Size(1440, 900)),
  ]) {
    testWidgets('muestra la información de la API en $nombre sin desbordes', (t) async {
      await _montar(t, tamano, correcto);
      expect(find.text('MedicalHub API'), findsOneWidget);
      expect(find.text('Versión 1.0.0 · Test'), findsOneWidget);
      expect(t.takeException(), isNull);
    });
  }

  testWidgets('muestra mensaje de conexión y permite reintentar', (t) async {
    await _montar(
      t,
      const Size(375, 812),
      infoSistemaProvider.overrideWith((ref) => throw FalloApi.desde(DioException(requestOptions: RequestOptions()))),
    );
    expect(find.textContaining('No se ha podido conectar'), findsOneWidget);
    expect(find.text('Reintentar'), findsOneWidget);
  });
}
