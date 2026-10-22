// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'usuarios_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

UsuarioDto _$UsuarioDtoFromJson(Map<String, dynamic> json) => UsuarioDto(
  id: json['id'] as String,
  email: json['email'] as String,
  nombreCompleto: json['nombreCompleto'] as String,
  roles: (json['roles'] as List<dynamic>)
      .map((e) => $enumDecode(_$RolEnumMap, e))
      .toList(),
  activo: json['activo'] as bool,
  mfaActivado: json['mfaActivado'] as bool,
);

const _$RolEnumMap = {
  Rol.administradorPlataforma: 'AdministradorPlataforma',
  Rol.administradorSociedad: 'AdministradorSociedad',
  Rol.coordinadorMedico: 'CoordinadorMedico',
  Rol.profesionalSanitario: 'ProfesionalSanitario',
  Rol.responsableCentro: 'ResponsableCentro',
};
