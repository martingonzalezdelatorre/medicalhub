import 'package:json_annotation/json_annotation.dart';

part 'modelos.g.dart';

enum Rol {
  @JsonValue('AdministradorPlataforma')
  administradorPlataforma,
  @JsonValue('AdministradorSociedad')
  administradorSociedad,
  @JsonValue('CoordinadorMedico')
  coordinadorMedico,
  @JsonValue('ProfesionalSanitario')
  profesionalSanitario,
  @JsonValue('ResponsableCentro')
  responsableCentro,
  @JsonValue('RecursosHumanos')
  recursosHumanos,
}

@JsonSerializable()
class PerfilUsuario {
  const PerfilUsuario({
    required this.id,
    this.organizacionId,
    this.organizacion,
    required this.email,
    required this.nombreCompleto,
    required this.roles,
    required this.mfaActivado,
    required this.mfaPendiente,
    this.zonaHoraria,
  });

  factory PerfilUsuario.fromJson(Map<String, dynamic> json) => _$PerfilUsuarioFromJson(json);

  final String id;
  final String? organizacionId;
  final String? organizacion;
  final String email;
  final String nombreCompleto;
  final List<Rol> roles;
  final bool mfaActivado;
  final bool mfaPendiente;
  final String? zonaHoraria;

  bool tieneRol(Rol rol) => roles.contains(rol);

  Map<String, dynamic> toJson() => _$PerfilUsuarioToJson(this);
}

@JsonSerializable()
class RespuestaSesion {
  const RespuestaSesion({
    required this.tokenAcceso,
    required this.accesoExpira,
    required this.tokenRefresco,
    required this.refrescoExpira,
    required this.mfaPendiente,
    required this.perfil,
  });

  factory RespuestaSesion.fromJson(Map<String, dynamic> json) => _$RespuestaSesionFromJson(json);

  final String tokenAcceso;
  final DateTime accesoExpira;
  final String tokenRefresco;
  final DateTime refrescoExpira;
  final bool mfaPendiente;
  final PerfilUsuario perfil;

  Map<String, dynamic> toJson() => _$RespuestaSesionToJson(this);
}

@JsonSerializable()
class ConfiguracionMfa {
  const ConfiguracionMfa({required this.secreto, required this.uriOtpAuth});

  factory ConfiguracionMfa.fromJson(Map<String, dynamic> json) => _$ConfiguracionMfaFromJson(json);

  final String secreto;
  final String uriOtpAuth;

  Map<String, dynamic> toJson() => _$ConfiguracionMfaToJson(this);
}
