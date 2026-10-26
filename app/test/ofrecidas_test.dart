import 'package:flutter_test/flutter_test.dart';
import 'package:medicalhub_app/features/ausencias/data/ausencias_repositorio.dart';
import 'package:medicalhub_app/features/planificacion/presentation/pagina_planificacion.dart';

DisponibilidadDto _d({
  String estado = 'Aprobada',
  String tipo = 'Disponible',
  String desde = '2026-10-26',
  String hasta = '2026-11-01',
  int dias = 31, // lunes a viernes
  String? centro = 'Hospital Norte',
  String? turno = 'Mañana',
}) =>
    DisponibilidadDto.fromJson({
      'id': 'd', 'profesionalId': 'p', 'profesional': 'Ana', 'tipo': tipo, 'desde': desde, 'hasta': hasta, 'dias': dias,
      'tipoTurnoId': null, 'tipoTurno': turno, 'comentario': null, 'estado': estado, 'fechaDeclaracion': '2026-10-20T10:00:00+00:00',
      'motivoDecision': null, 'centroId': 'c1', 'centro': centro,
    });

void main() {
  final lunes = DateTime(2026, 10, 26);
  final domingo = DateTime(2026, 11, 1);

  test('solo la disponibilidad aprobada de tipo «Disponible» aparece, y solo en los días de la semana elegidos', () {
    final r = ofrecidasEnRango([_d(), _d(estado: 'Declarada'), _d(estado: 'Rechazada'), _d(tipo: 'Bloqueo'), _d(tipo: 'Evita')], lunes, domingo);
    expect(r.map((o) => o.dia.day), [26, 27, 28, 29, 30]); // lunes a viernes, sin fin de semana
    expect(r.first.centro, 'Hospital Norte');
    expect(r.first.turno, 'Mañana');
  });

  test('se recorta al rango consultado y un mismo turno repetido por servicio sale una sola vez', () {
    final r = ofrecidasEnRango([_d(), _d()], DateTime(2026, 10, 28), DateTime(2026, 10, 29));
    expect(r.map((o) => o.dia.day), [28, 29]);
  });

  test('turnos distintos el mismo día se muestran por separado', () {
    final r = ofrecidasEnRango([_d(turno: 'Mañana'), _d(turno: 'Tarde')], lunes, lunes);
    expect(r.map((o) => o.turno), ['Mañana', 'Tarde']);
  });
}
