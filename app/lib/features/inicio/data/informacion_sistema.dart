import 'package:json_annotation/json_annotation.dart';

part 'informacion_sistema.g.dart';

@JsonSerializable()
class InformacionSistema {
  const InformacionSistema({required this.nombre, required this.version, required this.entorno});

  factory InformacionSistema.fromJson(Map<String, dynamic> json) => _$InformacionSistemaFromJson(json);

  final String nombre;
  final String version;
  final String entorno;

  Map<String, dynamic> toJson() => _$InformacionSistemaToJson(this);
}
