// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'modelos.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

PerfilUsuario _$PerfilUsuarioFromJson(Map<String, dynamic> json) =>
    PerfilUsuario(
      id: json['id'] as String,
      organizacionId: json['organizacionId'] as String?,
      organizacion: json['organizacion'] as String?,
      email: json['email'] as String,
      nombreCompleto: json['nombreCompleto'] as String,
      roles: (json['roles'] as List<dynamic>)
          .map((e) => $enumDecode(_$RolEnumMap, e))
          .toList(),
      mfaActivado: json['mfaActivado'] as bool,
      mfaPendiente: json['mfaPendiente'] as bool,
      zonaHoraria: json['zonaHoraria'] as String?,
    );

Map<String, dynamic> _$PerfilUsuarioToJson(PerfilUsuario instance) =>
    <String, dynamic>{
      'id': instance.id,
      'organizacionId': instance.organizacionId,
      'organizacion': instance.organizacion,
      'email': instance.email,
      'nombreCompleto': instance.nombreCompleto,
      'roles': instance.roles.map((e) => _$RolEnumMap[e]!).toList(),
      'mfaActivado': instance.mfaActivado,
      'mfaPendiente': instance.mfaPendiente,
      'zonaHoraria': instance.zonaHoraria,
    };

const _$RolEnumMap = {
  Rol.administradorPlataforma: 'AdministradorPlataforma',
  Rol.administradorSociedad: 'AdministradorSociedad',
  Rol.coordinadorMedico: 'CoordinadorMedico',
  Rol.profesionalSanitario: 'ProfesionalSanitario',
  Rol.responsableCentro: 'ResponsableCentro',
};

RespuestaSesion _$RespuestaSesionFromJson(Map<String, dynamic> json) =>
    RespuestaSesion(
      tokenAcceso: json['tokenAcceso'] as String,
      accesoExpira: DateTime.parse(json['accesoExpira'] as String),
      tokenRefresco: json['tokenRefresco'] as String,
      refrescoExpira: DateTime.parse(json['refrescoExpira'] as String),
      mfaPendiente: json['mfaPendiente'] as bool,
      perfil: PerfilUsuario.fromJson(json['perfil'] as Map<String, dynamic>),
    );

Map<String, dynamic> _$RespuestaSesionToJson(RespuestaSesion instance) =>
    <String, dynamic>{
      'tokenAcceso': instance.tokenAcceso,
      'accesoExpira': instance.accesoExpira.toIso8601String(),
      'tokenRefresco': instance.tokenRefresco,
      'refrescoExpira': instance.refrescoExpira.toIso8601String(),
      'mfaPendiente': instance.mfaPendiente,
      'perfil': instance.perfil,
    };

ConfiguracionMfa _$ConfiguracionMfaFromJson(Map<String, dynamic> json) =>
    ConfiguracionMfa(
      secreto: json['secreto'] as String,
      uriOtpAuth: json['uriOtpAuth'] as String,
    );

Map<String, dynamic> _$ConfiguracionMfaToJson(ConfiguracionMfa instance) =>
    <String, dynamic>{
      'secreto': instance.secreto,
      'uriOtpAuth': instance.uriOtpAuth,
    };
