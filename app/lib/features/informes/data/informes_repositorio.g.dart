// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'informes_repositorio.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TurnoSinCubrirDto _$TurnoSinCubrirDtoFromJson(Map<String, dynamic> json) =>
    TurnoSinCubrirDto(
      turnoId: json['turnoId'] as String,
      fecha: json['fecha'] as String,
      turno: json['turno'] as String,
      servicio: json['servicio'] as String,
      centro: json['centro'] as String,
      plazasLibres: (json['plazasLibres'] as num).toInt(),
    );

PendientesGestionDto _$PendientesGestionDtoFromJson(
  Map<String, dynamic> json,
) => PendientesGestionDto(
  vacantesAbiertas: (json['vacantesAbiertas'] as num).toInt(),
  ausenciasSolicitadas: (json['ausenciasSolicitadas'] as num).toInt(),
  ofertasPorAprobar: (json['ofertasPorAprobar'] as num).toInt(),
  disponibilidadesPendientes: (json['disponibilidadesPendientes'] as num)
      .toInt(),
  excepcionesUltimos30Dias: (json['excepcionesUltimos30Dias'] as num).toInt(),
);

DashboardGestionDto _$DashboardGestionDtoFromJson(Map<String, dynamic> json) =>
    DashboardGestionDto(
      desde: json['desde'] as String,
      hasta: json['hasta'] as String,
      plazasTotales: (json['plazasTotales'] as num).toInt(),
      plazasCubiertas: (json['plazasCubiertas'] as num).toInt(),
      cuadrantesBorrador: (json['cuadrantesBorrador'] as num).toInt(),
      proximosSinCubrir: (json['proximosSinCubrir'] as List<dynamic>)
          .map((e) => TurnoSinCubrirDto.fromJson(e as Map<String, dynamic>))
          .toList(),
      pendientes: json['pendientes'] == null
          ? null
          : PendientesGestionDto.fromJson(
              json['pendientes'] as Map<String, dynamic>,
            ),
    );

ProximoTurnoDto _$ProximoTurnoDtoFromJson(Map<String, dynamic> json) =>
    ProximoTurnoDto(
      asignacionId: json['asignacionId'] as String,
      fecha: json['fecha'] as String,
      turno: json['turno'] as String,
      servicio: json['servicio'] as String,
      centro: json['centro'] as String,
      cuadranteEstado: $enumDecode(
        _$EstadoCuadranteEnumMap,
        json['cuadranteEstado'],
      ),
    );

const _$EstadoCuadranteEnumMap = {
  EstadoCuadrante.borrador: 'Borrador',
  EstadoCuadrante.publicado: 'Publicado',
  EstadoCuadrante.archivado: 'Archivado',
};

DashboardProfesionalDto _$DashboardProfesionalDtoFromJson(
  Map<String, dynamic> json,
) => DashboardProfesionalDto(
  proximosTurnos: (json['proximosTurnos'] as List<dynamic>)
      .map((e) => ProximoTurnoDto.fromJson(e as Map<String, dynamic>))
      .toList(),
  horasPlanificadasMesMin: (json['horasPlanificadasMesMin'] as num).toInt(),
  ofertasPendientes: (json['ofertasPendientes'] as num).toInt(),
  ausenciasSolicitadas: (json['ausenciasSolicitadas'] as num).toInt(),
);

DashboardDto _$DashboardDtoFromJson(Map<String, dynamic> json) => DashboardDto(
  gestion: json['gestion'] == null
      ? null
      : DashboardGestionDto.fromJson(json['gestion'] as Map<String, dynamic>),
  profesional: json['profesional'] == null
      ? null
      : DashboardProfesionalDto.fromJson(
          json['profesional'] as Map<String, dynamic>,
        ),
);

AjusteHorasDto _$AjusteHorasDtoFromJson(Map<String, dynamic> json) =>
    AjusteHorasDto(
      minutos: (json['minutos'] as num).toInt(),
      motivo: json['motivo'] as String,
      usuario: json['usuario'] as String,
      fecha: DateTime.parse(json['fecha'] as String),
    );

