import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/informacion_sistema.dart';
import '../data/sistema_repositorio.dart';

final infoSistemaProvider = FutureProvider.autoDispose<InformacionSistema>(
  (ref) => ref.watch(sistemaRepositorioProvider).obtenerInfo(),
);