LineaHorasDto _$LineaHorasDtoFromJson(Map<String, dynamic> json) =>
    LineaHorasDto(
      asignacionId: json['asignacionId'] as String,
      profesionalId: json['profesionalId'] as String,
      profesional: json['profesional'] as String,
      fecha: json['fecha'] as String,
      turno: json['turno'] as String,
      servicio: json['servicio'] as String,
      centro: json['centro'] as String,
      esGuardia: json['esGuardia'] as bool,
      inicioUtc: DateTime.parse(json['inicioUtc'] as String),
      finUtc: DateTime.parse(json['finUtc'] as String),
      planificadasMin: (json['planificadasMin'] as num).toInt(),
      nocturnasPlanMin: (json['nocturnasPlanMin'] as num).toInt(),
      registroId: json['registroId'] as String?,
      inicioRealUtc: json['inicioRealUtc'] == null
          ? null
          : DateTime.parse(json['inicioRealUtc'] as String),
      finRealUtc: json['finRealUtc'] == null
          ? null
          : DateTime.parse(json['finRealUtc'] as String),
      realesMin: (json['realesMin'] as num?)?.toInt(),
      nocturnasRealMin: (json['nocturnasRealMin'] as num?)?.toInt(),
      ajustesMin: (json['ajustesMin'] as num).toInt(),
      efectivasMin: (json['efectivasMin'] as num?)?.toInt(),
      ajustes:
          (json['ajustes'] as List<dynamic>?)
              ?.map((e) => AjusteHorasDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
    );

FilaInformeHorasDto _$FilaInformeHorasDtoFromJson(Map<String, dynamic> json) =>
    FilaInformeHorasDto(
      profesionalId: json['profesionalId'] as String,
      profesional: json['profesional'] as String,
      turnos: (json['turnos'] as num).toInt(),
      guardias: (json['guardias'] as num).toInt(),
      planificadasMin: (json['planificadasMin'] as num).toInt(),
      nocturnasPlanMin: (json['nocturnasPlanMin'] as num).toInt(),
      turnosRegistrados: (json['turnosRegistrados'] as num).toInt(),
      realesMin: (json['realesMin'] as num).toInt(),
      nocturnasRealMin: (json['nocturnasRealMin'] as num).toInt(),
      ajustesMin: (json['ajustesMin'] as num).toInt(),
      efectivasMin: (json['efectivasMin'] as num).toInt(),
    );

InformeHorasDto _$InformeHorasDtoFromJson(Map<String, dynamic> json) =>
    InformeHorasDto(
      filas: (json['filas'] as List<dynamic>)
          .map((e) => FilaInformeHorasDto.fromJson(e as Map<String, dynamic>))
          .toList(),
      total: FilaInformeHorasDto.fromJson(
        json['total'] as Map<String, dynamic>,
      ),
    );

FilaInformeCoberturaDto _$FilaInformeCoberturaDtoFromJson(
  Map<String, dynamic> json,
) => FilaInformeCoberturaDto(
  servicio: json['servicio'] as String,
  centro: json['centro'] as String,
  plazas: (json['plazas'] as num).toInt(),
  cubiertas: (json['cubiertas'] as num).toInt(),
  libres: (json['libres'] as num).toInt(),
  porcentajeCobertura: (json['porcentajeCobertura'] as num).toDouble(),
  vacantesAbiertas: (json['vacantesAbiertas'] as num).toInt(),
  excepciones: (json['excepciones'] as num).toInt(),
);

InformeCoberturaDto _$InformeCoberturaDtoFromJson(Map<String, dynamic> json) =>
    InformeCoberturaDto(
      filas: (json['filas'] as List<dynamic>)
          .map(
            (e) => FilaInformeCoberturaDto.fromJson(e as Map<String, dynamic>),
          )
          .toList(),
      total: FilaInformeCoberturaDto.fromJson(
        json['total'] as Map<String, dynamic>,
      ),
    );
